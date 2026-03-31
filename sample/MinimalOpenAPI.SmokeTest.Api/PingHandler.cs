using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.SmokeTest.Api.Endpoints;

namespace MinimalOpenAPI.SmokeTest.Api;

/// <summary>Minimal concrete handler for the smoke-test ping endpoint.</summary>
public sealed class PingHandler : PingEndpointBase
{
    public override Task<Ok<string>> HandleAsync(CancellationToken cancellationToken)
        => Task.FromResult(TypedResults.Ok("pong"));
}
