namespace MinimalOpenAPI.Generator.Models;

internal sealed class OpenApiRequestBody
{
    public bool Required { get; init; }
    public OpenApiSchema? Schema { get; init; }
}
