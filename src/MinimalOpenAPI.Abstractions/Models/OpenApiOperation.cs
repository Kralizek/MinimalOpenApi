namespace MinimalOpenAPI.Abstractions.Models;

/// <summary>Represents a single HTTP operation (path + method combination) from an OpenAPI document.</summary>
public sealed class OpenApiOperation
{
    /// <summary>The unique identifier for the operation, used for code generation (e.g. <c>listTodos</c>).</summary>
    public string OperationId { get; init; } = string.Empty;

    /// <summary>The HTTP method in uppercase (e.g. <c>GET</c>, <c>POST</c>).</summary>
    public string HttpMethod { get; init; } = string.Empty;

    /// <summary>The route path template (e.g. <c>/todos/{id}</c>).</summary>
    public string Route { get; init; } = string.Empty;

    /// <summary>A short, human-readable summary of the operation.</summary>
    public string? Summary { get; init; }

    /// <summary>A longer description of the operation.</summary>
    public string? Description { get; init; }

    /// <summary>Tags used to group operations in documentation and tooling.</summary>
    public List<string> Tags { get; init; } = new List<string>();

    /// <summary>The parameters accepted by the operation (path, query, header and cookie).</summary>
    public List<OpenApiParameter> Parameters { get; init; } = new List<OpenApiParameter>();

    /// <summary>The request body definition, or <see langword="null"/> if the operation has no body.</summary>
    public OpenApiRequestBody? RequestBody { get; init; }

    /// <summary>The possible responses the operation can return, keyed by HTTP status code.</summary>
    public List<OpenApiResponse> Responses { get; init; } = new List<OpenApiResponse>();
}