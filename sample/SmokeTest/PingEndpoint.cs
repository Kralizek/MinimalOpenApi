using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.Samples.SmokeTest.Openapi.Endpoints;

namespace MinimalOpenAPI.Samples.SmokeTest;

/// <summary>Minimal concrete handler for the smoke-test ping endpoint.</summary>
public sealed class PingEndpoint : PingEndpointBase
{
    public override Task<Results<Ok<string>, ForbidHttpResult>> HandleAsync(CancellationToken cancellationToken)
        => Task.FromResult<Results<Ok<string>, ForbidHttpResult>>(TypedResults.Ok("pong"));
}
