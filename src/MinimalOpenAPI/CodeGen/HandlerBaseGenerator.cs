using System.Text;

using MinimalOpenAPI.Abstractions.Models;

namespace MinimalOpenAPI.Generator.CodeGen;

/// <summary>
/// A multipart form-data property that was skipped during code generation because its
/// schema shape cannot be bound via ASP.NET Core form binding.
/// </summary>
internal sealed record MultipartUnsupportedShape(string FormRecordTypeName, string PropertyName);

/// <summary>
/// Generates abstract handler base classes for each operation.
/// Inline object schemas (those without a <c>$ref</c>) in <c>requestBody</c> or
/// <c>responses</c> are emitted as nested <c>sealed record</c> types inside the base
/// class instead of as top-level types in the Contracts namespace.
/// </summary>
internal static class HandlerBaseGenerator
{
    public static string Generate(
        OpenApiOperation operation,
        string rootNamespace,
        string specName,
        SchemaDirectionalityAnalysis directionality,
        IReadOnlyDictionary<string, OpenApiSchema>? allSchemas = null,
        List<AllOfPropertyConflict>? allOfConflicts = null,
        List<MultipartUnsupportedShape>? multipartUnsupportedShapes = null,
        List<OpenApiResponse>? handlerResponses = null)
    {
        var contractsNs = $"{rootNamespace}.{specName}.Contracts";
        var handlerClass = TypeMapper.HandlerClassName(operation.OperationId);
        var schemas = allSchemas ?? new Dictionary<string, OpenApiSchema>();
        handlerResponses ??= operation.Responses;

        // When the caller does not supply a collector, use a local discard list so that
        // AllOfSchemaFlattener.Resolve always has a valid target.  Callers that care about
        // conflicts (e.g. the source-generator orchestrator) should pass their own list.
        var conflictSink = allOfConflicts ?? [];

        // Collect inline schemas in deterministic order: request body first,
        // then responses ordered by status code.
        // For allOf schemas, also pre-compute the flattened (effective) shape so that the
        // nested record contains all merged properties, not just the raw allOf list.
        var inlineSchemas = new List<(OpenApiSchema OriginalSchema, OpenApiSchema EffectiveSchema, string TypeName, SchemaGenerationScope Scope)>();
        if (operation.RequestBody?.Schema is { } reqSchema && TypeMapper.IsInlineObject(reqSchema)
            && TypeMapper.ShouldGenerateJsonBody(operation.RequestBody))
        {
            var effective = reqSchema.AllOf.Count > 0
                ? AllOfSchemaFlattener.Resolve(reqSchema, schemas, TypeMapper.GetInlineRequestBodyTypeName(), conflictSink)
                : reqSchema;
            inlineSchemas.Add((reqSchema, effective, TypeMapper.GetInlineRequestBodyTypeName(), SchemaGenerationScope.Request));
        }
        foreach (var r in operation.Responses.OrderBy(r => r.StatusCode))
        {
            // File responses carry raw bytes; their schema never produces a DTO.
            if (TypeMapper.IsFileResponse(r))
                continue;

            if (r.Schema is { } respSchema && TypeMapper.IsInlineObject(respSchema))
            {
                var typeName = TypeMapper.GetInlineResponseTypeName(r.StatusCode);
                var effective = respSchema.AllOf.Count > 0
                    ? AllOfSchemaFlattener.Resolve(respSchema, schemas, typeName, conflictSink)
                    : respSchema;
                inlineSchemas.Add((respSchema, effective, typeName, SchemaGenerationScope.Response));
            }
        }

        // Recursively collect all nested inline object schemas (including dictionary
        // value types and array item schemas) from the flattened effective schemas.  The list is ordered so that
        // dependencies appear before the schemas that reference them, which matches the
        // emission order required for the nested record declarations.
        var nestedSchemas = new List<(OpenApiSchema Schema, string TypeName, SchemaGenerationScope Scope)>();
        foreach (var (_, effective, typeName, scope) in inlineSchemas)
            CollectNestedInlineSchemas(effective, typeName, scope, directionality, nestedSchemas);

        // The resolver maps each schema instance (as it appears in the effective/original schema
        // graphs) to its short nested type name so that TypeMapper.MapSchema can substitute inline
        // schemas by reference when building property types, return types, and handler parameters.
        var allInlineSchemas = nestedSchemas
            .Concat(inlineSchemas.Select(s => (s.OriginalSchema, s.TypeName, s.Scope)))
            .ToList();

        // Resolver: inline schema → SHORT nested type name (usable inside the class body).
        InlineSchemaResolver localResolver = s =>
        {
            foreach (var (schema, typeName, _) in allInlineSchemas)
                if (ReferenceEquals(schema, s)) return typeName;
            return null;
        };

        var returnType = TypeMapper.BuildReturnType(
            handlerResponses,
            contractsNs,
            localResolver,
            resolveReference: referenceName => directionality.ResolveSchemaReference(referenceName, SchemaGenerationScope.Response));
        var parameters = BuildHandlerParameters(operation, contractsNs, localResolver, directionality);

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine($"namespace {rootNamespace}.{specName}.Endpoints;");
        sb.AppendLine();
        sb.AppendLine($"/// <summary>Abstract handler base for the <c>{operation.OperationId}</c> operation.</summary>");
        TypeMapper.AppendGeneratedAttributes(sb);
        sb.AppendLine($"public class {handlerClass}");
        sb.AppendLine("{");

        // Emit the multipart form-data Request record when applicable.
        if (TypeMapper.ShouldGenerateMultipartFormBody(operation.RequestBody))
        {
            var multipartSink = multipartUnsupportedShapes ?? [];
            AppendMultipartFormRecord(sb, TypeMapper.GetInlineRequestBodyTypeName(), operation.RequestBody!.Schema!, directionality, schemas, multipartSink, contractsNs);
            sb.AppendLine();
        }

        // Emit all nested records (deepest dependencies first so each type is declared before
        // it is referenced by a parent record).
        foreach (var (schema, typeName, scope) in nestedSchemas)
        {
            AppendNestedRecord(sb, typeName, schema, contractsNs, directionality, scope, localResolver);
            sb.AppendLine();
        }

        // Emit nested records using the EFFECTIVE (flattened) schema so all merged properties
        // from allOf branches are included in the generated record.
        foreach (var (_, effective, typeName, scope) in inlineSchemas)
        {
            AppendNestedRecord(sb, typeName, effective, contractsNs, directionality, scope, localResolver);
            sb.AppendLine();
        }

        // Emit the Parameters nested record for non-path parameters (query, header, cookie).
        var nonPathParams = operation.Parameters
            .Where(p => p.Location != ParameterLocation.Path)
            .ToList();
        if (nonPathParams.Count > 0)
        {
            AppendParametersRecord(sb, nonPathParams, contractsNs,
                referenceName => directionality.ResolveSchemaReference(referenceName, SchemaGenerationScope.Neutral));
            sb.AppendLine();
        }

        var problemResponses = handlerResponses
            .Where(TypeMapper.IsProblemResponse)
            .OrderBy(r => r.StatusCode)
            .ToList();

        foreach (var response in problemResponses)
        {
            AppendProblemResultWrapper(sb, response, contractsNs, localResolver);
            sb.AppendLine();
        }

        var fileResponses = handlerResponses
            .Where(TypeMapper.IsFileResponse)
            .OrderBy(r => r.StatusCode)
            .ToList();

        foreach (var response in fileResponses)
        {
            AppendFileResultWrapper(sb, response);
            sb.AppendLine();
        }

        sb.Append($"    public virtual global::System.Threading.Tasks.Task<{returnType}> HandleAsync(");

        for (var i = 0; i < parameters.Count; i++)
        {
            var sep = i < parameters.Count - 1 ? ", " : "";
            sb.Append($"{parameters[i]}{sep}");
        }

        sb.AppendLine(")");
        sb.AppendLine($"        => throw new global::System.NotImplementedException(");
        sb.AppendLine($"            $\"{{GetType().Name}} has not implemented HandleAsync for operation '{operation.OperationId}'.\");");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static List<string> BuildHandlerParameters(
        OpenApiOperation operation,
        string contractsNs,
        InlineSchemaResolver localResolver,
        SchemaDirectionalityAnalysis directionality)
    {
        var list = new List<string>();

        // Path parameters first
        foreach (var p in operation.Parameters.Where(p => p.Location == ParameterLocation.Path))
        {
            list.Add($"{TypeMapper.MapSchema(p.Schema, contractsNamespace: contractsNs, resolveReference: referenceName => directionality.ResolveSchemaReference(referenceName, SchemaGenerationScope.Neutral))} {TypeMapper.ToCamelCase(p.Name)}");
        }

        // Non-path parameters (query, header, cookie) wrapped in Parameters record
        if (operation.Parameters.Any(p => p.Location != ParameterLocation.Path))
        {
            list.Add("Parameters parameters");
        }

        // Request body
        if (TypeMapper.ShouldGenerateJsonBody(operation.RequestBody))
        {
            var bodyType = TypeMapper.MapSchema(
                operation.RequestBody!.Schema!,
                contractsNamespace: contractsNs,
                resolveInline: localResolver,
                resolveReference: referenceName => directionality.ResolveSchemaReference(referenceName, SchemaGenerationScope.Request));
            list.Add($"{bodyType} request");
        }
        else if (TypeMapper.ShouldGenerateMultipartFormBody(operation.RequestBody))
        {
            list.Add($"{TypeMapper.GetInlineRequestBodyTypeName()} request");
        }

        // CancellationToken always last
        list.Add("global::System.Threading.CancellationToken cancellationToken");

        return list;
    }

    /// <summary>
    /// Recursively walks <paramref name="effectiveSchema"/> and collects every inline object
    /// and dictionary-value inline-object property, together with the derived C# type name each
    /// one will be emitted as. Items are added in dependency order: a nested type is added only
    /// after all of its own nested types have been added first, so the resulting list can be
    /// iterated in order to emit declarations without forward references.
    /// </summary>
    private static void CollectNestedInlineSchemas(
        OpenApiSchema effectiveSchema,
        string parentTypeName,
        SchemaGenerationScope scope,
        SchemaDirectionalityAnalysis directionality,
        List<(OpenApiSchema Schema, string TypeName, SchemaGenerationScope Scope)> collected)
    {
        foreach (var propKvp in effectiveSchema.Properties)
        {
            var propSchema = propKvp.Value;
            if (!directionality.ShouldIncludeProperty(propSchema, scope))
                continue;

            var derivedName = parentTypeName + TypeMapper.ToPascalCase(propKvp.Key);

            if (TypeMapper.IsInlineObject(propSchema))
            {
                // Recurse first so that deeper types are declared before this one.
                CollectNestedInlineSchemas(propSchema, derivedName, scope, directionality, collected);
                collected.Add((propSchema, derivedName, scope));
            }
            else if (TypeMapper.IsDictionarySchema(propSchema)
                && propSchema.AdditionalProperties is { } valueSchema
                && TypeMapper.IsInlineObject(valueSchema))
            {
                var valueName = derivedName + "Value";
                CollectNestedInlineSchemas(valueSchema, valueName, scope, directionality, collected);
                collected.Add((valueSchema, valueName, scope));
            }
            else if (propSchema.Type?.ToLowerInvariant() == "array" && propSchema.Items is { } itemSchema)
            {
                var itemName = derivedName + "Item";
                CollectArrayItemInlineSchemas(itemSchema, itemName, scope, directionality, collected);
            }
        }
    }

    /// <summary>
    /// Walks an array item schema and collects any inline object types it introduces.
    /// Only inline object item schemas are collected; inline enum item schemas are intentionally
    /// ignored so that operation-scoped inline enum array items fall back to the primitive
    /// <c>string[]</c> type until handler-local enum emission is implemented.
    /// When the item schema is itself an array, recurses into its own items so that
    /// arbitrary array nesting (e.g. <c>array.items.items.type = object</c>) is handled.
    /// </summary>
    private static void CollectArrayItemInlineSchemas(
        OpenApiSchema itemSchema,
        string itemName,
        SchemaGenerationScope scope,
        SchemaDirectionalityAnalysis directionality,
        List<(OpenApiSchema Schema, string TypeName, SchemaGenerationScope Scope)> collected)
    {
        if (TypeMapper.IsInlineObject(itemSchema))
        {
            // Recurse into the item schema first so its own nested types are declared
            // before the item type itself.
            CollectNestedInlineSchemas(itemSchema, itemName, scope, directionality, collected);
            collected.Add((itemSchema, itemName, scope));
        }
        else if (itemSchema.Type?.ToLowerInvariant() == "array" && itemSchema.Items is { } innerItemSchema)
        {
            // array-of-array: walk into the inner item schema using an extra "Item" suffix.
            CollectArrayItemInlineSchemas(innerItemSchema, itemName + "Item", scope, directionality, collected);
        }
    }

    /// <summary>Appends a nested <c>sealed record</c> for an inline schema.</summary>
    private static void AppendNestedRecord(
        StringBuilder sb,
        string typeName,
        OpenApiSchema schema,
        string contractsNamespace,
        SchemaDirectionalityAnalysis directionality,
        SchemaGenerationScope scope,
        InlineSchemaResolver? resolveInline = null)
    {
        sb.AppendLine($"    /// <summary>Inline DTO for the <c>{typeName}</c> schema of this operation.</summary>");
        TypeMapper.AppendGeneratedAttributes(sb, "    ");
        sb.AppendLine($"    public sealed record {typeName}");
        sb.AppendLine("    {");

        foreach (var propKvp in schema.Properties)
        {
            var propName = propKvp.Key;
            var propSchema = propKvp.Value;
            if (!directionality.ShouldIncludeProperty(propSchema, scope))
                continue;
            var isRequired = schema.Required.Contains(propName, StringComparer.OrdinalIgnoreCase);
            var nullable = propSchema.Nullable || !isRequired;
            var csharpTypeName = TypeMapper.MapSchema(
                propSchema,
                nullable: nullable,
                contractsNamespace: contractsNamespace,
                resolveInline: resolveInline,
                resolveReference: referenceName => directionality.ResolveSchemaReference(referenceName, scope));
            var csharpName = TypeMapper.ToPascalCase(propName);

            sb.AppendLine($"        [global::System.Text.Json.Serialization.JsonPropertyName(\"{propName}\")]");

            foreach (var attr in ValidationAttributeEmitter.GetAttributes(propSchema, "        "))
                sb.AppendLine(attr);

            if (nullable)
            {
                sb.AppendLine($"        public {csharpTypeName} {csharpName} {{ get; init; }}");
            }
            else
            {
                sb.AppendLine($"        public required {csharpTypeName} {csharpName} {{ get; init; }}");
            }
        }

        sb.AppendLine("    }");
    }

    /// <summary>
    /// Appends nested <c>sealed record</c>s for a <c>multipart/form-data</c> request body.
    /// <para>
    /// Nested object properties (inline or via <c>$ref</c>) produce their own form-specific
    /// records (e.g. <c>RequestMetadata</c> for a property named <c>metadata</c>) emitted in
    /// dependency order so that each type is declared before it is referenced.  All generated
    /// form record properties use <c>[FromForm(Name = "...")]</c> with the original OpenAPI
    /// field name, matching the ASP.NET Core form binding key convention
    /// (e.g. <c>metadata.title</c> for the <c>title</c> field nested inside <c>metadata</c>).
    /// </para>
    /// <para>
    /// File fields (<c>string/binary</c>) map to <c>IFormFile</c>; arrays of file fields map to
    /// <c>IReadOnlyList&lt;IFormFile&gt;</c>.  Array-of-object and dictionary fields are not
    /// bindable via form data and are omitted — a <see cref="MultipartUnsupportedShape"/> entry
    /// is added to <paramref name="unsupportedShapes"/> for each such field so that the caller
    /// can emit a <c>MOA011</c> diagnostic.
    /// </para>
    /// </summary>
    private static void AppendMultipartFormRecord(
        StringBuilder sb,
        string typeName,
        OpenApiSchema schema,
        SchemaDirectionalityAnalysis directionality,
        IReadOnlyDictionary<string, OpenApiSchema> allSchemas,
        List<MultipartUnsupportedShape> unsupportedShapes,
        string? contractsNamespace = null)
    {
        // Collect all nested form records in dependency order (deepest first).
        // Each entry: (resolved schema, C# type name, original propSchema for lookup).
        var nestedFormTypes = new List<(OpenApiSchema Schema, string TypeName, OpenApiSchema OriginalPropSchema)>();
        CollectMultipartFormNestedTypes(schema, typeName, directionality, allSchemas, nestedFormTypes);

        // Build a resolver: given a propSchema (by identity), returns the C# type name for
        // its generated form record, or null if it is not a nested object type.
        FormTypeResolver resolveFormType = propSchema =>
        {
            foreach (var (_, nestedTypeName, originalPropSchema) in nestedFormTypes)
                if (ReferenceEquals(originalPropSchema, propSchema)) return nestedTypeName;
            return null;
        };

        // Emit nested types first (dependencies already in correct order).
        foreach (var (nestedSchema, nestedTypeName, _) in nestedFormTypes)
        {
            AppendSingleMultipartFormRecord(sb, nestedTypeName, nestedSchema, directionality, allSchemas, resolveFormType, unsupportedShapes, contractsNamespace);
            sb.AppendLine();
        }

        // Emit the root Request record.
        AppendSingleMultipartFormRecord(sb, typeName, schema, directionality, allSchemas, resolveFormType, unsupportedShapes, contractsNamespace);
    }

    /// <summary>
    /// Recursively collects all form-specific nested record types required by
    /// <paramref name="schema"/>'s object properties in dependency order.
    /// </summary>
    private static void CollectMultipartFormNestedTypes(
        OpenApiSchema schema,
        string parentTypeName,
        SchemaDirectionalityAnalysis directionality,
        IReadOnlyDictionary<string, OpenApiSchema> allSchemas,
        List<(OpenApiSchema Schema, string TypeName, OpenApiSchema OriginalPropSchema)> collected)
    {
        foreach (var propKvp in schema.Properties)
        {
            var propName = propKvp.Key;
            var propSchema = propKvp.Value;
            if (!directionality.ShouldIncludeProperty(propSchema, SchemaGenerationScope.Request))
                continue;

            var nestedTypeName = parentTypeName + TypeMapper.ToPascalCase(propName);

            // Resolve the property to an object schema (inline or via $ref).
            OpenApiSchema? resolvedSchema = null;
            if (propSchema.Reference is not null)
            {
                allSchemas.TryGetValue(propSchema.Reference, out resolvedSchema);
            }
            else if (TypeMapper.IsInlineObject(propSchema))
            {
                resolvedSchema = propSchema;
            }

            if (resolvedSchema is null || !TypeMapper.IsInlineObject(resolvedSchema))
                continue;

            // Recurse first so dependencies are emitted before the type that uses them.
            CollectMultipartFormNestedTypes(resolvedSchema, nestedTypeName, directionality, allSchemas, collected);
            collected.Add((resolvedSchema, nestedTypeName, propSchema));
        }
    }

    /// <summary>
    /// Emits a single form-data <c>sealed record</c> with <c>[FromForm(Name = "...")]</c>
    /// attributes on every property.
    /// </summary>
    private static void AppendSingleMultipartFormRecord(
        StringBuilder sb,
        string typeName,
        OpenApiSchema schema,
        SchemaDirectionalityAnalysis directionality,
        IReadOnlyDictionary<string, OpenApiSchema> allSchemas,
        FormTypeResolver resolveFormType,
        List<MultipartUnsupportedShape> unsupportedShapes,
        string? contractsNamespace = null)
    {
        sb.AppendLine($"    /// <summary>Form data DTO for the <c>{typeName}</c> record of this operation.</summary>");
        TypeMapper.AppendGeneratedAttributes(sb, "    ");
        sb.AppendLine($"    public sealed record {typeName}");
        sb.AppendLine("    {");

        foreach (var propKvp in schema.Properties)
        {
            var propName = propKvp.Key;
            var propSchema = propKvp.Value;
            if (!directionality.ShouldIncludeProperty(propSchema, SchemaGenerationScope.Request))
                continue;

            var isRequired = schema.Required.Contains(propName, StringComparer.OrdinalIgnoreCase);
            var nullable = propSchema.Nullable || !isRequired;
            var csharpName = TypeMapper.ToPascalCase(propName);

            string csharpTypeName;

            var nestedFormTypeName = resolveFormType(propSchema);
            if (nestedFormTypeName is not null)
            {
                // Nested object type — use the generated form-specific record.
                csharpTypeName = nullable ? $"{nestedFormTypeName}?" : nestedFormTypeName;
            }
            else if (IsUnsupportedMultipartFormShape(propSchema, allSchemas))
            {
                // Cannot bind via form data — emit a diagnostic entry and skip.
                unsupportedShapes.Add(new MultipartUnsupportedShape(typeName, propName));
                continue;
            }
            else
            {
                // Scalar, IFormFile, IReadOnlyList<IFormFile>, or referenced scalar/enum.
                csharpTypeName = TypeMapper.MapFormFieldSchema(
                    propSchema,
                    nullable: nullable,
                    contractsNamespace: contractsNamespace,
                    resolveReference: referenceName => directionality.ResolveSchemaReference(referenceName, SchemaGenerationScope.Neutral));
            }

            sb.AppendLine($"        [global::Microsoft.AspNetCore.Mvc.FromForm(Name = \"{propName}\")]");

            if (isRequired && !nullable)
                sb.AppendLine($"        public required {csharpTypeName} {csharpName} {{ get; init; }}");
            else
                sb.AppendLine($"        public {csharpTypeName} {csharpName} {{ get; init; }}");
        }

        sb.AppendLine("    }");
    }

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="propSchema"/> has a shape that
    /// cannot be bound from <c>multipart/form-data</c>: arrays of complex objects or
    /// dictionary types.
    /// </summary>
    private static bool IsUnsupportedMultipartFormShape(
        OpenApiSchema propSchema,
        IReadOnlyDictionary<string, OpenApiSchema> allSchemas)
    {
        // Array of complex objects cannot be bound from form data.
        if (string.Equals(propSchema.Type, "array", StringComparison.OrdinalIgnoreCase)
            && propSchema.Items is not null)
        {
            var items = propSchema.Items;
            if (TypeMapper.IsInlineObject(items))
                return true;
            if (items.Reference is not null
                && allSchemas.TryGetValue(items.Reference, out var refItemSchema)
                && TypeMapper.IsInlineObject(refItemSchema))
                return true;
        }

        // Dictionary types cannot be bound from form data.
        if (TypeMapper.IsDictionarySchema(propSchema))
            return true;

        return false;
    }

    /// <summary>
    /// Appends the nested <c>Parameters</c> sealed record aggregating all non-path parameters
    /// (query, header and cookie) so they can be bound via <c>[AsParameters]</c> without
    /// breaking existing handler implementations when new optional parameters are added.
    /// </summary>
    private static void AppendParametersRecord(StringBuilder sb, List<OpenApiParameter> parameters, string contractsNamespace, Func<string, string>? resolveReference = null)
    {
        sb.AppendLine("    /// <summary>Aggregates all query, header and cookie parameters for this operation.</summary>");
        TypeMapper.AppendGeneratedAttributes(sb, "    ");
        sb.AppendLine("    public sealed record Parameters");
        sb.AppendLine("    {");

        foreach (var p in parameters)
        {
            var type = TypeMapper.MapSchema(p.Schema, nullable: !p.Required, contractsNamespace: contractsNamespace, resolveReference: resolveReference);
            var propName = TypeMapper.ToPascalCase(p.Name);
            var bindingAttr = GetBindingAttribute(p);
            if (bindingAttr is not null)
                sb.AppendLine($"        {bindingAttr}");
            foreach (var attr in ValidationAttributeEmitter.GetAttributes(p.Schema, "        "))
                sb.AppendLine(attr);
            var defaultInitializer = GetDefaultInitializer(p.Schema);
            if (defaultInitializer is not null)
                sb.AppendLine($"        public {type} {propName} {{ get; init; }} = {defaultInitializer};");
            else if (p.Required && !type.EndsWith("?"))
                sb.AppendLine($"        public {type} {propName} {{ get; init; }} = default!;");
            else
                sb.AppendLine($"        public {type} {propName} {{ get; init; }}");
        }

        sb.AppendLine("    }");
    }

    private static string? GetBindingAttribute(OpenApiParameter p) => p.Location switch
    {
        ParameterLocation.Query => $"[global::Microsoft.AspNetCore.Mvc.FromQuery(Name = \"{p.Name}\")]",
        ParameterLocation.Header => $"[global::Microsoft.AspNetCore.Mvc.FromHeader(Name = \"{p.Name}\")]",
        // Cookie parameters have no dedicated binding attribute in ASP.NET Core minimal APIs;
        // they are included in the Parameters record without auto-binding so that the handler
        // signature stays stable. Implementations can access cookie values via HttpContext.
        ParameterLocation.Cookie => null,
        _ => null
    };

    /// <summary>
    /// Returns a C# default-value expression for a parameter's <c>default</c> keyword, or
    /// <see langword="null"/> when the default cannot be safely represented as a literal.
    /// </summary>
    private static string? GetDefaultInitializer(OpenApiSchema schema)
    {
        if (schema.Default is null) return null;
        if (schema.Reference is not null) return null; // $ref parameters — skip

        var raw = schema.Default;
        var type = schema.Type?.ToLowerInvariant();
        var format = schema.Format?.ToLowerInvariant();

        if (type == "string")
        {
            if (format == "uuid")
            {
                return System.Guid.TryParse(raw, out var guid)
                    ? $"global::System.Guid.Parse(\"{guid}\")"
                    : null;
            }

            if (format == "date")
            {
                // Parse and validate the date strictly so the emitted initializer never throws.
                // DateTime.TryParseExact is available in netstandard2.0 (unlike DateOnly.TryParse);
                // we use it purely for validation and then emit a constructor call rather than
                // a Parse call so that the generated code itself is non-throwing.
                if (!System.DateTime.TryParseExact(
                        raw,
                        "yyyy-MM-dd",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out var dt))
                    return null;
                return $"new global::System.DateOnly({dt.Year}, {dt.Month}, {dt.Day})";
            }

            return $"\"{EscapeStringLiteral(raw)}\"";
        }

        if (type == "integer")
        {
            if (format == "int64")
                return long.TryParse(raw, out _) ? raw + "L" : null;
            return int.TryParse(raw, out _) ? raw : null;
        }

        if (type == "number")
        {
            if (!double.TryParse(raw, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var d))
                return null;

            if (format == "float")
                return FormatFloat((float)d);

            return FormatDouble(d);
        }

        if (type == "boolean")
        {
            if (raw.Equals("true", StringComparison.OrdinalIgnoreCase)) return "true";
            if (raw.Equals("false", StringComparison.OrdinalIgnoreCase)) return "false";
        }

        return null;
    }

    private static string EscapeStringLiteral(string value)
    {
        var sb = new StringBuilder(value.Length + 4);
        foreach (var c in value)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (char.IsControl(c))
                        sb.Append($"\\u{(int)c:x4}");
                    else
                        sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    private static string FormatFloat(float value)
    {
        if (value == (float)Math.Floor(value))
            return ((long)value).ToString(System.Globalization.CultureInfo.InvariantCulture) + "f";
        return value.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "f";
    }

    private static string FormatDouble(double value)
    {
        if (value == Math.Floor(value)
            && value > (double)long.MinValue
            && value < (double)long.MaxValue)
        {
            return ((long)value).ToString(System.Globalization.CultureInfo.InvariantCulture) + ".0";
        }
        return value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void AppendProblemResultWrapper(
        StringBuilder sb,
        OpenApiResponse response,
        string contractsNamespace,
        InlineSchemaResolver? resolveInline = null)
    {
        var payloadType = response.Schema is not null
            ? TypeMapper.MapSchema(response.Schema, contractsNamespace: contractsNamespace, resolveInline: resolveInline)
            : "global::Microsoft.AspNetCore.Mvc.ProblemDetails";
        var resultTypeName = TypeMapper.GetProblemResultTypeName(response.StatusCode);
        var statusCodeExpression = TypeMapper.GetStatusCodeExpression(response.StatusCode);

        sb.AppendLine($"    /// <summary>Typed <c>application/problem+json</c> result for HTTP {response.StatusCode}.</summary>");
        TypeMapper.AppendGeneratedAttributes(sb, "    ");
        sb.AppendLine(
            $"    public sealed class {resultTypeName} : global::Microsoft.AspNetCore.Http.IResult, global::Microsoft.AspNetCore.Http.IStatusCodeHttpResult, global::Microsoft.AspNetCore.Http.IValueHttpResult, global::Microsoft.AspNetCore.Http.IValueHttpResult<{payloadType}>");
        sb.AppendLine("    {");
        sb.AppendLine($"        public {resultTypeName}({payloadType} value)");
        sb.AppendLine("        {");
        sb.AppendLine("            Value = value;");
        if (response.Schema is null)
        {
            sb.AppendLine(
                $"            Value.Status ??= {statusCodeExpression};");
        }
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        public int? StatusCode => {statusCodeExpression};");
        sb.AppendLine();
        sb.AppendLine("        object? global::Microsoft.AspNetCore.Http.IValueHttpResult.Value => Value;");
        sb.AppendLine();
        sb.AppendLine($"        public {payloadType} Value {{ get; }}");
        sb.AppendLine();
        sb.AppendLine("        public global::System.Threading.Tasks.Task ExecuteAsync(global::Microsoft.AspNetCore.Http.HttpContext httpContext)");
        sb.AppendLine("        {");
        sb.AppendLine($"            httpContext.Response.StatusCode = {statusCodeExpression};");
        sb.AppendLine();
        sb.AppendLine("            return global::Microsoft.AspNetCore.Http.HttpResponseJsonExtensions.WriteAsJsonAsync(");
        sb.AppendLine("                httpContext.Response,");
        sb.AppendLine("                Value,");
        sb.AppendLine("                options: null,");
        sb.AppendLine("                contentType: \"application/problem+json\");");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
    }

    /// <summary>
    /// Appends a status-specific file result wrapper for a binary/file response. The wrapper
    /// fixes the declared status code and, for a concrete media type, the content type; for a
    /// wildcard media type (<c>image/*</c>, <c>*/*</c>) the handler supplies a concrete content
    /// type, which is validated against the declared pattern. Execution delegates to the
    /// built-in ASP.NET Core stream or file-content result.
    /// </summary>
    private static void AppendFileResultWrapper(StringBuilder sb, OpenApiResponse response)
    {
        const string http = "global::Microsoft.AspNetCore.Http";
        const string stream = "global::System.IO.Stream";
        const string dateTimeOffset = "global::System.DateTimeOffset";
        const string entityTag = "global::Microsoft.Net.Http.Headers.EntityTagHeaderValue";

        var declaredMediaType = response.ContentType!;
        var essence = TypeMapper.GetMediaTypeEssence(declaredMediaType);
        var slash = essence.IndexOf('/');
        var wildcardType = slash > 0 && essence.Substring(slash + 1) == "*"
            ? essence.Substring(0, slash)
            : null;
        var isWildcard = wildcardType is not null;

        var resultTypeName = TypeMapper.GetFileResultTypeName(response.StatusCode);
        var statusCodeExpression = TypeMapper.GetStatusCodeExpression(response.StatusCode);
        var mediaTypeLiteral = $"\"{EscapeStringLiteral(declaredMediaType)}\"";
        var mediaTypeDoc = EscapeXml(declaredMediaType);

        var contentTypeParameter = isWildcard ? "string contentType, " : "";
        var contentTypeArgument = isWildcard ? "ValidateContentType(contentType)" : mediaTypeLiteral;
        var optionalParameters =
            $"string? fileDownloadName = null, {dateTimeOffset}? lastModified = null, {entityTag}? entityTag = null, bool enableRangeProcessing = false";
        var contentTypeParamDoc = isWildcard
            ? $"        /// <param name=\"contentType\">The concrete content type of the file. It must match the declared <c>{mediaTypeDoc}</c> media type.</param>"
            : null;

        sb.AppendLine($"    /// <summary>Typed <c>{mediaTypeDoc}</c> file result for HTTP {response.StatusCode}.</summary>");
        TypeMapper.AppendGeneratedAttributes(sb, "    ");
        sb.AppendLine(
            $"    public sealed class {resultTypeName} : {http}.IResult, {http}.IStatusCodeHttpResult, {http}.IContentTypeHttpResult, {http}.IFileHttpResult");
        sb.AppendLine("    {");

        // Stream constructor.
        sb.AppendLine("        /// <summary>Creates a result that writes <paramref name=\"fileStream\"/> to the response. The stream is disposed after the response is written.</summary>");
        sb.AppendLine("        /// <param name=\"fileStream\">The stream with the file content.</param>");
        if (contentTypeParamDoc is not null) sb.AppendLine(contentTypeParamDoc);
        AppendFileOptionalParameterDocs(sb);
        sb.AppendLine($"        public {resultTypeName}({stream} fileStream, {contentTypeParameter}{optionalParameters})");
        sb.AppendLine($"            : this(fileStream ?? throw new global::System.ArgumentNullException(nameof(fileStream)), null, {contentTypeArgument}, fileDownloadName, lastModified, entityTag, enableRangeProcessing)");
        sb.AppendLine("        {");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Byte-array constructor.
        sb.AppendLine("        /// <summary>Creates a result that writes <paramref name=\"fileContents\"/> to the response.</summary>");
        sb.AppendLine("        /// <param name=\"fileContents\">The file content.</param>");
        if (contentTypeParamDoc is not null) sb.AppendLine(contentTypeParamDoc);
        AppendFileOptionalParameterDocs(sb);
        sb.AppendLine($"        public {resultTypeName}(byte[] fileContents, {contentTypeParameter}{optionalParameters})");
        sb.AppendLine($"            : this(null, fileContents ?? throw new global::System.ArgumentNullException(nameof(fileContents)), {contentTypeArgument}, fileDownloadName, lastModified, entityTag, enableRangeProcessing)");
        sb.AppendLine("        {");
        sb.AppendLine("        }");
        sb.AppendLine();

        sb.AppendLine($"        private {resultTypeName}({stream}? fileStream, byte[]? fileContents, string contentType, string? fileDownloadName, {dateTimeOffset}? lastModified, {entityTag}? entityTag, bool enableRangeProcessing)");
        sb.AppendLine("        {");
        sb.AppendLine("            FileStream = fileStream;");
        sb.AppendLine("            FileContents = fileContents;");
        sb.AppendLine("            ContentType = contentType;");
        sb.AppendLine("            FileDownloadName = fileDownloadName;");
        sb.AppendLine("            LastModified = lastModified;");
        sb.AppendLine("            EntityTag = entityTag;");
        sb.AppendLine("            EnableRangeProcessing = enableRangeProcessing;");
        sb.AppendLine("        }");
        sb.AppendLine();

        sb.AppendLine("        /// <summary>The HTTP status code written to the response.</summary>");
        sb.AppendLine($"        public int? StatusCode => {statusCodeExpression};");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>The content type written to the response.</summary>");
        sb.AppendLine("        public string ContentType { get; }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>The file name for the <c>Content-Disposition</c> header, or <see langword=\"null\"/> for none.</summary>");
        sb.AppendLine("        public string? FileDownloadName { get; }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>The <c>Last-Modified</c> value used for conditional requests.</summary>");
        sb.AppendLine($"        public {dateTimeOffset}? LastModified {{ get; }}");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>The <c>ETag</c> value used for conditional requests.</summary>");
        sb.AppendLine($"        public {entityTag}? EntityTag {{ get; }}");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>Whether range requests are processed.</summary>");
        sb.AppendLine("        public bool EnableRangeProcessing { get; }");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>The stream with the file content, or <see langword=\"null\"/> when the result was created from a byte array.</summary>");
        sb.AppendLine($"        public {stream}? FileStream {{ get; }}");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>The file content, or <see langword=\"null\"/> when the result was created from a stream.</summary>");
        sb.AppendLine("        public byte[]? FileContents { get; }");
        sb.AppendLine();

        sb.AppendLine("        /// <inheritdoc />");
        sb.AppendLine($"        public global::System.Threading.Tasks.Task ExecuteAsync({http}.HttpContext httpContext)");
        sb.AppendLine("        {");
        sb.AppendLine("            global::System.ArgumentNullException.ThrowIfNull(httpContext);");
        sb.AppendLine();
        sb.AppendLine($"            httpContext.Response.StatusCode = {statusCodeExpression};");
        sb.AppendLine();
        sb.AppendLine($"            {http}.IResult result = FileStream is not null");
        sb.AppendLine($"                ? {http}.TypedResults.Stream(FileStream, contentType: ContentType, fileDownloadName: FileDownloadName, lastModified: LastModified, entityTag: EntityTag, enableRangeProcessing: EnableRangeProcessing)");
        sb.AppendLine($"                : {http}.TypedResults.File(FileContents!, contentType: ContentType, fileDownloadName: FileDownloadName, enableRangeProcessing: EnableRangeProcessing, lastModified: LastModified, entityTag: EntityTag);");
        sb.AppendLine();
        sb.AppendLine("            return result.ExecuteAsync(httpContext);");
        sb.AppendLine("        }");

        if (isWildcard)
        {
            sb.AppendLine();
            sb.AppendLine("        private static string ValidateContentType(string contentType)");
            sb.AppendLine("        {");
            sb.AppendLine("            global::System.ArgumentException.ThrowIfNullOrWhiteSpace(contentType);");
            sb.AppendLine();
            sb.AppendLine("            var essence = contentType.Split(';')[0].Trim();");
            sb.AppendLine("            var separator = essence.IndexOf('/');");
            sb.Append("            if (separator <= 0 || separator == essence.Length - 1 || essence.IndexOf('*') >= 0");
            if (wildcardType != "*")
            {
                sb.AppendLine();
                sb.Append($"                || !essence.StartsWith(\"{EscapeStringLiteral(wildcardType!)}/\", global::System.StringComparison.OrdinalIgnoreCase)");
            }
            sb.AppendLine(")");
            sb.AppendLine("            {");
            sb.AppendLine("                throw new global::System.ArgumentException(");
            sb.AppendLine($"                    \"Content type '\" + contentType + \"' does not match the declared response media type '{EscapeStringLiteral(declaredMediaType)}'.\",");
            sb.AppendLine("                    nameof(contentType));");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            return contentType;");
            sb.AppendLine("        }");
        }

        sb.AppendLine("    }");
    }

    private static void AppendFileOptionalParameterDocs(StringBuilder sb)
    {
        sb.AppendLine("        /// <param name=\"fileDownloadName\">The file name for the <c>Content-Disposition: attachment</c> header, or <see langword=\"null\"/> for none.</param>");
        sb.AppendLine("        /// <param name=\"lastModified\">The <c>Last-Modified</c> value used for conditional requests.</param>");
        sb.AppendLine("        /// <param name=\"entityTag\">The <c>ETag</c> value used for conditional requests.</param>");
        sb.AppendLine("        /// <param name=\"enableRangeProcessing\">Whether range requests are processed.</param>");
    }

    private static string EscapeXml(string value)
        => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}