namespace MinimalOpenAPI.Abstractions.Models;

public sealed class OpenApiSchema
{
    public string? Type { get; init; }
    public string? Format { get; init; }
    public bool Nullable { get; init; }
    public string? Ref { get; init; }
    public OpenApiSchema? Items { get; init; }
    public Dictionary<string, OpenApiSchema> Properties { get; init; } = new Dictionary<string, OpenApiSchema>(StringComparer.Ordinal);
    public List<string> Required { get; init; } = new List<string>();
}