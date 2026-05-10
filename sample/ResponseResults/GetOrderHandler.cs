using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.Samples.ResponseResults.Openapi.Contracts;
using MinimalOpenAPI.Samples.ResponseResults.Openapi.Endpoints;

namespace MinimalOpenAPI.Samples.ResponseResults;

/// <summary>
/// Demonstrates <c>Ok&lt;Order&gt;</c> plus a schema-backed <c>NotFoundProblem</c>.
/// The <c>NotFoundProblem</c> wrapper carries a typed payload with the missing order ID.
/// </summary>
public sealed class GetOrderHandler : GetOrderEndpointBase
{
    private readonly InMemoryOrderStore _store;

    public GetOrderHandler(InMemoryOrderStore store) => _store = store;

    public override Task<Results<Ok<Order>, NotFoundProblem>> HandleAsync(
        System.Guid id,
        CancellationToken cancellationToken)
    {
        var order = _store.Get(id);
        if (order is null)
        {
            return Task.FromResult<Results<Ok<Order>, NotFoundProblem>>(
                new NotFoundProblem(new GetOrderEndpointBase.NotFoundResponse { OrderId = id }));
        }

        return Task.FromResult<Results<Ok<Order>, NotFoundProblem>>(TypedResults.Ok(order));
    }
}