namespace MinimalOpenAPI.Abstractions.Models;

/// <summary>Describes a single HTTP response that an operation can return.</summary>
public sealed class OpenApiResponse
{
    /// <summary>The HTTP status code (e.g. 200, 201, 404).</summary>
    public int StatusCode { get; init; }

    /// <summary>A human-readable description of the response.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// The selected response content/media type (for example <c>application/json</c> or <c>image/png</c>),
    /// or <see langword="null"/> if the response declares no content or none of its media types is supported.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Every media type declared under the response's <c>content</c>, in document order, whether or not
    /// it is supported. Empty when the response declares no content.
    /// </summary>
    public IReadOnlyList<string> DeclaredContentTypes { get; init; } = [];

    /// <summary>The schema of the response body, or <see langword="null"/> if the response has no body.</summary>
    public OpenApiSchema? Schema { get; init; }
}