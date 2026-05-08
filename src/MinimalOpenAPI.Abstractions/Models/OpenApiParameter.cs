namespace MinimalOpenAPI.Abstractions.Models;

/// <summary>Specifies where an OpenAPI parameter appears in the HTTP request.</summary>
public enum ParameterLocation
{
    /// <summary>The parameter is part of the URL path (e.g. <c>/items/{id}</c>).</summary>
    Path,
    /// <summary>The parameter is a URL query string value (e.g. <c>?page=1</c>).</summary>
    Query,
    /// <summary>The parameter is an HTTP request header.</summary>
    Header,
    /// <summary>The parameter is an HTTP cookie.</summary>
    Cookie
}

/// <summary>Describes a single parameter accepted by an OpenAPI operation.</summary>
public sealed class OpenApiParameter
{
    /// <summary>The name of the parameter as it appears in the OpenAPI specification.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Indicates where the parameter is found in the HTTP request.</summary>
    public ParameterLocation Location { get; init; }

    /// <summary>Whether the parameter must be present in the request.</summary>
    public bool Required { get; init; }

    /// <summary>The schema that describes the parameter's type and format.</summary>
    public OpenApiSchema Schema { get; init; } = new OpenApiSchema();

    /// <summary>
    /// When non-<see langword="null"/>, this parameter is an unresolved <c>$ref</c> entry from the
    /// OpenAPI parameters array.  The value is the raw reference string (e.g.
    /// <c>#/components/parameters/Page</c>).  The generator resolves these references against
    /// <see cref="OpenApiDocument.ComponentParameters"/> before code generation.
    /// </summary>
    public string? Reference { get; init; }
}