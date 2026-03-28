namespace MinimalOpenAPI.Abstractions.Models;

public enum ParameterLocation
{
    Path,
    Query,
    Header,
    Cookie
}

public sealed class OpenApiParameter
{
    public string Name { get; init; } = string.Empty;
    public ParameterLocation In { get; init; }
    public bool Required { get; init; }
    public OpenApiSchema Schema { get; init; } = new OpenApiSchema();
}
