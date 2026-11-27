using Inventory.Api.Stock.Endpoints;
using Inventory.Stock.Contracts;

using Microsoft.AspNetCore.Http.HttpResults;

namespace MinimalOpenAPI.Samples.ClassLibrary.Host.Inventory;

/// <summary>
/// Handles <c>GET /inventory/stock/{sku}</c>. The handler base is generated in <c>Inventory.Api</c> and
/// the DTO in <c>Inventory.Contracts</c>; MinimalOpenAPI registers this host class for it.
/// </summary>
public sealed class GetStockHandler : GetStockEndpointBase
{
    public override Task<Results<Ok<StockLevel>, NotFound>> HandleAsync(string sku, CancellationToken cancellationToken)
    {
        if (sku == "unknown")
            return Task.FromResult<Results<Ok<StockLevel>, NotFound>>(TypedResults.NotFound());

        return Task.FromResult<Results<Ok<StockLevel>, NotFound>>(
            TypedResults.Ok(new StockLevel { Sku = sku, Quantity = sku.Length }));
    }
}

/// <summary>Adds a response header so tests can verify that host endpoint configurations are applied.</summary>
public sealed class GetStockEndpointConfiguration : GetStockEndpointConfigurationBase
{
    public override void Configure(Microsoft.AspNetCore.Builder.RouteHandlerBuilder endpoint)
        => endpoint.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers["X-Inventory-Configured"] = "true";
            return await next(context);
        });
}