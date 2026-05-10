using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.Samples.SchemaPublishing.PublicApi.Endpoints;

namespace MinimalOpenAPI.Samples.SchemaPublishing;

/// <summary>Returns a simple public status response.</summary>
public sealed class GetStatusHandler : GetStatusEndpointBase
{
    public override Task<Ok<OkResponse>> HandleAsync(CancellationToken cancellationToken)
        => Task.FromResult(TypedResults.Ok(new OkResponse { Status = "ok", Version = "1.0.0" }));
}