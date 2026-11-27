using System.Text;

using MinimalOpenAPI.Abstractions.Models;

namespace MinimalOpenAPI.Generator.CodeGen;

/// <summary>
/// Emits the endpoint filter that enforces the OpenAPI <c>required</c> list of a
/// <c>multipart/form-data</c> request body before the handler runs.
/// <para>
/// ASP.NET Core form binding ignores both the OpenAPI <c>required</c> list and the C#
/// <c>required</c> modifier: a form without any of the record's keys binds to <see langword="null"/>,
/// and a missing member keeps its default value. The filter checks the presence of every required
/// field and file (recursing into nested objects, and into optional nested objects only when the
/// form contains any of their fields) and returns a <c>400</c> validation problem listing the missing
/// fields. When the form record has no required members, a form without any of its keys binds to
/// <see langword="null"/>; the filter replaces it with an empty record so handlers never receive
/// <see langword="null"/> for the non-nullable <c>Request</c> parameter.
/// </para>
/// </summary>
internal static class MultipartRequiredFieldsFilterEmitter
{
    /// <summary>Name of the generated helper that tests whether a form contains fields of a nested object.</summary>
    public const string HasFieldsWithPrefixHelperName = "HasFormFieldsWithPrefix";

    public static void Emit(
        StringBuilder sb,
        string endpointVariable,
        OpenApiSchema schema,
        int requestArgumentIndex,
        string requestTypeName,
        SchemaDirectionalityAnalysis directionality,
        IReadOnlyDictionary<string, OpenApiSchema> allSchemas)
    {
        var checks = new StringBuilder();
        AppendChecks(checks, schema, prefix: string.Empty, indent: "            ", directionality, allSchemas);
        var hasRequiredMembers = HasRequiredRecordMembers(schema, directionality, allSchemas);

        sb.AppendLine();
        sb.AppendLine("        // Enforce the OpenAPI 'required' list of the multipart form; ASP.NET Core form binding does not.");
        sb.AppendLine($"        global::Microsoft.AspNetCore.Http.EndpointFilterExtensions.AddEndpointFilter({endpointVariable}, static async (context, next) =>");
        sb.AppendLine("        {");
        sb.AppendLine("            if (!context.HttpContext.Request.HasFormContentType)");
        sb.AppendLine("                return await next(context);");
        sb.AppendLine();
        sb.AppendLine("            var form = await context.HttpContext.Request.ReadFormAsync(context.HttpContext.RequestAborted);");
        sb.AppendLine("            var errors = new global::System.Collections.Generic.Dictionary<string, string[]>(global::System.StringComparer.OrdinalIgnoreCase);");
        sb.Append(checks);
        sb.AppendLine();
        sb.AppendLine("            if (errors.Count > 0)");
        sb.AppendLine("                return global::Microsoft.AspNetCore.Http.TypedResults.ValidationProblem(errors);");
        if (!hasRequiredMembers)
        {
            sb.AppendLine();
            sb.AppendLine($"            context.Arguments[{requestArgumentIndex}] ??= new {requestTypeName}();");
        }

        sb.AppendLine();
        sb.AppendLine("            return await next(context);");
        sb.AppendLine("        });");
    }

    /// <summary>Emits the shared prefix helper used by the generated filters.</summary>
    public static void EmitHelpers(StringBuilder sb)
    {
        sb.AppendLine();
        sb.AppendLine("    private static bool " + HasFieldsWithPrefixHelperName + "(global::Microsoft.AspNetCore.Http.IFormCollection form, string prefix)");
        sb.AppendLine("    {");
        sb.AppendLine("        foreach (var key in form.Keys)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (key.StartsWith(prefix, global::System.StringComparison.OrdinalIgnoreCase))");
        sb.AppendLine("                return true;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        foreach (var file in form.Files)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (file.Name.StartsWith(prefix, global::System.StringComparison.OrdinalIgnoreCase))");
        sb.AppendLine("                return true;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        return false;");
        sb.AppendLine("    }");
    }

