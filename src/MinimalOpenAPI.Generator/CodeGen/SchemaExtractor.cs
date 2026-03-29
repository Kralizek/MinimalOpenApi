using MinimalOpenAPI.Abstractions.Models;

namespace MinimalOpenAPI.Generator.CodeGen;

/// <summary>
/// Promotes inline object schemas defined inside <c>requestBody</c> or <c>responses</c>
/// into named component schemas, replacing them with <c>$ref</c> schemas.
/// This normalises the document so that all other generators can always treat
/// schemas as named references.
/// </summary>
internal static class SchemaExtractor
{
    /// <summary>
    /// Returns a new <see cref="OpenApiDocument"/> where any inline object schemas
    /// inside operation <c>requestBody</c> or <c>responses</c> have been promoted to
    /// named schemas and replaced by <c>$ref</c> schemas.
    /// Inline schemas that already exist by the generated name are left unchanged.
    /// </summary>
    public static OpenApiDocument NormalizeDocument(OpenApiDocument doc)
    {
        var schemas = new Dictionary<string, OpenApiSchema>(doc.Schemas, StringComparer.Ordinal);
        var operations = doc.Operations
            .Select(op => NormalizeOperation(op, schemas))
            .ToList();

        return new OpenApiDocument
        {
            Title = doc.Title,
            Version = doc.Version,
            Schemas = schemas,
            Operations = operations
        };
    }

    private static OpenApiOperation NormalizeOperation(
        OpenApiOperation op,
        Dictionary<string, OpenApiSchema> schemas)
    {
        var requestBody = op.RequestBody;
        if (requestBody?.Schema is { } reqSchema && IsInlineObject(reqSchema))
        {
            var name = TypeMapper.ToPascalCase(op.OperationId) + "Request";
            if (!schemas.ContainsKey(name))
                schemas[name] = reqSchema;
            requestBody = new OpenApiRequestBody
            {
                Required = requestBody.Required,
                Schema = new OpenApiSchema { Ref = name }
            };
        }

        var responses = op.Responses
            .Select(r =>
            {
                if (r.Schema is { } respSchema && IsInlineObject(respSchema))
                {
                    var name = TypeMapper.ToPascalCase(op.OperationId) + r.StatusCode + "Response";
                    if (!schemas.ContainsKey(name))
                        schemas[name] = respSchema;
                    return new OpenApiResponse
                    {
                        StatusCode = r.StatusCode,
                        Description = r.Description,
                        Schema = new OpenApiSchema { Ref = name }
                    };
                }
                return r;
            })
            .ToList();

        return new OpenApiOperation
        {
            OperationId = op.OperationId,
            HttpMethod = op.HttpMethod,
            Route = op.Route,
            Summary = op.Summary,
            Description = op.Description,
            Tags = op.Tags,
            Parameters = op.Parameters,
            RequestBody = requestBody,
            Responses = responses
        };
    }

    /// <summary>
    /// Returns <see langword="true"/> when the schema is an inline object definition
    /// (i.e. it has properties or an explicit <c>object</c> type, but no <c>$ref</c>).
    /// </summary>
    private static bool IsInlineObject(OpenApiSchema schema)
        => schema.Ref is null
            && (schema.Type?.ToLowerInvariant() == "object" || schema.Properties.Count > 0);
}
