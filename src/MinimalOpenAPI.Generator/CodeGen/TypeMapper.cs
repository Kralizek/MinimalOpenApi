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
    /// <summary>The tool name used in <c>[GeneratedCode]</c> attributes on all generated types.</summary>
    public const string GeneratorName = "MinimalOpenAPI.Generator";

    /// <summary>The tool version used in <c>[GeneratedCode]</c> attributes on all generated types.</summary>
    public const string GeneratorVersion = "1.0.0";

    /// <summary>
    /// Returns <see langword="true"/> when the schema is an inline object definition —
    /// i.e. it declares properties or an explicit <c>object</c> type, but has no <c>$ref</c>.
    /// </summary>
    public static bool IsInlineObject(OpenApiSchema schema)
        => schema.Ref is null
            && (schema.Type?.ToLowerInvariant() == "object" || schema.Properties.Count > 0);

    /// <summary>Returns the nested record name used for an inline request-body schema.</summary>
    public static string GetInlineRequestBodyTypeName() => "Request";

    /// <summary>Returns the nested record name used for an inline response schema with the given status code.</summary>
    public static string GetInlineResponseTypeName(int statusCode) => statusCode switch
    {
        200 => "OkResponse",
        201 => "CreatedResponse",
        202 => "AcceptedResponse",
        204 => "NoContentResponse",
        400 => "BadRequestResponse",
        401 => "UnauthorizedResponse",
        403 => "ForbiddenResponse",
        404 => "NotFoundResponse",
        409 => "ConflictResponse",
        422 => "UnprocessableEntityResponse",
        _ => $"Status{statusCode}Response"
    };

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
        if (schema.Ref is not null)
        {
            var typeName = contractsNamespace is not null
                ? $"global::{contractsNamespace}.{schema.Ref}"
                : schema.Ref;
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

        // Inline object schema: ask the resolver for the type name (e.g. a nested record).
        if (IsInlineObject(schema) && resolveInline is not null)
        {
            var resolved = resolveInline(schema);
            if (resolved is not null)
                return nullable ? $"{resolved}?" : resolved;
        }

        var baseType = (schema.Type?.ToLowerInvariant(), schema.Format?.ToLowerInvariant()) switch
        {
            ("string", "uuid") => "global::System.Guid",
            ("string", "date-time") => "global::System.DateTimeOffset",
            ("string", "date") => "string",
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

        return statusCode switch
        {
            200 => "global::Microsoft.AspNetCore.Http.HttpResults.Ok",
            201 => "global::Microsoft.AspNetCore.Http.HttpResults.Created",
            202 => "global::Microsoft.AspNetCore.Http.HttpResults.Accepted",
            204 => "global::Microsoft.AspNetCore.Http.HttpResults.NoContent",
            400 => "global::Microsoft.AspNetCore.Http.HttpResults.BadRequest",
            401 => "global::Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult",
            403 => "global::Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult",
            404 => "global::Microsoft.AspNetCore.Http.HttpResults.NotFound",
            409 => "global::Microsoft.AspNetCore.Http.HttpResults.Conflict",
            422 => "global::Microsoft.AspNetCore.Http.HttpResults.UnprocessableEntity",
            _ => "global::Microsoft.AspNetCore.Http.IResult"
        };
    }

    /// <summary>Builds the return type for a handler: Results&lt;T1, T2, ...&gt; or single type.</summary>
    public static string BuildReturnType(
        List<OpenApiResponse> responses,
        string? contractsNamespace = null,
        InlineSchemaResolver? resolveInline = null)
    {
        var types = responses
            .OrderBy(r => r.StatusCode)
            .Select(r => MapStatusCode(r.StatusCode, r.Schema, contractsNamespace, resolveInline))
            .Distinct()
            .ToList();

        return types.Count switch
        {
            0 => "global::Microsoft.AspNetCore.Http.IResult",
            1 => types[0],
            _ => $"global::Microsoft.AspNetCore.Http.HttpResults.Results<{string.Join(", ", types)}>"
        };
    }

    /// <summary>Converts an operationId to a PascalCase class name suffix (e.g. "getClient" → "GetClient").</summary>
    public static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToUpperInvariant(name[0]) + name.Substring(1);
    }

    /// <summary>Converts a parameter name to a valid C# identifier (camelCase).</summary>
    public static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
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
        foreach (var p in parameters.Where(p => p.In == ParameterLocation.Path))
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
}