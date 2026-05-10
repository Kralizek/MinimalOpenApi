using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.Samples.ResponseResults.Openapi.Endpoints;

namespace MinimalOpenAPI.Samples.ResponseResults;

/// <summary>
/// Demonstrates <c>NoContent</c> plus schema-less <c>NotFoundProblem</c>.
/// </summary>
public sealed class CancelOrderHandler : CancelOrderEndpointBase
{
    private readonly InMemoryOrderStore _store;

    public CancelOrderHandler(InMemoryOrderStore store) => _store = store;

    public override Task<Results<NoContent, NotFoundProblem>> HandleAsync(
        System.Guid id,
        CancellationToken cancellationToken)
    {
        if (!_store.Cancel(id))
        {
            return Task.FromResult<Results<NoContent, NotFoundProblem>>(
                new NotFoundProblem(new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Title = "Order not found",
                    Detail = $"No order with id {id} was found."
                }));
        }

        return Task.FromResult<Results<NoContent, NotFoundProblem>>(TypedResults.NoContent());
    }
}