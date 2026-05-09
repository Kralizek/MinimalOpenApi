using System.Globalization;

using MinimalOpenAPI.Abstractions.Models;

namespace MinimalOpenAPI.Generator.CodeGen;

/// <summary>
/// Resolves a C# type name for an inline (non-<c>$ref</c>) object schema.
/// Implementations differ by context: the handler base class uses the short nested type
/// name (e.g. <c>"Request"</c>), while the endpoint mapping uses the fully-qualified
/// outer-class path (e.g. <c>"global::Ns.Endpoints.CreateOrderEndpointBase.Request"</c>).
/// Returns <see langword="null"/> when the schema is not an inline object that this
/// resolver handles.
/// </summary>
internal delegate string? InlineSchemaResolver(OpenApiSchema schema);

/// <summary>
/// Shared utilities for mapping OpenAPI concepts to C# types and identifiers.
/// </summary>
internal static class TypeMapper
{
    internal sealed record HttpStatusCodeMapping(
        int StatusCode,
        string ResponseName,
        string? ProblemName,
        string? TypedResultName,
        string StatusCodeExpression);

    /// <summary>
    /// A synthetic type name used as a sentinel to mark a property that could not be
    /// resolved during <c>allOf</c> flattening due to incompatible definitions across branches.
    /// <see cref="MapSchema"/> maps any schema carrying this type to
    /// <c>global::System.Text.Json.JsonElement</c> so that the property is still usable
    /// (callers can inspect the raw JSON value) without implying arbitrary CLR object semantics.
    /// </summary>
    internal const string RawJsonSentinelType = "x-raw-json";

    /// <summary>The tool name used in <c>[GeneratedCode]</c> attributes on all generated types.</summary>
    public const string GeneratorName = "MinimalOpenAPI.Generator";

    /// <summary>The tool version used in <c>[GeneratedCode]</c> attributes on all generated types.</summary>
    public const string GeneratorVersion = "1.0.0";

    /// <summary>
    /// Returns <see langword="true"/> when the schema represents a free-form map —
    /// i.e. it has no named <c>properties</c> and either has a typed
    /// <c>additionalProperties</c> schema or has <c>additionalProperties: true</c>.
    /// Such schemas map to <c>Dictionary&lt;string, T&gt;</c> rather than a generated record.
    /// </summary>
    public static bool IsDictionarySchema(OpenApiSchema schema)
        => schema.Reference is null
            && schema.Properties.Count == 0
            && (schema.AdditionalProperties is not null || schema.AdditionalPropertiesAllowed);

    /// <summary>
    /// Returns <see langword="true"/> when the schema is an inline object definition —
    /// i.e. it declares properties, an explicit <c>object</c> type, or one or more
    /// <c>allOf</c> entries, but has no <c>$ref</c> and is not a pure dictionary schema
    /// (see <see cref="IsDictionarySchema"/>).
    /// </summary>
    public static bool IsInlineObject(OpenApiSchema schema)
        => schema.Reference is null
            && !IsDictionarySchema(schema)
            && (schema.Type?.ToLowerInvariant() == "object" || schema.Properties.Count > 0 || schema.AllOf.Count > 0);

    /// <summary>Returns the nested record name used for an inline request-body schema.</summary>
    public static string GetInlineRequestBodyTypeName() => "Request";

    /// <summary>Returns the nested record name used for an inline response schema with the given status code.</summary>
    public static string GetInlineResponseTypeName(int statusCode) => $"{GetHttpStatusCodeMapping(statusCode).ResponseName}Response";

    public static string GetProblemResultTypeName(int statusCode)
    {
        var mapping = GetHttpStatusCodeMapping(statusCode);
        return $"{mapping.ProblemName ?? $"Status{statusCode}"}Problem";
    }

    public static string GetStatusCodeExpression(int statusCode) => GetHttpStatusCodeMapping(statusCode).StatusCodeExpression;

    public static bool IsProblemResponse(OpenApiResponse response)
        => string.Equals(response.ContentType, "application/problem+json", StringComparison.OrdinalIgnoreCase);

    /// <summary>Returns the C# default-value expression for <paramref name="typeName"/>.</summary>
    public static string GetDefaultValue(string typeName) => typeName switch
    {
        "string" => "string.Empty",
        "bool" => "false",
        "int" => "0",
        "long" => "0",
        "float" => "0f",
        "double" => "0.0",
        "global::System.Guid" => "global::System.Guid.Empty",
        "global::System.DateTimeOffset" => "default",
        _ => "default!"
    };

