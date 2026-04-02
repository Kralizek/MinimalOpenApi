namespace MinimalOpenAPI.Abstractions.Models;

/// <summary>Identifies the serialisation format of an OpenAPI document file.</summary>
public enum OpenApiFormat
{
    /// <summary>The format could not be determined (e.g. unrecognised file extension).</summary>
    Unknown,

    /// <summary>YAML format (<c>.yaml</c> / <c>.yml</c>).</summary>
    Yaml,

    /// <summary>JSON format (<c>.json</c>).</summary>
    Json,
}