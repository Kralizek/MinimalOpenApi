using MinimalOpenAPI.Abstractions.Models;

namespace MinimalOpenAPI.Generator.CodeGen;

internal enum UnionClassifierKind
{
    None,
    Structural,
    Discriminator
}

internal sealed record UnionCaseDefinition(string TypeName, string? ComponentReference, OpenApiSchema? InlineObject = null);

internal sealed record InlineUnionDefinition(
    string Name, SchemaGenerationScope Scope, SchemaUnionDefinition Definition);

internal sealed record UnionDiscriminatorMapping(string Value, int CaseIndex);

internal sealed record SchemaUnionDefinition(
    IReadOnlyList<UnionCaseDefinition> Cases,
    UnionClassifierKind ClassifierKind,
    string? DiscriminatorPropertyName,
    IReadOnlyList<UnionDiscriminatorMapping> DiscriminatorMappings);

internal sealed record UnsupportedUnionSchema(string SchemaPath, string Reason);

internal sealed class SchemaUnionAnalysis
{
    private readonly Dictionary<(OpenApiSchema, SchemaGenerationScope), SchemaUnionDefinition> _definitions;
    private readonly Dictionary<(OpenApiSchema, SchemaGenerationScope), InlineUnionDefinition> _inline;

    private SchemaUnionAnalysis(
        Dictionary<(OpenApiSchema, SchemaGenerationScope), SchemaUnionDefinition> definitions,
        Dictionary<(OpenApiSchema, SchemaGenerationScope), InlineUnionDefinition> inline,
        IReadOnlyList<UnsupportedUnionSchema> unsupportedSchemas)
    {
        _definitions = definitions;
        _inline = inline;
        UnsupportedSchemas = unsupportedSchemas;
    }

    public IReadOnlyList<UnsupportedUnionSchema> UnsupportedSchemas { get; }
    public bool HasUnions => _definitions.Count > 0 || _inline.Count > 0;
    public IEnumerable<InlineUnionDefinition> InlineDefinitions => _inline.Values;

    public IEnumerable<(string Reference, string Property, SchemaGenerationScope Scope)> DiscriminatorProperties
        => _definitions.Select(entry => (Scope: entry.Key.Item2, Definition: entry.Value))
            .Concat(_inline.Values.Select(entry => (entry.Scope, entry.Definition)))
            .Where(entry => entry.Definition.DiscriminatorPropertyName is not null)
            .SelectMany(entry => entry.Definition.Cases.Select(item =>
                (item.ComponentReference!, entry.Definition.DiscriminatorPropertyName!, entry.Scope)));

    public string? ResolveInline(OpenApiSchema schema, SchemaGenerationScope scope, string? contractsNamespace = null)
    {
        if (!_inline.TryGetValue((schema, scope), out var entry)
            && !_inline.TryGetValue((schema, SchemaGenerationScope.Neutral), out entry))
            return null;

        return contractsNamespace is null ? entry.Name : $"global::{contractsNamespace}.{entry.Name}";
    }

    public bool TryGetDefinition(OpenApiSchema schema, SchemaGenerationScope scope, out SchemaUnionDefinition definition)
        => _definitions.TryGetValue((schema, scope), out definition!);

