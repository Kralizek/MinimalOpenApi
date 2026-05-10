using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.Samples.SmokeTest.Openapi.Endpoints;

namespace MinimalOpenAPI.Samples.SmokeTest;

/// <summary>
/// Demonstrates a path parameter resolved from <c>components/parameters</c> via <c>$ref</c>.
/// The <c>tenantId</c> path parameter is defined as a reusable component and referenced
/// in the operation, exercising the component parameter resolution pipeline of the shipped package.
/// </summary>
public sealed class GetTenantItemsEndpoint : GetTenantItemsEndpointBase
{
    public override Task<Ok<string>> HandleAsync(System.Guid tenantId, CancellationToken cancellationToken)
        => Task.FromResult(TypedResults.Ok("items"));
}
