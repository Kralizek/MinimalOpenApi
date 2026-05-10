using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.Samples.Parameters.Openapi.Endpoints;

namespace MinimalOpenAPI.Samples.Parameters;

/// <summary>
/// Demonstrates reading grouped parameters (path, query, header, cookie) from the
/// generated <c>Parameters</c> record. All non-path parameters are grouped into
/// the record with <c>[AsParameters]</c>.
/// </summary>
public sealed class ListTenantItemsHandler : ListTenantItemsEndpointBase
{
    public override Task<Ok<OkResponse>> HandleAsync(
        System.Guid tenantId,
        ListTenantItemsEndpointBase.Parameters parameters,
        CancellationToken cancellationToken)
    {
        // All query, header, and cookie parameters are available via the Parameters record.
        // Note: cookie parameters (SessionId) appear in the Parameters record but are not
        // automatically bound by ASP.NET Core Minimal APIs — see README for details.
        var response = new OkResponse
        {
            TenantId = tenantId,
            Items = [$"item-page{parameters.Page ?? 1}", $"size{parameters.PageSize ?? 20}"]
        };
        return Task.FromResult(TypedResults.Ok(response));
    }
}