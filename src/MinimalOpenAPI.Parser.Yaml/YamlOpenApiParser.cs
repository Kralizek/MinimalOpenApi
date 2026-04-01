using MinimalOpenAPI.Abstractions;
using MinimalOpenAPI.Abstractions.Models;

using YamlDotNet.RepresentationModel;

namespace MinimalOpenAPI.Parser.Yaml;

/// <summary>
/// Parses OpenAPI 3.x YAML documents using YamlDotNet.
/// </summary>
public sealed class YamlOpenApiParser : IOpenApiParser
{
    /// <inheritdoc/>
    public System.Threading.Tasks.Task<OpenApiDocument> ParseAsync(string content, System.Threading.CancellationToken cancellationToken = default)
    {
        var stream = new YamlStream();
        stream.Load(new StringReader(content));

        if (stream.Documents.Count == 0)
            return System.Threading.Tasks.Task.FromResult(new OpenApiDocument());

        var root = (YamlMappingNode)stream.Documents[0].RootNode;
        return System.Threading.Tasks.Task.FromResult(ExtractDocument(root));
    }

    private static OpenApiDocument ExtractDocument(YamlMappingNode root)
    {
        return new OpenApiDocument
        {
            OpenApiVersion = ParseVersion(GetString(root, "openapi")),
            Title = GetString(root, "info", "title") ?? string.Empty,
            Version = GetString(root, "info", "version") ?? "1.0.0",
            Schemas = ExtractSchemas(root),
            Operations = ExtractOperations(root)
        };
    }

    // ── Version detection ─────────────────────────────────────────────────

    private static Version? ParseVersion(string? versionString)
    {
        if (versionString is null) return null;
        return Version.TryParse(versionString, out var v) ? v : null;
    }

    // ── Schemas ───────────────────────────────────────────────────────────

    private static Dictionary<string, OpenApiSchema> ExtractSchemas(YamlMappingNode root)
    {
        var result = new Dictionary<string, OpenApiSchema>(StringComparer.Ordinal);
        var schemasNode = GetMapping(root, "components", "schemas");
        if (schemasNode is null) return result;

        foreach (var entry in schemasNode.Children)
        {
            result[Scalar(entry.Key)] = ExtractSchema((YamlMappingNode)entry.Value);
        }
        return result;
    }

    private static OpenApiSchema ExtractSchema(YamlMappingNode node)
    {
        var refValue = GetString(node, "$ref");
        if (refValue is not null)
            return new OpenApiSchema { Reference = ResolveRef(refValue) };

        var properties = new Dictionary<string, OpenApiSchema>(StringComparer.Ordinal);
        var propsNode = GetMapping(node, "properties");
        if (propsNode is not null)
        {
            foreach (var entry in propsNode.Children)
                properties[Scalar(entry.Key)] = ExtractSchema((YamlMappingNode)entry.Value);
        }

        var required = new List<string>();
        var requiredNode = GetSequence(node, "required");
        if (requiredNode is not null)
        {
            foreach (var item in requiredNode.Children)
                required.Add(Scalar(item));
        }

        List<string>? enumValues = null;
        var enumNode = GetSequence(node, "enum");
        if (enumNode is not null)
        {
            enumValues = new List<string>();
            foreach (var item in enumNode.Children)
                enumValues.Add(Scalar(item));
        }

        // OpenAPI 3.1 allows 'type' to be an array, e.g. ["string", "null"].
        // Normalise to a single type string + Nullable flag so the rest of the
        // pipeline can treat all versions uniformly.
        var (resolvedType, nullableFromTypeArray) = GetTypeInfo(node);

        return new OpenApiSchema
        {
            Type = resolvedType,
            Format = GetString(node, "format"),
            Nullable = GetBool(node, "nullable") || nullableFromTypeArray,
            Properties = properties,
            Required = required,
            Items = ExtractItemsSchema(node),
            Enum = enumValues
        };
    }

    private static OpenApiSchema? ExtractItemsSchema(YamlMappingNode node)
    {
        var itemsNode = GetMapping(node, "items");
        return itemsNode is not null ? ExtractSchema(itemsNode) : null;
    }

    // ── Operations ────────────────────────────────────────────────────────

    private static readonly string[] HttpMethods =
        { "get", "post", "put", "patch", "delete", "head", "options" };

    private static List<OpenApiOperation> ExtractOperations(YamlMappingNode root)
    {
        var result = new List<OpenApiOperation>();
        var pathsNode = GetMapping(root, "paths");
        if (pathsNode is null) return result;

        foreach (var pathEntry in pathsNode.Children)
        {
            var route = Scalar(pathEntry.Key);
            var pathItem = (YamlMappingNode)pathEntry.Value;

            foreach (var method in HttpMethods)
            {
                var opNode = GetMapping(pathItem, method);
                if (opNode is null) continue;

                result.Add(ExtractOperation(opNode, method.ToUpperInvariant(), route));
            }
        }

        return result;
    }

    private static OpenApiOperation ExtractOperation(
        YamlMappingNode opNode, string method, string route)
    {
        var tagsNode = GetSequence(opNode, "tags");
        var tags = new List<string>();
        if (tagsNode is not null)
        {
            foreach (var item in tagsNode.Children)
                tags.Add(Scalar(item));
        }

        return new OpenApiOperation
        {
            OperationId = GetString(opNode, "operationId") ?? BuildOperationId(method, route),
            HttpMethod = method,
            Route = route,
            Summary = GetString(opNode, "summary"),
            Description = GetString(opNode, "description"),
            Tags = tags,
            Parameters = ExtractParameters(opNode),
            RequestBody = ExtractRequestBody(opNode),
            Responses = ExtractResponses(opNode)
        };
    }

