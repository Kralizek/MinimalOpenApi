namespace MinimalOpenAPI.Abstractions.Models;

/// <summary>
/// Defines the OpenAPI specification versions that this library explicitly supports.
/// Parsers store the raw <see cref="System.Version"/> parsed from the document's <c>openapi</c> field on
/// <see cref="OpenApiDocument.OpenApiVersion"/>; callers can compare against these constants (by
/// <see cref="System.Version.Major"/> and <see cref="System.Version.Minor"/>) to decide how to handle
/// a document.  Any version not listed here is accepted and stored but will trigger a diagnostic warning.
/// </summary>
public static class KnownOpenApiVersions
{
    /// <summary>OpenAPI 3.0.x — uses the <c>nullable</c> keyword for optional-null fields.</summary>
    public static readonly Version V3_0 = new(3, 0);

    /// <summary>OpenAPI 3.1.x — full JSON Schema 2020-12 alignment; uses <c>type: [T, "null"]</c> for optional-null fields.</summary>
    public static readonly Version V3_1 = new(3, 1);
}