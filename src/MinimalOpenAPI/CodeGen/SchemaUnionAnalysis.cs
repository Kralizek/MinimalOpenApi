using MinimalOpenAPI.Abstractions.Models;

namespace MinimalOpenAPI.Generator.CodeGen;

internal enum UnionClassifierKind
{
    None,
    Structural,
    Discriminator
}

internal sealed record UnionCaseDefinition(string TypeName, string? ComponentReference);

internal sealed record UnionDiscriminatorMapping(string Value, int CaseIndex);

internal sealed record SchemaUnionDefinition(
    IReadOnlyList<UnionCaseDefinition> Cases,
    UnionClassifierKind ClassifierKind,
    string? DiscriminatorPropertyName,
    IReadOnlyList<UnionDiscriminatorMapping> DiscriminatorMappings);

internal sealed record UnsupportedUnionSchema(string SchemaPath, string Reason);

internal sealed class SchemaUnionAnalysis
{
    private readonly Dictionary<OpenApiSchema, SchemaUnionDefinition> _definitions;

    private SchemaUnionAnalysis(
        Dictionary<OpenApiSchema, SchemaUnionDefinition> definitions,
        IReadOnlyList<UnsupportedUnionSchema> unsupportedSchemas)
    {
        _definitions = definitions;
        UnsupportedSchemas = unsupportedSchemas;
    }

    public IReadOnlyList<UnsupportedUnionSchema> UnsupportedSchemas { get; }

    public bool TryGetDefinition(OpenApiSchema schema, out SchemaUnionDefinition definition)
        => _definitions.TryGetValue(schema, out definition!);

    public static SchemaUnionAnalysis Create(
        OpenApiDocument document,
        SchemaNameMap schemaNameMap)
    {
        var definitions = new Dictionary<OpenApiSchema, SchemaUnionDefinition>();
        var unsupportedSchemas = new List<UnsupportedUnionSchema>();
        var visited = new HashSet<OpenApiSchema>();

        foreach (var component in document.Schemas)
        {
            Visit(
                component.Value,
                $"components/schemas/{component.Key}",
                component.Value);
        }

        foreach (var operation in document.Operations)
        {
            foreach (var parameter in operation.Parameters)
                Visit(parameter.Schema, $"operations/{operation.OperationId}/parameters/{parameter.Name}", null);

            if (operation.RequestBody?.Schema is { } requestSchema)
                Visit(requestSchema, $"operations/{operation.OperationId}/requestBody", null);

            foreach (var response in operation.Responses)
            {
                if (response.Schema is { } responseSchema)
                    Visit(responseSchema, $"operations/{operation.OperationId}/responses/{response.StatusCode}", null);
            }
        }

        return new SchemaUnionAnalysis(definitions, unsupportedSchemas);

        void Visit(OpenApiSchema schema, string path, OpenApiSchema? componentRoot)
        {
            if (!visited.Add(schema))
                return;

            if (schema.OneOf is not null || schema.AnyOf is not null)
            {
                if (componentRoot is null || !ReferenceEquals(schema, componentRoot))
                {
                    unsupportedSchemas.Add(new UnsupportedUnionSchema(
                        path,
                        "inline unions are not supported yet"));
                }
                else if (TryAnalyze(schema, document.Schemas, schemaNameMap, out var definition, out var reason))
                {
                    definitions.Add(schema, definition);
                }
                else
                {
                    unsupportedSchemas.Add(new UnsupportedUnionSchema(path, reason));
                }
            }

            foreach (var property in schema.Properties)
                Visit(property.Value, $"{path}/properties/{property.Key}", null);

            if (schema.Items is { } items)
                Visit(items, $"{path}/items", null);

            if (schema.AdditionalProperties is { } additionalProperties)
                Visit(additionalProperties, $"{path}/additionalProperties", null);

            for (var i = 0; i < schema.AllOf.Count; i++)
                Visit(schema.AllOf[i], $"{path}/allOf[{i}]", null);

            if (schema.OneOf is { } oneOf)
            {
                for (var i = 0; i < oneOf.Count; i++)
                    Visit(oneOf[i], $"{path}/oneOf[{i}]", null);
            }

            if (schema.AnyOf is { } anyOf)
            {
                for (var i = 0; i < anyOf.Count; i++)
                    Visit(anyOf[i], $"{path}/anyOf[{i}]", null);
            }
        }
    }

