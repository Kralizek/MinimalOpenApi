namespace MinimalOpenAPI.Abstractions.Models;

/// <summary>Identifies the OpenAPI specification version detected from the <c>openapi</c> field of a document.</summary>
public enum OpenApiVersion
{
    /// <summary>The version could not be determined (field absent, malformed, or unrecognised).</summary>
    Unknown,

    /// <summary>OpenAPI 3.0.x (uses the <c>nullable</c> keyword for optional-null fields).</summary>
    V3_0,

    /// <summary>OpenAPI 3.1.x (full JSON Schema 2020-12 alignment; uses <c>type: [T, "null"]</c> for optional-null fields).</summary>
    V3_1,
}