    public static SchemaUnionAnalysis Create(
        OpenApiDocument document,
        IReadOnlyList<OpenApiOperation> operations,
        SchemaNameMap schemaNameMap,
        SchemaDirectionalityAnalysis directionality)
    {
        var definitions = new Dictionary<(OpenApiSchema, SchemaGenerationScope), SchemaUnionDefinition>();
        var inline = new Dictionary<(OpenApiSchema, SchemaGenerationScope), InlineUnionDefinition>();
        var unsupportedSchemas = new List<UnsupportedUnionSchema>();

        foreach (var component in document.Schemas)
        {
            var path = $"components/schemas/{component.Key}";
            foreach (var scope in new[] { SchemaGenerationScope.Neutral, SchemaGenerationScope.Request, SchemaGenerationScope.Response })
            {
                if (directionality.ShouldGenerateScope(component.Key, scope))
                    Visit(component.Value, path, directionality.ResolveSchemaReference(component.Key, scope), scope, componentRoot: true);
            }
        }

        foreach (var operation in operations)
        {
            var path = $"operations/{operation.OperationId}";
            var owner = TypeMapper.HandlerClassName(operation.OperationId);
            foreach (var parameter in operation.Parameters)
                CheckBinding(parameter.Schema, $"{path}/parameters/{parameter.Name}");

            if (operation.RequestBody?.Schema is { } requestSchema)
            {
                if (TypeMapper.ShouldGenerateJsonBody(operation.RequestBody))
                    VisitBody(requestSchema, $"{path}/requestBody", owner + "Request", SchemaGenerationScope.Request);
                else
                    CheckBinding(requestSchema, $"{path}/requestBody");
            }

            foreach (var response in operation.Responses)
            {
                if (response.Schema is { } responseSchema)
                {
                    if (response.ContentType is null || string.Equals(response.ContentType, "application/json", StringComparison.OrdinalIgnoreCase))
                        VisitBody(responseSchema, $"{path}/responses/{response.StatusCode}", owner + "Status" + response.StatusCode, SchemaGenerationScope.Response);
                    else
                        CheckBinding(responseSchema, $"{path}/responses/{response.StatusCode}");
                }
            }
        }

        return new SchemaUnionAnalysis(definitions, inline, unsupportedSchemas.Distinct().ToArray());

        void VisitBody(OpenApiSchema schema, string path, string name, SchemaGenerationScope scope)
        {
            var terminal = schema;
            while (terminal.Items is not null || (TypeMapper.IsDictionarySchema(terminal) && terminal.AdditionalProperties is not null))
                terminal = terminal.Items ?? terminal.AdditionalProperties!;

            if (!ReferenceEquals(terminal, schema) && TypeMapper.IsInlineObject(terminal)
                && AllOfSchemaFlattener.ContainsUnion(terminal, document.Schemas))
            {
                unsupportedSchemas.Add(new UnsupportedUnionSchema(path,
                    "a JSON body container with union-bearing inline object items must reference a named object schema instead"));
                return;
            }

            Visit(schema, path, name, scope);
        }

        void CheckBinding(OpenApiSchema schema, string path)
        {
            if (AllOfSchemaFlattener.ContainsUnion(schema, document.Schemas))
                unsupportedSchemas.Add(new UnsupportedUnionSchema(path, "schema unions are only supported in JSON payloads"));
        }

        void Visit(OpenApiSchema schema, string path, string name, SchemaGenerationScope scope, bool componentRoot = false)
        {
            if (!TypeMapper.IsUnionSchema(schema)
                && schema.Reference is { } referenceName
                && document.Schemas.TryGetValue(referenceName, out var referenced)
                && !TypeMapper.IsInlineObject(referenced)
                && !TypeMapper.IsUnionSchema(referenced)
                && AllOfSchemaFlattener.ContainsUnion(referenced, document.Schemas))
            {
                unsupportedSchemas.Add(new UnsupportedUnionSchema(path,
                    $"reference '{referenceName}' does not name a generated union or object type; inline the container or reference its union directly"));
            }

            if (schema.AllOf.Count > 0)
            {
                var conflicts = new List<AllOfPropertyConflict>();
                AllOfSchemaFlattener.Resolve(schema, document.Schemas, name, conflicts);
                foreach (var conflict in conflicts.Where(conflict => conflict.InvolvesUnion))
                    unsupportedSchemas.Add(new UnsupportedUnionSchema($"{path}/properties/{conflict.PropertyName}", "conflicting allOf definitions involve a schema union"));
            }

            if (TypeMapper.IsUnionSchema(schema))
            {
                var unionName = componentRoot ? name : name + "Union";
                if (!TryAnalyze(schema, document.Schemas, schemaNameMap, unionName, scope, directionality, out var definition, out var reason))
                {
                    unsupportedSchemas.Add(new UnsupportedUnionSchema(path, reason));
                    return;
                }

                if (componentRoot)
                    definitions[(schema, scope)] = definition;
                else if (inline.ContainsKey((schema, scope)))
                    return;
                else
                    inline.Add((schema, scope), new InlineUnionDefinition(unionName, scope, definition));

                for (var i = 0; i < definition.Cases.Count; i++)
                {
                    var item = definition.Cases[i];
                    if (item.InlineObject is { } inlineObject)
                        Visit(inlineObject, $"{path}/alternatives[{i}]", item.TypeName, scope);
                }
                return;
            }

            foreach (var property in schema.Properties)
            {
                if (directionality.ShouldIncludeProperty(property.Value, scope))
                    Visit(property.Value, $"{path}/properties/{property.Key}", name + TypeMapper.ToPascalCase(property.Key), scope);
            }
            if (schema.Items is { } items)
                Visit(items, $"{path}/items", name + "Item", scope);
            if (schema.AdditionalProperties is { } additionalProperties)
            {
                if (TypeMapper.IsDictionarySchema(schema))
                    Visit(additionalProperties, $"{path}/additionalProperties", name + "Value", scope);
                else if (AllOfSchemaFlattener.ContainsUnion(additionalProperties, document.Schemas))
                    unsupportedSchemas.Add(new UnsupportedUnionSchema($"{path}/additionalProperties",
                        "schema unions for additional properties require a dictionary without named properties"));
            }
            for (var i = 0; i < schema.AllOf.Count; i++)
            {
                var branch = schema.AllOf[i];
                if (TypeMapper.IsUnionSchema(branch)
                    || (branch.Reference is { } reference && document.Schemas.TryGetValue(reference, out var target) && TypeMapper.IsUnionSchema(target)))
                    unsupportedSchemas.Add(new UnsupportedUnionSchema($"{path}/allOf[{i}]", "a schema union cannot be an allOf branch"));
                else
                    Visit(branch, $"{path}/allOf[{i}]", name + "AllOf" + i, scope);
            }
        }
    }