    /// <summary>Maps an OpenAPI schema to the C# type name.</summary>
    /// <param name="schema">The schema to map.</param>
    /// <param name="nullable">Whether the resulting type should be nullable.</param>
    /// <param name="contractsNamespace">
    /// When provided, <c>$ref</c> schema names are qualified as
    /// <c>global::{contractsNamespace}.{Name}</c> so they resolve correctly from
    /// outside the Contracts namespace (e.g. in handler bases and endpoint mappings).
    /// </param>
    /// <param name="resolveInline">
    /// When provided, inline object schemas (those without a <c>$ref</c>) are passed
    /// to this delegate to obtain their C# type name (e.g. a nested record name).
    /// If the delegate returns <see langword="null"/>, the schema falls through to the
    /// normal primitive-type mapping.
    /// </param>
    public static string MapSchema(
        OpenApiSchema schema,
        bool nullable = false,
        string? contractsNamespace = null,
        InlineSchemaResolver? resolveInline = null)
    {
        // Explicit raw-JSON sentinel: emitted when allOf flattening encounters incompatible
        // property definitions. Map directly to JsonElement rather than falling through to
        // the default 'object' branch to be consistent with how we represent unknown JSON
        // shapes (e.g. additionalProperties: true).
        if (schema.Type == RawJsonSentinelType)
        {
            const string je = "global::System.Text.Json.JsonElement";
            return nullable || schema.Nullable ? $"{je}?" : je;
        }

        if (schema.Reference is not null)
        {
            var typeName = contractsNamespace is not null
                ? $"global::{contractsNamespace}.{schema.Reference}"
                : schema.Reference;
            return nullable ? $"{typeName}?" : typeName;
        }

        if (schema.Type?.ToLowerInvariant() == "array")
        {
            var itemType = schema.Items is not null
                ? MapSchema(schema.Items, contractsNamespace: contractsNamespace, resolveInline: resolveInline)
                : "object";
            var arrayType = $"{itemType}[]";
            return nullable ? $"{arrayType}?" : arrayType;
        }

        // Dictionary schema: no named properties AND either a typed additionalProperties schema
        // or additionalProperties:true (allow-any).
        if (IsDictionarySchema(schema))
        {
            string valueType;
            if (schema.AdditionalProperties is not null)
                valueType = MapSchema(schema.AdditionalProperties, contractsNamespace: contractsNamespace, resolveInline: resolveInline);
            else
                valueType = "global::System.Text.Json.JsonElement"; // additionalProperties: true — payloads are assumed to be JSON
            var dictType = $"global::System.Collections.Generic.Dictionary<string, {valueType}>";
            return nullable || schema.Nullable ? $"{dictType}?" : dictType;
        }

        // Inline object schema: ask the resolver for the type name (e.g. a nested record).
        if (IsInlineObject(schema) && resolveInline is not null)
        {
            var resolved = resolveInline(schema);
            if (resolved is not null)
                return nullable ? $"{resolved}?" : resolved;
        }

        // Inline enum schema: ask the resolver for the generated enum type name.
        if (schema.Enum is not null && resolveInline is not null)
        {
            var resolved = resolveInline(schema);
            if (resolved is not null)
                return nullable ? $"{resolved}?" : resolved;
        }

        var baseType = (schema.Type?.ToLowerInvariant(), schema.Format?.ToLowerInvariant()) switch
        {
            ("string", "uuid") => "global::System.Guid",
            ("string", "date-time") => "global::System.DateTimeOffset",
            ("string", "date") => "global::System.DateOnly",
            ("string", _) => "string",
            ("integer", "int64") => "long",
            ("integer", _) => "int",
            ("number", "float") => "float",
            ("number", _) => "double",
            ("boolean", _) => "bool",
            _ => "object"
        };

        if (nullable || schema.Nullable)
            return baseType + "?";

        return baseType;
    }

