using System.Text.Json;
using System.Text.Json.Nodes;

using MinimalOpenAPI.Abstractions;
using MinimalOpenAPI.Abstractions.Models;

namespace MinimalOpenAPI.Parser.Json;

/// <summary>
/// Parses OpenAPI 3.x JSON documents using System.Text.Json.
/// </summary>
public sealed class JsonOpenApiParser : IOpenApiParser
{
    /// <inheritdoc/>
    public System.Threading.Tasks.Task<OpenApiDocument> ParseAsync(string content, System.Threading.CancellationToken cancellationToken = default)
    {
        var root = JsonNode.Parse(content)?.AsObject();
        if (root is null)
            return System.Threading.Tasks.Task.FromResult(new OpenApiDocument());

        return System.Threading.Tasks.Task.FromResult(ExtractDocument(root));
    }

    private static OpenApiDocument ExtractDocument(JsonObject root)
    {
        return new OpenApiDocument
        {
            OpenApiVersion = DetectVersion(GetString(root, "openapi")),
            Title = GetString(root, "info", "title") ?? string.Empty,
            Version = GetString(root, "info", "version") ?? "1.0.0",
            Schemas = ExtractSchemas(root),
            Operations = ExtractOperations(root)
        };
    }

    // ── Version detection ─────────────────────────────────────────────────

    private static OpenApiVersion DetectVersion(string? versionString)
    {
        if (versionString is null) return OpenApiVersion.Unknown;

        if (versionString.StartsWith("3.0.", StringComparison.OrdinalIgnoreCase) || versionString == "3.0")
            return OpenApiVersion.V3_0;

        if (versionString.StartsWith("3.1.", StringComparison.OrdinalIgnoreCase) || versionString == "3.1")
            return OpenApiVersion.V3_1;

        return OpenApiVersion.Unknown;
    }

    // ── Schemas ───────────────────────────────────────────────────────────

    private static Dictionary<string, OpenApiSchema> ExtractSchemas(JsonObject root)
    {
        var result = new Dictionary<string, OpenApiSchema>(StringComparer.Ordinal);
        var schemasNode = GetObject(root, "components", "schemas");
        if (schemasNode is null) return result;

        foreach (var entry in schemasNode)
        {
            if (entry.Value?.AsObject() is { } schemaObj)
                result[entry.Key] = ExtractSchema(schemaObj);
        }
        return result;
    }