    private static bool TryAnalyze(
        OpenApiSchema schema,
        IReadOnlyDictionary<string, OpenApiSchema> schemas,
        SchemaNameMap schemaNameMap,
        out SchemaUnionDefinition definition,
        out string reason)
    {
        definition = null!;
        reason = string.Empty;

        if (schema.Nullable)
            return Fail("nullable union schemas are not supported", out reason);

        if (schema.OneOf is not null && schema.AnyOf is not null)
            return Fail("oneOf and anyOf cannot both be declared on the same schema", out reason);

        if (schema.Type is not null
            || schema.Enum is not null
            || schema.Properties.Count > 0
            || schema.Items is not null
            || schema.AllOf.Count > 0
            || schema.AdditionalProperties is not null
            || schema.AdditionalPropertiesAllowed)
        {
            return Fail("union schemas cannot be combined with type, enum, object, array, dictionary, or allOf siblings", out reason);
        }

        var alternatives = schema.OneOf ?? schema.AnyOf!;
        if (alternatives.Count < 2)
            return Fail("at least two alternatives are required", out reason);

        var cases = new List<UnionCaseDefinition>(alternatives.Count);
        var objectSchemas = new List<OpenApiSchema>();
        var allObjects = true;
        var allScalars = true;

        foreach (var alternative in alternatives)
        {
            if (alternative.Nullable)
                return Fail("nullable union alternatives are not supported", out reason);

            if (alternative.Reference is { } reference)
            {
                if (!schemas.TryGetValue(reference, out var referencedSchema))
                    return Fail($"reference '{alternative.RawReference ?? reference}' could not be resolved", out reason);

                if (referencedSchema.Nullable)
                    return Fail($"referenced alternative '{reference}' is nullable", out reason);

                if (referencedSchema.OneOf is not null || referencedSchema.AnyOf is not null)
                    return Fail("nested union alternatives are not supported", out reason);

                if (TypeMapper.IsInlineObject(referencedSchema))
                {
                    allScalars = false;
                    var conflicts = new List<AllOfPropertyConflict>();
                    objectSchemas.Add(AllOfSchemaFlattener.Resolve(referencedSchema, schemas, reference, conflicts));
                    cases.Add(new UnionCaseDefinition(schemaNameMap.GetTypeName(reference), reference));
                }
                else if (referencedSchema.Enum is not null)
                {
                    allObjects = false;
                    cases.Add(new UnionCaseDefinition(schemaNameMap.GetTypeName(reference), null));
                }
                else if (IsScalarType(referencedSchema.Type))
                {
                    allObjects = false;
                    cases.Add(new UnionCaseDefinition(TypeMapper.MapSchema(referencedSchema), null));
                }
                else
                {
                    return Fail($"reference '{reference}' does not resolve to a supported object or scalar schema", out reason);
                }

                continue;
            }

            if (alternative.Enum is not null)
                return Fail("inline enum alternatives are not supported", out reason);

            if (TypeMapper.IsInlineObject(alternative))
                return Fail("inline object alternatives are not supported", out reason);

            if (!IsScalarType(alternative.Type))
                return Fail("only scalar alternatives and references to component objects are supported", out reason);

            allObjects = false;
            cases.Add(new UnionCaseDefinition(TypeMapper.MapSchema(alternative), null));
        }

        if (cases.Select(unionCase => unionCase.TypeName).Distinct(StringComparer.Ordinal).Count() != cases.Count)
            return Fail("alternatives resolve to duplicate C# case types", out reason);

        if (allObjects)
        {
            if (schema.Discriminator is { } discriminator)
            {
                if (!TryResolveDiscriminator(discriminator, alternatives, objectSchemas, out var mappings, out reason))
                    return false;

                definition = new SchemaUnionDefinition(
                    cases,
                    UnionClassifierKind.Discriminator,
                    discriminator.PropertyName,
                    mappings);
                return true;
            }

            if (!HasDistinctRequiredProperties(objectSchemas, schemas))
                return Fail("object alternatives need distinct required properties or an authored discriminator mapping", out reason);

            definition = new SchemaUnionDefinition(
                cases,
                UnionClassifierKind.Structural,
                null,
                []);
            return true;
        }

        if (!allScalars)
            return Fail("mixing object and scalar alternatives is not supported", out reason);

        if (schema.Discriminator is not null)
            return Fail("discriminators are only supported for object alternatives", out reason);

        var scalarKinds = cases.Select(unionCase => GetScalarKind(unionCase.TypeName)).ToArray();
        if (scalarKinds.Contains("string", StringComparer.Ordinal)
            && scalarKinds.Contains("number", StringComparer.Ordinal))
        {
            return Fail("string and number alternatives are ambiguous under ASP.NET Core JSON web defaults", out reason);
        }

        if (scalarKinds.Any(static kind => kind is null)
            || scalarKinds.Distinct(StringComparer.Ordinal).Count() != scalarKinds.Length)
        {
            return Fail("scalar alternatives cannot be reliably classified from distinct JSON token types", out reason);
        }

        definition = new SchemaUnionDefinition(cases, UnionClassifierKind.None, null, []);
        return true;
    }

