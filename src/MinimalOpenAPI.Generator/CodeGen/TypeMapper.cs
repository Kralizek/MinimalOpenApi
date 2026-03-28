using MinimalOpenAPI.Abstractions.Models;

namespace MinimalOpenAPI.Generator.CodeGen;

/// <summary>
/// Shared utilities for mapping OpenAPI concepts to C# types and identifiers.
/// </summary>
internal static class TypeMapper
{
    /// <summary>The tool name used in <c>[GeneratedCode]</c> attributes on all generated types.</summary>
    public const string GeneratorName = "MinimalOpenAPI.Generator";

    /// <summary>The tool version used in <c>[GeneratedCode]</c> attributes on all generated types.</summary>
    public const string GeneratorVersion = "1.0.0";

    /// <summary>Maps an OpenAPI schema to the C# type name.</summary>
    public static string MapSchema(OpenApiSchema schema, bool nullable = false)
    {
        if (schema.Ref is not null)
        {
            return nullable ? $"{schema.Ref}?" : schema.Ref;
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
    public static string MapStatusCode(int statusCode, OpenApiSchema? schema)
    {
        if (schema?.Ref is not null)
        {
            return statusCode switch
            {
                200 => $"global::Microsoft.AspNetCore.Http.HttpResults.Ok<{schema.Ref}>",
                201 => $"global::Microsoft.AspNetCore.Http.HttpResults.Created<{schema.Ref}>",
                202 => $"global::Microsoft.AspNetCore.Http.HttpResults.Accepted<{schema.Ref}>",
                400 => $"global::Microsoft.AspNetCore.Http.HttpResults.BadRequest<{schema.Ref}>",
                _ => $"global::Microsoft.AspNetCore.Http.HttpResults.Ok<{schema.Ref}>"
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
    public static string BuildReturnType(List<OpenApiResponse> responses)
    {
        var types = responses
            .OrderBy(r => r.StatusCode)
            .Select(r => MapStatusCode(r.StatusCode, r.Schema))
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
        ToPascalCase(operationId) + "Endpoint";

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
