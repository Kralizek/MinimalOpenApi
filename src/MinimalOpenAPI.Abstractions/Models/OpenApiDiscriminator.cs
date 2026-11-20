namespace MinimalOpenAPI.Abstractions.Models;

/// <summary>Describes the discriminator metadata authored on an OpenAPI schema.</summary>
public sealed class OpenApiDiscriminator
{
    /// <summary>The discriminator property name.</summary>
    public string? PropertyName { get; init; }

    /// <summary>
    /// Explicit discriminator value-to-schema-reference mappings, preserving their authored values.
    /// </summary>
    public Dictionary<string, string> Mapping { get; init; } = new(StringComparer.Ordinal);
}