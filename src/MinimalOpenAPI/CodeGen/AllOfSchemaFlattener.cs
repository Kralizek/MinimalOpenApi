using MinimalOpenAPI.Abstractions.Models;

namespace MinimalOpenAPI.Generator.CodeGen;

internal sealed record AllOfPropertyConflict(string SchemaName, string PropertyName);

internal static class AllOfSchemaFlattener
{
    public static OpenApiSchema Resolve(
        OpenApiSchema schema,
        IReadOnlyDictionary<string, OpenApiSchema> allSchemas,
        string ownerSchemaName,
        List<AllOfPropertyConflict> conflicts)
        => ResolveCore(
            schema,
            allSchemas,
            ownerSchemaName,
            conflicts,
            new HashSet<string>(StringComparer.Ordinal));

    private static OpenApiSchema ResolveCore(
        OpenApiSchema schema,
        IReadOnlyDictionary<string, OpenApiSchema> allSchemas,
        string ownerSchemaName,
        List<AllOfPropertyConflict> conflicts,
        HashSet<string> expandingReferences)
    {
        var resolvedProperties = new Dictionary<string, OpenApiSchema>(StringComparer.Ordinal);
        foreach (var property in schema.Properties)
            resolvedProperties[property.Key] = ResolveCore(property.Value, allSchemas, ownerSchemaName, conflicts, expandingReferences);

        var resolvedItems = schema.Items is not null
            ? ResolveCore(schema.Items, allSchemas, ownerSchemaName, conflicts, expandingReferences)
            : null;

        var resolvedAdditionalProperties = schema.AdditionalProperties is not null
            ? ResolveCore(schema.AdditionalProperties, allSchemas, ownerSchemaName, conflicts, expandingReferences)
            : null;

        if (schema.AllOf.Count == 0)
        {
            return new OpenApiSchema
            {
                Type = schema.Type,
                Format = schema.Format,
                Nullable = schema.Nullable,
                ReadOnly = schema.ReadOnly,
                WriteOnly = schema.WriteOnly,
                Reference = schema.Reference,
                Items = resolvedItems,
                Properties = resolvedProperties,
                Required = schema.Required.ToList(),
                Enum = schema.Enum?.ToList(),
                MinLength = schema.MinLength,
                MaxLength = schema.MaxLength,
                Pattern = schema.Pattern,
                Minimum = schema.Minimum,
                Maximum = schema.Maximum,
                MinItems = schema.MinItems,
                MaxItems = schema.MaxItems,
                AdditionalProperties = resolvedAdditionalProperties,
                AdditionalPropertiesAllowed = schema.AdditionalPropertiesAllowed,
                Default = schema.Default
            };
        }

        var mergedProperties = new Dictionary<string, OpenApiSchema>(StringComparer.Ordinal);
        var mergedRequired = new HashSet<string>(StringComparer.Ordinal);
        var conflictedProperties = new HashSet<string>(StringComparer.Ordinal);
        var mergedAdditionalProperties = resolvedAdditionalProperties;
        var mergedAdditionalPropertiesAllowed = schema.AdditionalPropertiesAllowed;

        foreach (var allOfSchema in schema.AllOf)
        {
            var resolvedAllOfSchema = ResolveCore(allOfSchema, allSchemas, ownerSchemaName, conflicts, expandingReferences);
            if (resolvedAllOfSchema.Reference is not null
                && resolvedAllOfSchema.AllOf.Count == 0
                && resolvedAllOfSchema.Properties.Count == 0)
            {
                resolvedAllOfSchema = ResolveReferencedSchema(
                    resolvedAllOfSchema.Reference,
                    allSchemas,
                    ownerSchemaName,
                    conflicts,
                    expandingReferences);
            }

            MergeInto(
                targetProperties: mergedProperties,
                targetRequired: mergedRequired,
                targetAdditionalProperties: ref mergedAdditionalProperties,
                targetAdditionalPropertiesAllowed: ref mergedAdditionalPropertiesAllowed,
                sourceSchema: resolvedAllOfSchema,
                ownerSchemaName: ownerSchemaName,
                conflicts: conflicts,
                conflictedProperties: conflictedProperties);
        }

        MergeInto(
            targetProperties: mergedProperties,
            targetRequired: mergedRequired,
            targetAdditionalProperties: ref mergedAdditionalProperties,
            targetAdditionalPropertiesAllowed: ref mergedAdditionalPropertiesAllowed,
            sourceSchema: new OpenApiSchema
            {
                Properties = resolvedProperties,
                Required = schema.Required.ToList(),
                AdditionalProperties = resolvedAdditionalProperties,
                AdditionalPropertiesAllowed = schema.AdditionalPropertiesAllowed
            },
            ownerSchemaName: ownerSchemaName,
            conflicts: conflicts,
            conflictedProperties: conflictedProperties);

        return new OpenApiSchema
        {
            Type = "object",
            Nullable = schema.Nullable,
            ReadOnly = schema.ReadOnly,
            WriteOnly = schema.WriteOnly,
            Properties = mergedProperties,
            Required = mergedRequired.ToList(),
            AdditionalProperties = mergedAdditionalProperties,
            AdditionalPropertiesAllowed = mergedAdditionalPropertiesAllowed,
            Default = schema.Default
        };
    }

