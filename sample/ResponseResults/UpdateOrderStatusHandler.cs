using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.Samples.ResponseResults.Openapi.Contracts;
using MinimalOpenAPI.Samples.ResponseResults.Openapi.Endpoints;

namespace MinimalOpenAPI.Samples.ResponseResults;

/// <summary>
/// Demonstrates three outcome types: <c>Ok&lt;Order&gt;</c>, schema-less <c>NotFoundProblem</c>,
/// and schema-backed <c>ConflictProblem</c> that carries the current and requested status values.
/// </summary>
public sealed class UpdateOrderStatusHandler : UpdateOrderStatusEndpointBase
{
    private readonly InMemoryOrderStore _store;

    public UpdateOrderStatusHandler(InMemoryOrderStore store) => _store = store;

    public override Task<Results<Ok<Order>, NotFoundProblem, ConflictProblem>> HandleAsync(
        System.Guid id,
        Request request,
        CancellationToken cancellationToken)
    {
        if (!_store.TryUpdateStatus(id, request.Status, out var order, out var currentStatus))
        {
            if (currentStatus is null)
            {
                return Task.FromResult<Results<Ok<Order>, NotFoundProblem, ConflictProblem>>(
                    new NotFoundProblem(new Microsoft.AspNetCore.Mvc.ProblemDetails
                    {
                        Title = "Order not found",
                        Detail = $"No order with id {id} was found."
                    }));
            }

            return Task.FromResult<Results<Ok<Order>, NotFoundProblem, ConflictProblem>>(
                new ConflictProblem(new UpdateOrderStatusEndpointBase.ConflictResponse
                {
                    CurrentStatus = currentStatus.Value,
                    RequestedStatus = request.Status
                }));
        }

        return Task.FromResult<Results<Ok<Order>, NotFoundProblem, ConflictProblem>>(TypedResults.Ok(order!));
    }
}