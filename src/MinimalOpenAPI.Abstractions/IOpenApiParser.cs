using MinimalOpenAPI.Abstractions.Models;

namespace MinimalOpenAPI.Abstractions;

/// <summary>Parses a raw OpenAPI document string into an <see cref="OpenApiDocument"/>.</summary>
public interface IOpenApiParser
{
    /// <summary>
    /// Returns <see langword="true"/> when this parser is able to handle the document described by
    /// <paramref name="request"/>.  Implementations declare their supported scope with a simple
    /// condition on <see cref="OpenApiParserRequest.Format"/> and, for version-targeted parsers, on
    /// <see cref="OpenApiParserRequest.Version"/>.  The call site is responsible for format and
    /// version detection before invoking this method.
    /// </summary>
    /// <param name="request">The pre-detected format and version of the OpenAPI document.</param>
    /// <returns><see langword="true"/> if this parser should be used; <see langword="false"/> otherwise.</returns>
    bool CanParse(OpenApiParserRequest request);

    /// <summary>
    /// Parses the given OpenAPI document content and returns a structured <see cref="OpenApiDocument"/>.
    /// Only called after <see cref="CanParse"/> has returned <see langword="true"/>.
    /// </summary>
    /// <param name="content">The raw text content of the OpenAPI document (JSON or YAML).</param>
    /// <param name="cancellationToken">A token to cancel the parsing operation.</param>
    /// <returns>A task that resolves to the parsed <see cref="OpenApiDocument"/>.</returns>
    System.Threading.Tasks.Task<OpenApiDocument> ParseAsync(string content, System.Threading.CancellationToken cancellationToken = default);
}