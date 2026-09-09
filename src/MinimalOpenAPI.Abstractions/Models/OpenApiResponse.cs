namespace MinimalOpenAPI.Abstractions.Models;

/// <summary>Describes a single HTTP response that an operation can return.</summary>
public sealed class OpenApiResponse
{
    /// <summary>The HTTP status code (e.g. 200, 201, 404).</summary>
    public int StatusCode { get; init; }

    /// <summary>A human-readable description of the response.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>The selected response content/media type (for example <c>application/json</c>).</summary>
    public string? ContentType { get; init; }

    /// <summary>Whether a content map was declared, including an empty map or unsupported media types.</summary>
    public bool HasContent { get; init; }

    /// <summary>Whether the declared content map contains at least one media-type representation.</summary>
    public bool HasContentRepresentations { get; init; }

    /// <summary>The raw response reference, when the response is a reference object.</summary>
    public string? Reference { get; init; }

    /// <summary>The schema of the response body, or <see langword="null"/> if the response has no body.</summary>
    public OpenApiSchema? Schema { get; init; }
}