    private static OpenApiSchema ResolveReferencedSchema(
        string referenceName,
        IReadOnlyDictionary<string, OpenApiSchema> allSchemas,
        string ownerSchemaName,
        List<AllOfPropertyConflict> conflicts,
        HashSet<string> expandingReferences)
    {
        if (!allSchemas.TryGetValue(referenceName, out var referencedSchema))
            return new OpenApiSchema { Reference = referenceName };

        // allOf may legitimately form cycles across component refs (A -> B -> A).
        // Stop recursive expansion when we revisit a ref currently being expanded.
        if (!expandingReferences.Add(referenceName))
            return new OpenApiSchema { Reference = referenceName };

        try
        {
            return ResolveCore(referencedSchema, allSchemas, ownerSchemaName, conflicts, expandingReferences);
        }
        finally
        {
            expandingReferences.Remove(referenceName);
        }
    }

    private static void MergeInto(
        Dictionary<string, OpenApiSchema> targetProperties,
        HashSet<string> targetRequired,
        ref OpenApiSchema? targetAdditionalProperties,
        ref bool targetAdditionalPropertiesAllowed,
        OpenApiSchema sourceSchema,
        string ownerSchemaName,
        List<AllOfPropertyConflict> conflicts,
        HashSet<string> conflictedProperties)
    {
        foreach (var required in sourceSchema.Required)
            targetRequired.Add(required);

        foreach (var property in sourceSchema.Properties)
        {
            if (!targetProperties.TryGetValue(property.Key, out var existing))
            {
                targetProperties[property.Key] = property.Value;
                continue;
            }

            if (AreEquivalent(existing, property.Value))
                continue;

            targetProperties[property.Key] = CreateRawJsonFallbackSchema();
            if (conflictedProperties.Add(property.Key))
                conflicts.Add(new AllOfPropertyConflict(ownerSchemaName, property.Key));
        }

        if (sourceSchema.AdditionalPropertiesAllowed)
            targetAdditionalPropertiesAllowed = true;

        if (sourceSchema.AdditionalProperties is null)
            return;

        if (targetAdditionalProperties is null)
        {
            targetAdditionalProperties = sourceSchema.AdditionalProperties;
            return;
        }

        if (!AreEquivalent(targetAdditionalProperties, sourceSchema.AdditionalProperties))
        {
            targetAdditionalProperties = null;
            targetAdditionalPropertiesAllowed = true;
        }
    }

    private static bool AreEquivalent(OpenApiSchema left, OpenApiSchema right)
    {
        if (!string.Equals(left.Type, right.Type, StringComparison.Ordinal)
            || !string.Equals(left.Format, right.Format, StringComparison.Ordinal)
            || left.Nullable != right.Nullable
            || !string.Equals(left.Reference, right.Reference, StringComparison.Ordinal)
            || left.AdditionalPropertiesAllowed != right.AdditionalPropertiesAllowed
            || left.ReadOnly != right.ReadOnly
            || left.WriteOnly != right.WriteOnly
            || left.MinLength != right.MinLength
            || left.MaxLength != right.MaxLength
            || !string.Equals(left.Pattern, right.Pattern, StringComparison.Ordinal)
            || left.Minimum != right.Minimum
            || left.Maximum != right.Maximum
            || left.MinItems != right.MinItems
            || left.MaxItems != right.MaxItems
            || !string.Equals(left.Default, right.Default, StringComparison.Ordinal))
        {
            return false;
        }

        if (!SequenceEqual(left.Enum, right.Enum))
            return false;
        if (!SetEqual(left.Required, right.Required))
            return false;
        if (!AreEquivalentNullable(left.Items, right.Items))
            return false;
        if (!AreEquivalentNullable(left.AdditionalProperties, right.AdditionalProperties))
            return false;
        if (!DictionaryEqual(left.Properties, right.Properties))
            return false;
        if (left.AllOf.Count != right.AllOf.Count)
            return false;

        for (var i = 0; i < left.AllOf.Count; i++)
        {
            if (!AreEquivalent(left.AllOf[i], right.AllOf[i]))
                return false;
        }

        return true;
    }

    private static bool AreEquivalentNullable(OpenApiSchema? left, OpenApiSchema? right)
    {
        if (left is null || right is null)
            return left is null && right is null;
        return AreEquivalent(left, right);
    }

    private static bool DictionaryEqual(
        Dictionary<string, OpenApiSchema> left,
        Dictionary<string, OpenApiSchema> right)
    {
        if (left.Count != right.Count)
            return false;

        foreach (var pair in left)
        {
            if (!right.TryGetValue(pair.Key, out var rightValue))
                return false;
            if (!AreEquivalent(pair.Value, rightValue))
                return false;
        }

        return true;
    }

    private static bool SequenceEqual(List<string>? left, List<string>? right)
    {
        if (left is null || right is null)
            return left is null && right is null;

        if (left.Count != right.Count)
            return false;

        for (var i = 0; i < left.Count; i++)
        {
            if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
                return false;
        }

        return true;
    }

    private static bool SetEqual(List<string> left, List<string> right)
    {
        if (left.Count != right.Count)
            return false;

        var set = new HashSet<string>(left, StringComparer.Ordinal);
        foreach (var value in right)
        {
            if (!set.Remove(value))
                return false;
        }

        return set.Count == 0;
    }

    private static OpenApiSchema CreateRawJsonFallbackSchema()
        // This sentinel type is recognized by TypeMapper.MapSchema and maps to
        // global::System.Text.Json.JsonElement — the appropriate fallback for a JSON property
        // whose type could not be determined during allOf flattening.
        => new() { Type = TypeMapper.RawJsonSentinelType };
}