    /// <summary>Maps an HTTP status code to the TypedResults type name.</summary>
    public static string MapStatusCode(
        int statusCode,
        OpenApiSchema? schema,
        string? contractsNamespace = null,
        InlineSchemaResolver? resolveInline = null)
    {
        var responseType = schema is not null
            ? MapSchema(schema, contractsNamespace: contractsNamespace, resolveInline: resolveInline)
            : null;

        if (responseType is not null && responseType != "object")
        {
            return statusCode switch
            {
                200 => $"global::Microsoft.AspNetCore.Http.HttpResults.Ok<{responseType}>",
                201 => $"global::Microsoft.AspNetCore.Http.HttpResults.Created<{responseType}>",
                202 => $"global::Microsoft.AspNetCore.Http.HttpResults.Accepted<{responseType}>",
                400 => $"global::Microsoft.AspNetCore.Http.HttpResults.BadRequest<{responseType}>",
                _ => $"global::Microsoft.AspNetCore.Http.HttpResults.Ok<{responseType}>"
            };
        }

        var mapping = GetHttpStatusCodeMapping(statusCode);
        return mapping.TypedResultName is not null
            ? $"global::Microsoft.AspNetCore.Http.HttpResults.{mapping.TypedResultName}"
            : "global::Microsoft.AspNetCore.Http.IResult";
    }

    public static string MapResponseResultType(
        OpenApiResponse response,
        string? contractsNamespace = null,
        InlineSchemaResolver? resolveInline = null)
    {
        if (IsProblemResponse(response))
            return GetProblemResultTypeName(response.StatusCode);

        return MapStatusCode(response.StatusCode, response.Schema, contractsNamespace, resolveInline);
    }

    /// <summary>Builds the return type for a handler: Results&lt;T1, T2, ...&gt; or single type.</summary>
    public static string BuildReturnType(
        List<OpenApiResponse> responses,
        string? contractsNamespace = null,
        InlineSchemaResolver? resolveInline = null)
    {
        var types = responses
            .OrderBy(r => r.StatusCode)
            .Select(r => MapResponseResultType(r, contractsNamespace, resolveInline))
            .Distinct()
            .ToList();

        return types.Count switch
        {
            0 => "global::Microsoft.AspNetCore.Http.IResult",
            1 => types[0],
            _ => $"global::Microsoft.AspNetCore.Http.HttpResults.Results<{string.Join(", ", types)}>"
        };
    }

    /// <summary>Converts an OpenAPI enum value to a valid C# enum member name (PascalCase).</summary>
    /// <remarks>
    /// Word separators (<c>-</c>, <c>_</c>, space, <c>.</c>) produce PascalCase segments.
    /// Any remaining characters that are not valid in a C# identifier (letters, digits,
    /// or underscores) are stripped.  If the result starts with a digit it is prefixed
    /// with <c>Value</c> (e.g. <c>"0"</c> → <c>"Value0"</c>).  An empty or all-punctuation
    /// value falls back to <c>"Empty"</c>.
    /// </remarks>
    public static string ToEnumMemberName(string value)
    {
        if (string.IsNullOrEmpty(value)) return "Empty";

        // Split on common word separators and join as PascalCase.
        var parts = value.Split(new[] { '-', '_', ' ', '.' }, StringSplitOptions.RemoveEmptyEntries);
        var joined = string.Concat(Array.ConvertAll(parts, ToPascalCase));

        // Strip any character that is not a valid C# identifier character.
        var name = new string(System.Array.FindAll(joined.ToCharArray(),
            c => char.IsLetterOrDigit(c) || c == '_'));

        if (string.IsNullOrEmpty(name)) return "Empty";

        // C# identifiers cannot start with a digit; prefix with "Value" for readability.
        if (char.IsDigit(name[0]))
            return "Value" + name;

        return name;
    }

