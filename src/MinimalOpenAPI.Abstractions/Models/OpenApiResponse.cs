namespace MinimalOpenAPI.Abstractions.Models;

public sealed class OpenApiResponse
{
    public int StatusCode { get; init; }
    public string Description { get; init; } = string.Empty;
    public OpenApiSchema? Schema { get; init; }
}