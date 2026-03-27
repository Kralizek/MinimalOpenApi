using MinimalOpenAPI.Generator.Models;

namespace MinimalOpenAPI.Generator.Parser;

internal interface IOpenApiParser
{
    OpenApiDocument Parse(string content);
}
