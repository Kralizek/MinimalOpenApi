namespace MinimalOpenAPI.Abstractions.Models;

public sealed class OpenApiOperation
{
    public string OperationId { get; init; } = string.Empty;
    public string HttpMethod { get; init; } = string.Empty;
    public string Route { get; init; } = string.Empty;
    public List<OpenApiParameter> Parameters { get; init; } = new List<OpenApiParameter>();
    public OpenApiRequestBody? RequestBody { get; init; }
    public List<OpenApiResponse> Responses { get; init; } = new List<OpenApiResponse>();
}
