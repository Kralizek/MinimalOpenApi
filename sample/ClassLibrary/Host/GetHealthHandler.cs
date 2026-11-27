using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.Samples.ClassLibrary.Host.Health.Contracts;
using MinimalOpenAPI.Samples.ClassLibrary.Host.Health.Endpoints;

namespace MinimalOpenAPI.Samples.ClassLibrary.Host;

/// <summary>Handles <c>GET /health</c>, declared by the host's own OpenAPI document.</summary>
public sealed class GetHealthHandler : GetHealthEndpointBase
{
    public override Task<Ok<Health.Contracts.Health>> HandleAsync(CancellationToken cancellationToken)
        => Task.FromResult(TypedResults.Ok(new Health.Contracts.Health { Status = "ok" }));
}