    private static void AppendChecks(
        StringBuilder sb,
        OpenApiSchema schema,
        string prefix,
        string indent,
        SchemaDirectionalityAnalysis directionality,
        IReadOnlyDictionary<string, OpenApiSchema> allSchemas)
    {
        foreach (var property in FormProperties(schema, directionality, allSchemas))
        {
            var fieldName = prefix + property.Name;
            var literal = Literal(fieldName);
            var error = $"errors[{literal}] = new[] {{ {Literal($"The {fieldName} field is required.")} }};";

            if (property.NestedObject is { } nested)
            {
                var nestedChecks = new StringBuilder();
                AppendChecks(nestedChecks, nested, fieldName + ".", indent + "    ", directionality, allSchemas);
                var hasFieldsCall = $"{HasFieldsWithPrefixHelperName}(form, {Literal(fieldName + ".")})";

                if (property.IsRequired)
                {
                    sb.AppendLine($"{indent}if (!{hasFieldsCall})");
                    sb.AppendLine($"{indent}    {error}");
                    if (nestedChecks.Length > 0)
                    {
                        sb.AppendLine($"{indent}else");
                        sb.AppendLine($"{indent}{{");
                        sb.Append(nestedChecks);
                        sb.AppendLine($"{indent}}}");
                    }
                }
                else if (nestedChecks.Length > 0)
                {
                    sb.AppendLine($"{indent}if ({hasFieldsCall})");
                    sb.AppendLine($"{indent}{{");
                    sb.Append(nestedChecks);
                    sb.AppendLine($"{indent}}}");
                }

                continue;
            }

            if (!property.IsRequired)
                continue;

            var missing = TypeMapper.IsFormFileSchema(property.Schema)
                ? $"form.Files.GetFile({literal}) is null"
                : TypeMapper.IsFormFileArraySchema(property.Schema)
                    ? $"form.Files.GetFiles({literal}).Count == 0"
                    : $"!form.ContainsKey({literal})";
            sb.AppendLine($"{indent}if ({missing})");
            sb.AppendLine($"{indent}    {error}");
        }
    }

    /// <summary>
    /// Whether the generated form record declares C# <c>required</c> members, so that it cannot be
    /// created empty. Mirrors the record emission in <see cref="HandlerBaseGenerator"/>.
    /// </summary>
    private static bool HasRequiredRecordMembers(
        OpenApiSchema schema,
        SchemaDirectionalityAnalysis directionality,
        IReadOnlyDictionary<string, OpenApiSchema> allSchemas)
        => FormProperties(schema, directionality, allSchemas).Any(p => p.IsRequired && !p.Schema.Nullable);

    private static IEnumerable<FormProperty> FormProperties(
        OpenApiSchema schema,
        SchemaDirectionalityAnalysis directionality,
        IReadOnlyDictionary<string, OpenApiSchema> allSchemas)
    {
        foreach (var entry in schema.Properties)
        {
            var propertySchema = entry.Value;
            if (!directionality.ShouldIncludeProperty(propertySchema, SchemaGenerationScope.Request))
                continue;

            OpenApiSchema? nested = null;
            if (propertySchema.Reference is not null)
                allSchemas.TryGetValue(propertySchema.Reference, out nested);
            else if (TypeMapper.IsInlineObject(propertySchema))
                nested = propertySchema;

            if (nested is not null && !TypeMapper.IsInlineObject(nested))
                nested = null;

            // Fields the form record omits (MOA011) cannot be enforced either.
            if (nested is null && HandlerBaseGenerator.IsUnsupportedMultipartFormShape(propertySchema, allSchemas))
                continue;

            var isRequired = schema.Required.Contains(entry.Key, StringComparer.OrdinalIgnoreCase);
            yield return new FormProperty(entry.Key, propertySchema, isRequired, nested);
        }
    }

    private static string Literal(string value)
        => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    private sealed record FormProperty(string Name, OpenApiSchema Schema, bool IsRequired, OpenApiSchema? NestedObject);
}