    private static List<OpenApiParameter> ExtractParameters(YamlMappingNode opNode)
    {
        var result = new List<OpenApiParameter>();
        var paramsNode = GetSequence(opNode, "parameters");
        if (paramsNode is null) return result;

        foreach (var item in paramsNode.Children)
        {
            var paramNode = (YamlMappingNode)item;
            var inStr = GetString(paramNode, "in") ?? "query";
            var location = inStr.ToLowerInvariant() switch
            {
                "path" => ParameterLocation.Path,
                "header" => ParameterLocation.Header,
                "cookie" => ParameterLocation.Cookie,
                _ => ParameterLocation.Query
            };

            var schemaNode = GetMapping(paramNode, "schema");
            result.Add(new OpenApiParameter
            {
                Name = GetString(paramNode, "name") ?? string.Empty,
                Location = location,
                Required = GetBool(paramNode, "required"),
                Schema = schemaNode is not null ? ExtractSchema(schemaNode) : new OpenApiSchema()
            });
        }

        return result;
    }

    private static OpenApiRequestBody? ExtractRequestBody(YamlMappingNode opNode)
    {
        var bodyNode = GetMapping(opNode, "requestBody");
        if (bodyNode is null) return null;

        var schemaNode = GetMapping(bodyNode, "content", "application/json", "schema");
        return new OpenApiRequestBody
        {
            Required = GetBool(bodyNode, "required"),
            Schema = schemaNode is not null ? ExtractSchema(schemaNode) : null
        };
    }

    private static List<OpenApiResponse> ExtractResponses(YamlMappingNode opNode)
    {
        var result = new List<OpenApiResponse>();
        var responsesNode = GetMapping(opNode, "responses");
        if (responsesNode is null) return result;

        foreach (var entry in responsesNode.Children)
        {
            if (!int.TryParse(Scalar(entry.Key), out var statusCode)) continue;

            var responseNode = (YamlMappingNode)entry.Value;
            var schemaNode = GetMapping(responseNode, "content", "application/json", "schema");
            result.Add(new OpenApiResponse
            {
                StatusCode = statusCode,
                Description = GetString(responseNode, "description") ?? string.Empty,
                Schema = schemaNode is not null ? ExtractSchema(schemaNode) : null
            });
        }

        return result;
    }

    // ── YamlDotNet helpers ────────────────────────────────────────────────

    /// <summary>
    /// Returns the resolved type name and whether the type array contained <c>"null"</c>.
    /// Handles both a plain scalar <c>type: string</c> (3.0) and the JSON Schema 2020-12
    /// array form <c>type: [string, "null"]</c> (3.1).
    /// </summary>
    private static (string? type, bool nullableFromTypeArray) GetTypeInfo(YamlMappingNode node)
    {
        if (!node.Children.TryGetValue(new YamlScalarNode("type"), out var typeNode))
            return (null, false);

        if (typeNode is YamlScalarNode scalar)
            return (scalar.Value, false);

        // OpenAPI 3.1 type array: e.g. [string, "null"]
        if (typeNode is YamlSequenceNode sequence)
        {
            var types = new List<string>();
            foreach (var child in sequence.Children)
            {
                if (child is YamlScalarNode s && s.Value is not null)
                    types.Add(s.Value);
            }
            var hasNull = types.Remove("null");
            return (types.Count > 0 ? types[0] : null, hasNull);
        }

        return (null, false);
    }

    private static string? GetString(YamlMappingNode node, params string[] path)
    {
        var current = (YamlNode)node;
        foreach (var key in path)
        {
            if (current is not YamlMappingNode mapping) return null;
            if (!mapping.Children.TryGetValue(new YamlScalarNode(key), out current!)) return null;
        }
        return current is YamlScalarNode scalar ? scalar.Value : null;
    }

    private static bool GetBool(YamlMappingNode node, string key)
    {
        var value = GetString(node, key);
        return value is "true" or "True" or "TRUE";
    }

    private static YamlMappingNode? GetMapping(YamlMappingNode node, params string[] path)
    {
        var current = (YamlNode)node;
        foreach (var key in path)
        {
            if (current is not YamlMappingNode mapping) return null;
            if (!mapping.Children.TryGetValue(new YamlScalarNode(key), out current!)) return null;
        }
        return current as YamlMappingNode;
    }

    private static YamlSequenceNode? GetSequence(YamlMappingNode node, string key)
    {
        if (!node.Children.TryGetValue(new YamlScalarNode(key), out var child)) return null;
        return child as YamlSequenceNode;
    }

    private static string Scalar(YamlNode node) =>
        node is YamlScalarNode s ? (s.Value ?? string.Empty) : string.Empty;

    // ── Utilities ─────────────────────────────────────────────────────────

    private static string BuildOperationId(string method, string route)
    {
        var parts = route.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        var name = string.Concat(Array.ConvertAll(parts, p =>
        {
            if (string.IsNullOrEmpty(p) || (p.StartsWith("{") && p.EndsWith("}")))
                return string.Empty;
            return char.ToUpperInvariant(p[0]) + p.Substring(1);
        }));
        return char.ToLowerInvariant(method[0]) + method.Substring(1).ToLowerInvariant() + name;
    }

    private static string ResolveRef(string refValue)
    {
        // '#/components/schemas/Client' → 'Client'
        var lastSlash = refValue.LastIndexOf('/');
        return lastSlash >= 0 ? refValue.Substring(lastSlash + 1) : refValue;
    }
}