namespace MinimalOpenAPI.Generator.Models;

internal enum ParameterLocation
{
    Path,
    Query,
    Header,
    Cookie
}

internal sealed class OpenApiParameter
{
    public string Name { get; init; } = string.Empty;
    public ParameterLocation In { get; init; }
    public bool Required { get; init; }
    public OpenApiSchema Schema { get; init; } = new();
}
