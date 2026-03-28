using MinimalOpenAPI.Abstractions.Models;

namespace MinimalOpenAPI.Abstractions;

public interface IOpenApiParser
{
    OpenApiDocument Parse(string content);
}
