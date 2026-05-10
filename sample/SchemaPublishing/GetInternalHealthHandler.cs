using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.Samples.SchemaPublishing.InternalApi.Endpoints;

namespace MinimalOpenAPI.Samples.SchemaPublishing;

/// <summary>Returns an internal health response. This endpoint is not publicly documented.</summary>
public sealed class GetInternalHealthHandler : GetInternalHealthEndpointBase
{
    public override Task<Ok<OkResponse>> HandleAsync(CancellationToken cancellationToken)
        => Task.FromResult(TypedResults.Ok(new OkResponse { Status = "healthy" }));
}