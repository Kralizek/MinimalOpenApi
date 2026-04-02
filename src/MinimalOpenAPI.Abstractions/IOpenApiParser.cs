using MinimalOpenAPI.Abstractions.Models;

namespace MinimalOpenAPI.Abstractions;

/// <summary>Parses a raw OpenAPI document string into an <see cref="OpenApiDocument"/>.</summary>
public interface IOpenApiParser
{
    /// <summary>
    /// Returns <see langword="true"/> when this parser is able to handle the given file.
    /// Implementations should inspect the file extension for format compatibility and may
    /// also do a lightweight content peek to check the <c>openapi</c> version field when
    /// they only support a specific version range.
    /// </summary>
    /// <param name="filePath">The full path of the OpenAPI file (used to check the extension).</param>
    /// <param name="content">The raw text content of the OpenAPI file (may be peeked for version).</param>
    /// <returns><see langword="true"/> if this parser should be used; <see langword="false"/> otherwise.</returns>
    bool CanParse(string filePath, string content);

    /// <summary>
    /// Parses the given OpenAPI document content and returns a structured <see cref="OpenApiDocument"/>.
    /// Only called after <see cref="CanParse"/> has returned <see langword="true"/>.
    /// </summary>
    /// <param name="content">The raw text content of the OpenAPI document (JSON or YAML).</param>
    /// <param name="cancellationToken">A token to cancel the parsing operation.</param>
    /// <returns>A task that resolves to the parsed <see cref="OpenApiDocument"/>.</returns>
    System.Threading.Tasks.Task<OpenApiDocument> ParseAsync(string content, System.Threading.CancellationToken cancellationToken = default);
}