    private static bool TryResolveDiscriminator(
        OpenApiDiscriminator discriminator,
        IReadOnlyList<OpenApiSchema> alternatives,
        IReadOnlyList<OpenApiSchema> objectSchemas,
        out IReadOnlyList<UnionDiscriminatorMapping> mappings,
        out string reason)
    {
        mappings = [];
        reason = string.Empty;

        var discriminatorPropertyName = discriminator.PropertyName;
        if (string.IsNullOrWhiteSpace(discriminatorPropertyName))
            return Fail("the discriminator must declare propertyName", out reason);
        var propertyName = discriminatorPropertyName!;

        if (discriminator.Mapping.Count == 0)
            return Fail("the discriminator must declare explicit mappings", out reason);

        var indexes = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < alternatives.Count; i++)
        {
            var alternative = alternatives[i];
            if (alternative.Reference is null)
                return Fail("discriminators require referenced object alternatives", out reason);

            var objectSchema = objectSchemas[i];
            if (!objectSchema.Properties.TryGetValue(propertyName, out var property)
                || !string.Equals(property.Type, "string", StringComparison.OrdinalIgnoreCase))
            {
                return Fail(
                    $"component '{alternative.Reference}' must declare string discriminator property '{propertyName}'",
                    out reason);
            }

            indexes[alternative.Reference] = i;
        }

        var resolvedMappings = new List<UnionDiscriminatorMapping>(discriminator.Mapping.Count);
        var matchedCases = new HashSet<int>();
        foreach (var mapping in discriminator.Mapping.OrderBy(static entry => entry.Key, StringComparer.Ordinal))
        {
            var referenceName = ResolveMappingReference(mapping.Value, alternatives);
            if (referenceName is null || !indexes.TryGetValue(referenceName, out var caseIndex))
                return Fail($"discriminator mapping '{mapping.Key}' does not target a union alternative", out reason);

            var targetSchema = objectSchemas[caseIndex];
            if (targetSchema.Properties[propertyName].Enum is { } allowedValues
                && !allowedValues.Contains(mapping.Key, StringComparer.Ordinal))
            {
                return Fail(
                    $"discriminator mapping value '{mapping.Key}' is not allowed by component '{referenceName}'",
                    out reason);
            }

            resolvedMappings.Add(new UnionDiscriminatorMapping(mapping.Key, caseIndex));
            matchedCases.Add(caseIndex);
        }

        if (matchedCases.Count != alternatives.Count)
            return Fail("every discriminator alternative must have an explicit mapping", out reason);

        mappings = resolvedMappings;
        return true;
    }

    private static string? ResolveMappingReference(string mappingReference, IReadOnlyList<OpenApiSchema> alternatives)
    {
        foreach (var alternative in alternatives)
        {
            if (alternative.Reference is null)
                continue;

            if (string.Equals(mappingReference, alternative.Reference, StringComparison.Ordinal)
                || string.Equals(mappingReference, alternative.RawReference, StringComparison.Ordinal))
            {
                return alternative.Reference;
            }

            const string prefix = "#/components/schemas/";
            if (mappingReference.StartsWith(prefix, StringComparison.Ordinal))
            {
                var name = DecodeJsonPointerToken(mappingReference.Substring(prefix.Length));
                if (string.Equals(name, alternative.Reference, StringComparison.Ordinal))
                    return alternative.Reference;
            }
        }

        return null;
    }

    private static string DecodeJsonPointerToken(string value)
        => value.Replace("~1", "/").Replace("~0", "~");

    private static bool HasDistinctRequiredProperties(
        IReadOnlyList<OpenApiSchema> schemas,
        IReadOnlyDictionary<string, OpenApiSchema> allSchemas)
    {
        var requiredProperties = schemas
            .Select(schema =>
            {
                var conflicts = new List<AllOfPropertyConflict>();
                return new HashSet<string>(
                    AllOfSchemaFlattener.Resolve(schema, allSchemas, string.Empty, conflicts).Required,
                    StringComparer.Ordinal);
            })
            .ToArray();

        for (var i = 0; i < requiredProperties.Length; i++)
        {
            if (!requiredProperties[i].Any(property =>
                    requiredProperties.Where((_, index) => index != i).All(other => !other.Contains(property))))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsScalarType(string? type)
        => type is not null
            && (string.Equals(type, "string", StringComparison.OrdinalIgnoreCase)
                || string.Equals(type, "integer", StringComparison.OrdinalIgnoreCase)
                || string.Equals(type, "number", StringComparison.OrdinalIgnoreCase)
                || string.Equals(type, "boolean", StringComparison.OrdinalIgnoreCase));

    private static string? GetScalarKind(string typeName)
    {
        if (typeName is "string" or "global::System.Guid" or "global::System.DateOnly" or "global::System.DateTimeOffset")
            return "string";

        if (typeName is "int" or "long" or "float" or "double")
            return "number";

        if (typeName == "bool")
            return "boolean";

        return null;
    }

    private static bool Fail(string message, out string reason)
    {
        reason = message;
        return false;
    }
}