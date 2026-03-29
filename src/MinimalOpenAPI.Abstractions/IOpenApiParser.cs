using MinimalOpenAPI.Abstractions.Models;

namespace MinimalOpenAPI.Abstractions;

public interface IOpenApiParser
{
    System.Threading.Tasks.Task<OpenApiDocument> ParseAsync(string content, System.Threading.CancellationToken cancellationToken = default);
}