namespace MinimalOpenAPI.Abstractions.Models;

public sealed class OpenApiRequestBody
{
    public bool Required { get; init; }
    public OpenApiSchema? Schema { get; init; }
}