    /// <summary>Converts an operationId to a PascalCase class name suffix (e.g. "getClient" → "GetClient").</summary>
    /// <remarks>
    /// Handles camelCase, PascalCase, snake_case, and kebab-case inputs:
    /// word boundaries are detected at <c>_</c>, <c>-</c>, and lower-to-upper transitions.
    /// Each word segment is emitted with its first letter uppercased.
    /// </remarks>
    public static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        var sb = new System.Text.StringBuilder(name.Length);
        bool capitalizeNext = true;

        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];

            if (c == '_' || c == '-')
            {
                capitalizeNext = true;
                continue;
            }

            // Detect a camelCase / PascalCase word boundary: lower→upper transition.
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(name[i - 1]))
                capitalizeNext = true;

            sb.Append(capitalizeNext ? char.ToUpperInvariant(c) : c);
            capitalizeNext = false;
        }

        return sb.ToString();
    }

    /// <summary>Converts a parameter name to a valid C# identifier (camelCase).</summary>
    public static string ToCamelCase(string name)
    {
        var pascal = ToPascalCase(name);
        if (string.IsNullOrEmpty(pascal)) return pascal;
        return char.ToLowerInvariant(pascal[0]) + pascal.Substring(1);
    }

    /// <summary>Returns the handler base class name for an operation.</summary>
    public static string HandlerClassName(string operationId) =>
        ToPascalCase(operationId) + "EndpointBase";

    /// <summary>Returns the registration customizer class name for an operation.</summary>
    public static string RegistrationClassName(string operationId) =>
        ToPascalCase(operationId) + "EndpointRegistration";

    /// <summary>
    /// Emits the standard generated-code attribute lines that must appear on every
    /// generated type: <c>[ExcludeFromCodeCoverage]</c> and <c>[GeneratedCode]</c>.
    /// </summary>
    public static void AppendGeneratedAttributes(System.Text.StringBuilder sb, string indent = "")
    {
        sb.AppendLine($"{indent}[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
        sb.AppendLine($"{indent}[global::System.CodeDom.Compiler.GeneratedCode(\"{GeneratorName}\", \"{GeneratorVersion}\")]");
    }

    /// <summary>Builds a route with type constraints for path parameters.</summary>
    public static string BuildConstrainedRoute(string route, List<OpenApiParameter> parameters)
    {
        var result = route;
        foreach (var p in parameters.Where(p => p.Location == ParameterLocation.Path))
        {
            var constraint = GetRouteConstraint(p.Schema);
            if (constraint is not null)
            {
                result = result.Replace($"{{{p.Name}}}", $"{{{p.Name}:{constraint}}}");
            }
        }
        return result;
    }

    private static string? GetRouteConstraint(OpenApiSchema schema)
    {
        return (schema.Type?.ToLowerInvariant(), schema.Format?.ToLowerInvariant()) switch
        {
            ("string", "uuid") => "guid",
            ("integer", "int64") => "long",
            ("integer", _) => "int",
            _ => null
        };
    }

    private static HttpStatusCodeMapping GetHttpStatusCodeMapping(int statusCode) => statusCode switch
    {
        200 => new HttpStatusCodeMapping(200, "Ok", "Ok", "Ok", "global::Microsoft.AspNetCore.Http.StatusCodes.Status200OK"),
        201 => new HttpStatusCodeMapping(201, "Created", "Created", "Created", "global::Microsoft.AspNetCore.Http.StatusCodes.Status201Created"),
        202 => new HttpStatusCodeMapping(202, "Accepted", "Accepted", "Accepted", "global::Microsoft.AspNetCore.Http.StatusCodes.Status202Accepted"),
        204 => new HttpStatusCodeMapping(204, "NoContent", "NoContent", "NoContent", "global::Microsoft.AspNetCore.Http.StatusCodes.Status204NoContent"),
        302 => new HttpStatusCodeMapping(302, "Redirect", null, null, "global::Microsoft.AspNetCore.Http.StatusCodes.Status302Found"),
        400 => new HttpStatusCodeMapping(400, "BadRequest", "BadRequest", "BadRequest", "global::Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest"),
        401 => new HttpStatusCodeMapping(401, "Unauthorized", "Unauthorized", "UnauthorizedHttpResult", "global::Microsoft.AspNetCore.Http.StatusCodes.Status401Unauthorized"),
        403 => new HttpStatusCodeMapping(403, "Forbidden", "Forbidden", "ForbidHttpResult", "global::Microsoft.AspNetCore.Http.StatusCodes.Status403Forbidden"),
        404 => new HttpStatusCodeMapping(404, "NotFound", "NotFound", "NotFound", "global::Microsoft.AspNetCore.Http.StatusCodes.Status404NotFound"),
        409 => new HttpStatusCodeMapping(409, "Conflict", "Conflict", "Conflict", "global::Microsoft.AspNetCore.Http.StatusCodes.Status409Conflict"),
        422 => new HttpStatusCodeMapping(422, "UnprocessableEntity", "UnprocessableEntity", "UnprocessableEntity", "global::Microsoft.AspNetCore.Http.StatusCodes.Status422UnprocessableEntity"),
        500 => new HttpStatusCodeMapping(500, "InternalServerError", "InternalServerError", null, "global::Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError"),
        _ => new HttpStatusCodeMapping(statusCode, $"Status{statusCode}", null, null, statusCode.ToString(CultureInfo.InvariantCulture))
    };
}