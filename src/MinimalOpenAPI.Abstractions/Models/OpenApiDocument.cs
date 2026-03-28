namespace MinimalOpenAPI.Abstractions.Models;

public sealed class OpenApiDocument
{
    public string Title { get; init; } = string.Empty;
    public string Version { get; init; } = "1.0.0";
    public List<OpenApiOperation> Operations { get; init; } = new List<OpenApiOperation>();
    public Dictionary<string, OpenApiSchema> Schemas { get; init; } = new Dictionary<string, OpenApiSchema>(StringComparer.Ordinal);
}
