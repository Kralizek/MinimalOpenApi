using MinimalOpenAPI.Generator.Models;

namespace MinimalOpenAPI.Generator.Parser;

/// <summary>
/// Minimal YAML parser for OpenAPI 3.x documents.
/// Handles the subset of YAML used in standard OpenAPI specs.
/// </summary>
internal sealed class YamlOpenApiParser : IOpenApiParser
{
    public OpenApiDocument Parse(string content)
    {
        var root = YamlNode.Parse(content);
        return ExtractDocument(root);
    }

    private static OpenApiDocument ExtractDocument(YamlNode root)
    {
        var doc = new OpenApiDocument
        {
            Title = root.GetString("info", "title") ?? string.Empty,
            Version = root.GetString("info", "version") ?? "1.0.0",
            Schemas = ExtractSchemas(root),
            Operations = ExtractOperations(root)
        };
        return doc;
    }

    private static Dictionary<string, OpenApiSchema> ExtractSchemas(YamlNode root)
    {
        var result = new Dictionary<string, OpenApiSchema>(StringComparer.Ordinal);
        var schemas = root.GetNode("components", "schemas");
        if (schemas is null) return result;

        foreach (var kvp in schemas.Mapping)
        {
            result[kvp.Key] = ExtractSchema(kvp.Value);
        }
        return result;
    }

    private static OpenApiSchema ExtractSchema(YamlNode node)
    {
        var refVal = node.GetString("$ref");
        if (refVal is not null)
        {
            return new OpenApiSchema { Ref = ResolveRef(refVal) };
        }

        var properties = new Dictionary<string, OpenApiSchema>(StringComparer.Ordinal);
        var propsNode = node.GetNode("properties");
        if (propsNode is not null)
        {
        foreach (var kvp in propsNode.Mapping)
        {
            properties[kvp.Key] = ExtractSchema(kvp.Value);
        }
        }

        var required = new List<string>();
        var requiredNode = node.GetNode("required");
        if (requiredNode is not null)
        {
            foreach (var item in requiredNode.Sequence)
            {
                if (item.Scalar is not null) required.Add(item.Scalar);
            }
        }

        return new OpenApiSchema
        {
            Type = node.GetString("type"),
            Format = node.GetString("format"),
            Nullable = node.GetBool("nullable"),
            Properties = properties,
            Required = required
        };
    }

    private static List<OpenApiOperation> ExtractOperations(YamlNode root)
    {
        var result = new List<OpenApiOperation>();
        var pathsNode = root.GetNode("paths");
        if (pathsNode is null) return result;

        var httpMethods = new[] { "get", "post", "put", "patch", "delete", "head", "options" };

        foreach (var kvp in pathsNode.Mapping)
        {
            var route = kvp.Key;
            var pathItem = kvp.Value;
            foreach (var method in httpMethods)
            {
                var opNode = pathItem.GetNode(method);
                if (opNode is null) continue;

                var operation = ExtractOperation(opNode, method.ToUpperInvariant(), route);
                result.Add(operation);
            }
        }

        return result;
    }

    private static OpenApiOperation ExtractOperation(YamlNode opNode, string method, string route)
    {
        var operationId = opNode.GetString("operationId") ?? BuildOperationId(method, route);
        var parameters = ExtractParameters(opNode);
        var requestBody = ExtractRequestBody(opNode);
        var responses = ExtractResponses(opNode);

        return new OpenApiOperation
        {
            OperationId = operationId,
            HttpMethod = method,
            Route = route,
            Parameters = parameters,
            RequestBody = requestBody,
            Responses = responses
        };
    }

    private static List<OpenApiParameter> ExtractParameters(YamlNode opNode)
    {
        var result = new List<OpenApiParameter>();
        var paramsNode = opNode.GetNode("parameters");
        if (paramsNode is null) return result;

        foreach (var paramNode in paramsNode.Sequence)
        {
            var name = paramNode.GetString("name") ?? string.Empty;
            var inStr = paramNode.GetString("in") ?? "query";
            var required = paramNode.GetBool("required");
            var schema = ExtractSchema(paramNode.GetNode("schema") ?? YamlNode.Empty());

            var location = inStr.ToLowerInvariant() switch
            {
                "path" => ParameterLocation.Path,
                "header" => ParameterLocation.Header,
                "cookie" => ParameterLocation.Cookie,
                _ => ParameterLocation.Query
            };

            result.Add(new OpenApiParameter
            {
                Name = name,
                In = location,
                Required = required,
                Schema = schema
            });
        }

        return result;
    }

    private static OpenApiRequestBody? ExtractRequestBody(YamlNode opNode)
    {
        var bodyNode = opNode.GetNode("requestBody");
        if (bodyNode is null) return null;

        var required = bodyNode.GetBool("required");
        var schema = bodyNode.GetNode("content", "application/json", "schema");
        return new OpenApiRequestBody
        {
            Required = required,
            Schema = schema is not null ? ExtractSchema(schema) : null
        };
    }

    private static List<OpenApiResponse> ExtractResponses(YamlNode opNode)
    {
        var result = new List<OpenApiResponse>();
        var responsesNode = opNode.GetNode("responses");
        if (responsesNode is null) return result;

        foreach (var kvp in responsesNode.Mapping)
        {
            var code = kvp.Key;
            var responseNode = kvp.Value;
            if (!int.TryParse(code, out var statusCode)) continue;
            var description = responseNode.GetString("description") ?? string.Empty;
            var schema = responseNode.GetNode("content", "application/json", "schema");
            result.Add(new OpenApiResponse
            {
                StatusCode = statusCode,
                Description = description,
                Schema = schema is not null ? ExtractSchema(schema) : null
            });
        }

        return result;
    }

    private static string BuildOperationId(string method, string route)
    {
        var parts = route.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        var name = string.Concat(parts.Select(p =>
        {
            if (string.IsNullOrEmpty(p) || (p.StartsWith("{") && p.EndsWith("}")))
                return string.Empty;
            return char.ToUpperInvariant(p[0]) + p.Substring(1);
        }));
        return method.ToLowerInvariant() + name;
    }

    private static string ResolveRef(string refValue)
    {
        // '#/components/schemas/Client' → 'Client'
        var lastSlash = refValue.LastIndexOf('/');
        return lastSlash >= 0 ? refValue.Substring(lastSlash + 1) : refValue;
    }
}
