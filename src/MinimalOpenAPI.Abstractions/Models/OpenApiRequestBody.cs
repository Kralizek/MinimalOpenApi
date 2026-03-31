namespace MinimalOpenAPI.Abstractions.Models;

/// <summary>Describes the body that the client must supply when calling an operation.</summary>
public sealed class OpenApiRequestBody
{
    /// <summary>Whether the request body must be present for the operation to succeed.</summary>
    public bool Required { get; init; }

    /// <summary>The schema describing the expected body content, or <see langword="null"/> if unspecified.</summary>
    public OpenApiSchema? Schema { get; init; }
}