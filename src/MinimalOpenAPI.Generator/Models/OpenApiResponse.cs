namespace MinimalOpenAPI.Generator.Models;

internal sealed class OpenApiResponse
{
    public int StatusCode { get; init; }
    public string Description { get; init; } = string.Empty;
    public OpenApiSchema? Schema { get; init; }
}