    private static bool TryAnalyze(
        OpenApiSchema schema,
        IReadOnlyDictionary<string, OpenApiSchema> schemas,
        SchemaNameMap schemaNameMap,
        string unionName,
        SchemaGenerationScope scope,
        SchemaDirectionalityAnalysis directionality,
        out SchemaUnionDefinition definition,
        out string reason)
    {
        definition = null!;
        reason = string.Empty;

        if (schema.Nullable)
            return Fail("nullable union schemas are not supported", out reason);

        if (schema.OneOf is not null && schema.AnyOf is not null)
            return Fail("oneOf and anyOf cannot both be declared on the same schema", out reason);

        if (schema.Reference is not null
            || schema.Type is not null
            || schema.Enum is not null
            || schema.Properties.Count > 0
            || schema.Items is not null
            || schema.AllOf.Count > 0
            || schema.AdditionalProperties is not null
            || schema.AdditionalPropertiesAllowed)
        {
            return Fail("union schemas cannot be combined with $ref, type, enum, object, array, dictionary, or allOf siblings", out reason);
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
            if (TypeMapper.IsUnionSchema(alternative))
                return Fail("nested union alternatives are not supported", out reason);

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
            {
                allScalars = false;
                var caseName = unionName + "Case" + (cases.Count + 1);
                objectSchemas.Add(AllOfSchemaFlattener.Resolve(alternative, schemas, caseName, []));
                cases.Add(new UnionCaseDefinition(caseName, null, alternative));
                continue;
            }

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
                if (!TryResolveDiscriminator(discriminator, alternatives, objectSchemas, scope, directionality, out var mappings, out reason))
                    return false;

                definition = new SchemaUnionDefinition(
                    cases,
                    UnionClassifierKind.Discriminator,
                    discriminator.PropertyName,
                    mappings);
                return true;
            }

            if (!HasDistinctRequiredProperties(objectSchemas, directionality, scope))
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
        SchemaGenerationScope scope,
        SchemaDirectionalityAnalysis directionality,
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
                || !string.Equals(property.Type, "string", StringComparison.OrdinalIgnoreCase)
                || (property.Enum is null && TypeMapper.MapSchema(property) != "string")
                || !directionality.ShouldIncludeProperty(property, scope))
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
        SchemaDirectionalityAnalysis directionality,
        SchemaGenerationScope scope)
    {
        var requiredProperties = schemas
            .Select(schema => new HashSet<string>(schema.Required.Where(name =>
                schema.Properties.TryGetValue(name, out var property) && directionality.ShouldIncludeProperty(property, scope)),
                StringComparer.Ordinal))
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