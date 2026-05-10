using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.Samples.GeneratedFiles.Openapi.Endpoints;

namespace MinimalOpenAPI.Samples.GeneratedFiles;

/// <summary>
/// Tiny handler for the GET /widgets/{id} endpoint.
/// The generated base class (<c>GetWidgetEndpointBase</c>) and its nested <c>OkResponse</c>
/// record are emitted by the MinimalOpenAPI source generator and visible in the
/// <c>Generated/</c> directory after building.
/// </summary>
public sealed class GetWidgetHandler : GetWidgetEndpointBase
{
    public override Task<Results<Ok<OkResponse>, NotFound>> HandleAsync(
        System.Guid id,
        CancellationToken cancellationToken)
    {
        var widget = new OkResponse { Id = id, Name = $"Widget-{id:N}" };
        return Task.FromResult<Results<Ok<OkResponse>, NotFound>>(TypedResults.Ok(widget));
    }
}