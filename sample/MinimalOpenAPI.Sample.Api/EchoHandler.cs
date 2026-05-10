using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.Sample.Api.Openapi.Endpoints;

namespace MinimalOpenAPI.Sample.Api;

/// <summary>
/// Echoes the <c>X-Correlation-Id</c> header value back in the response body.
/// Exists solely to provide a generated header-parameter endpoint for integration testing.
/// </summary>
public sealed class EchoHandler : EchoEndpointBase
{
    public override Task<Ok<string>> HandleAsync(
        EchoEndpointBase.Parameters parameters,
        CancellationToken cancellationToken)
    {
        // Return the header value (or empty string when the header was absent).
        return Task.FromResult(TypedResults.Ok(parameters.XCorrelationId ?? string.Empty));
    }
}