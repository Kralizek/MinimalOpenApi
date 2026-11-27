using Billing.Billing.Contracts;
using Billing.Billing.Endpoints;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Billing.Handlers;

/// <summary>Handles <c>GET /billing/invoices/{id}</c>.</summary>
public sealed class GetInvoiceHandler : GetInvoiceEndpointBase
{
    public override Task<Ok<Invoice>> HandleAsync(int id, CancellationToken cancellationToken)
        => Task.FromResult(TypedResults.Ok(new Invoice { Id = id, Total = id * 10.5 }));
}

/// <summary>Adds a response header so tests can verify that library endpoint configurations are applied.</summary>
public sealed class GetInvoiceEndpointConfiguration : GetInvoiceEndpointConfigurationBase
{
    public override void Configure(RouteHandlerBuilder endpoint)
        => endpoint.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers["X-Billing-Configured"] = "true";
            return await next(context);
        });
}