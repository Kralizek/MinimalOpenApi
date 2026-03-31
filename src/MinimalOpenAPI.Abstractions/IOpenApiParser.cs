using MinimalOpenAPI.Abstractions.Models;

namespace MinimalOpenAPI.Abstractions;

/// <summary>Parses a raw OpenAPI document string into an <see cref="OpenApiDocument"/>.</summary>
public interface IOpenApiParser
{
    /// <summary>
    /// Parses the given OpenAPI document content and returns a structured <see cref="OpenApiDocument"/>.
    /// </summary>
    /// <param name="content">The raw text content of the OpenAPI document (JSON or YAML).</param>
    /// <param name="cancellationToken">A token to cancel the parsing operation.</param>
    /// <returns>A task that resolves to the parsed <see cref="OpenApiDocument"/>.</returns>
    System.Threading.Tasks.Task<OpenApiDocument> ParseAsync(string content, System.Threading.CancellationToken cancellationToken = default);
}