    private static OpenApiSchema ExtractSchema(JsonObject node)
    {
        var refValue = GetString(node, "$ref");
        if (refValue is not null)
            return new OpenApiSchema { Reference = ResolveRef(refValue) };

        var properties = new Dictionary<string, OpenApiSchema>(StringComparer.Ordinal);
        var propsNode = GetObject(node, "properties");
        if (propsNode is not null)
        {
            foreach (var entry in propsNode)
            {
                if (entry.Value?.AsObject() is { } propObj)
                    properties[entry.Key] = ExtractSchema(propObj);
            }
        }

        var required = new List<string>();
        var requiredNode = GetArray(node, "required");
        if (requiredNode is not null)
        {
            foreach (var item in requiredNode)
            {
                if (item?.GetValue<string>() is { } s)
                    required.Add(s);
            }
        }

        List<string>? enumValues = null;
        var enumNode = GetArray(node, "enum");
        if (enumNode is not null)
        {
            enumValues = new List<string>();
            foreach (var item in enumNode)
            {
                if (item?.GetValue<string>() is { } s)
                    enumValues.Add(s);
            }
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

    private static OpenApiSchema? ExtractItemsSchema(JsonObject node)
    {
        var itemsNode = GetObject(node, "items");
        return itemsNode is not null ? ExtractSchema(itemsNode) : null;
    }

    // ── Operations ────────────────────────────────────────────────────────

    private static readonly string[] HttpMethods =
        { "get", "post", "put", "patch", "delete", "head", "options" };

    private static List<OpenApiOperation> ExtractOperations(JsonObject root)
    {
        var result = new List<OpenApiOperation>();
        var pathsNode = GetObject(root, "paths");
        if (pathsNode is null) return result;

        foreach (var pathEntry in pathsNode)
        {
            var route = pathEntry.Key;
            if (pathEntry.Value?.AsObject() is not { } pathItem) continue;

            foreach (var method in HttpMethods)
            {
                var opNode = GetObject(pathItem, method);
                if (opNode is null) continue;

                result.Add(ExtractOperation(opNode, method.ToUpperInvariant(), route));
            }
        }

        return result;
    }

    private static OpenApiOperation ExtractOperation(JsonObject opNode, string method, string route)
    {
        var tags = new List<string>();
        var tagsNode = GetArray(opNode, "tags");
        if (tagsNode is not null)
        {
            foreach (var item in tagsNode)
            {
                if (item?.GetValue<string>() is { } s)
                    tags.Add(s);
            }
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

    private static List<OpenApiParameter> ExtractParameters(JsonObject opNode)
    {
        var result = new List<OpenApiParameter>();
        var paramsNode = GetArray(opNode, "parameters");
        if (paramsNode is null) return result;

        foreach (var item in paramsNode)
        {
            if (item?.AsObject() is not { } paramNode) continue;

            var inStr = GetString(paramNode, "in") ?? "query";
            var location = inStr.ToLowerInvariant() switch
            {
                "path" => ParameterLocation.Path,
                "header" => ParameterLocation.Header,
                "cookie" => ParameterLocation.Cookie,
                _ => ParameterLocation.Query
            };

            var schemaNode = GetObject(paramNode, "schema");
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

    private static OpenApiRequestBody? ExtractRequestBody(JsonObject opNode)
    {
        var bodyNode = GetObject(opNode, "requestBody");
        if (bodyNode is null) return null;

        var schemaNode = GetObject(bodyNode, "content", "application/json", "schema");
        return new OpenApiRequestBody
        {
            Required = GetBool(bodyNode, "required"),
            Schema = schemaNode is not null ? ExtractSchema(schemaNode) : null
        };
    }

    private static List<OpenApiResponse> ExtractResponses(JsonObject opNode)
    {
        var result = new List<OpenApiResponse>();
        var responsesNode = GetObject(opNode, "responses");
        if (responsesNode is null) return result;

        foreach (var entry in responsesNode)
        {
            if (!int.TryParse(entry.Key, out var statusCode)) continue;
            if (entry.Value?.AsObject() is not { } responseNode) continue;

            var schemaNode = GetObject(responseNode, "content", "application/json", "schema");
            result.Add(new OpenApiResponse
            {
                StatusCode = statusCode,
                Description = GetString(responseNode, "description") ?? string.Empty,
                Schema = schemaNode is not null ? ExtractSchema(schemaNode) : null
            });
        }

        return result;
    }

    // ── System.Text.Json helpers ──────────────────────────────────────────

    /// <summary>
    /// Returns the resolved type name and whether the type array contained <c>"null"</c>.
    /// Handles both a plain string <c>"type": "string"</c> (3.0) and the JSON Schema 2020-12
    /// array form <c>"type": ["string", "null"]</c> (3.1).
    /// </summary>
    private static (string? type, bool nullableFromTypeArray) GetTypeInfo(JsonObject node)
    {
        if (!node.TryGetPropertyValue("type", out var typeNode) || typeNode is null)
            return (null, false);

        if (typeNode.GetValueKind() == JsonValueKind.String)
            return (typeNode.GetValue<string>(), false);

        // OpenAPI 3.1 type array: e.g. ["string", "null"]
        if (typeNode is JsonArray typeArray)
        {
            var types = new List<string>();
            foreach (var item in typeArray)
            {
                if (item?.GetValueKind() == JsonValueKind.String)
                    types.Add(item.GetValue<string>());
            }
            var hasNull = types.Remove("null");
            return (types.Count > 0 ? types[0] : null, hasNull);
        }

        return (null, false);
    }

    private static string? GetString(JsonObject node, params string[] path)
    {
        JsonNode? current = node;
        foreach (var key in path)
        {
            if (current is not JsonObject obj) return null;
            if (!obj.TryGetPropertyValue(key, out current)) return null;
        }
        return current?.GetValue<string>();
    }

    private static bool GetBool(JsonObject node, string key)
    {
        if (!node.TryGetPropertyValue(key, out var value) || value is null)
            return false;
        return value.GetValueKind() == JsonValueKind.True;
    }

    private static JsonObject? GetObject(JsonObject node, params string[] path)
    {
        JsonNode? current = node;
        foreach (var key in path)
        {
            if (current is not JsonObject obj) return null;
            if (!obj.TryGetPropertyValue(key, out current)) return null;
        }
        return current as JsonObject;
    }

    private static JsonArray? GetArray(JsonObject node, string key)
    {
        if (!node.TryGetPropertyValue(key, out var value)) return null;
        return value as JsonArray;
    }

    // ── Utilities ─────────────────────────────────────────────────────────

    private static string BuildOperationId(string method, string route)
    {
        var parts = route.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        var name = string.Concat(Array.ConvertAll(parts, p =>
        {
            if (string.IsNullOrEmpty(p) || (p.StartsWith("{", StringComparison.Ordinal) && p.EndsWith("}", StringComparison.Ordinal)))
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