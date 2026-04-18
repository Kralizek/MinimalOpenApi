using Microsoft.AspNetCore.Http.HttpResults;

using WithMinimalOpenApi.Todo.Contracts;
using WithMinimalOpenApi.Todo.Endpoints;

namespace WithMinimalOpenApi.Endpoints;

public sealed class UpdateTodoHandler : UpdateTodoEndpointBase
{
    private readonly InMemoryTodoStore _store;

    public UpdateTodoHandler(InMemoryTodoStore store) => _store = store;

    public override Task<Results<Ok<TodoItem>, NotFound>> HandleAsync(Guid id, Request request, CancellationToken cancellationToken)
    {
        var updated = _store.Update(id, request.Title, request.Completed, request.DueDate, request.Notes);

        if (updated is null)
        {
            return Task.FromResult<Results<Ok<TodoItem>, NotFound>>(TypedResults.NotFound());
        }

        return Task.FromResult<Results<Ok<TodoItem>, NotFound>>(TypedResults.Ok(updated.ToContract()));
    }
}