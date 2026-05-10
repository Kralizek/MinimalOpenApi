using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.Samples.Parameters.Openapi.Endpoints;

namespace MinimalOpenAPI.Samples.Parameters;

/// <summary>
/// Demonstrates operation-level override of a path-level parameter.
/// The tenantId path parameter is overridden at the operation level with the same (name, in) pair.
/// </summary>
public sealed class CreateTenantItemHandler : CreateTenantItemEndpointBase
{
    public override Task<Created<string>> HandleAsync(
        System.Guid tenantId,
        CreateTenantItemEndpointBase.Parameters parameters,
        Request request,
        CancellationToken cancellationToken)
    {
        var itemId = Guid.NewGuid();
        return Task.FromResult(TypedResults.Created($"/tenants/{tenantId}/items/{itemId}", itemId.ToString()));
    }
}