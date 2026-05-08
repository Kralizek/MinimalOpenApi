namespace MinimalOpenAPI.Abstractions.Models;

/// <summary>Represents a parsed OpenAPI document, containing all operations and shared schemas.</summary>
public sealed class OpenApiDocument
{
    /// <summary>
    /// The OpenAPI specification version parsed from the <c>openapi</c> field of the document,
    /// or <see langword="null"/> if the field is absent or its value cannot be parsed as a version.
    /// Compare <see cref="Version.Major"/> and <see cref="Version.Minor"/> against
    /// <see cref="KnownOpenApiVersions"/> to determine whether the version is explicitly supported.
    /// </summary>
    public Version? OpenApiVersion { get; init; }

    /// <summary>The title of the API, taken from the <c>info.title</c> field.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>The version of the API, taken from the <c>info.version</c> field.</summary>
    public string Version { get; init; } = "1.0.0";

    /// <summary>All operations discovered across all paths in the document.</summary>
    public List<OpenApiOperation> Operations { get; init; } = new List<OpenApiOperation>();

    /// <summary>Reusable schema components defined under <c>components/schemas</c>, keyed by schema name.</summary>
    public Dictionary<string, OpenApiSchema> Schemas { get; init; } = new Dictionary<string, OpenApiSchema>(StringComparer.Ordinal);

    /// <summary>Reusable parameter components defined under <c>components/parameters</c>, keyed by parameter name.</summary>
    public Dictionary<string, OpenApiParameter> ComponentParameters { get; init; } = new Dictionary<string, OpenApiParameter>(StringComparer.Ordinal);
}