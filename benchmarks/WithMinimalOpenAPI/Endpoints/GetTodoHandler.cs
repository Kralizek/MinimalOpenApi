using Microsoft.AspNetCore.Http.HttpResults;

using WithMinimalOpenApi.Todo.Contracts;
using WithMinimalOpenApi.Todo.Endpoints;

namespace WithMinimalOpenApi.Endpoints;

public sealed class GetTodoHandler : GetTodoEndpointBase
{
    private readonly InMemoryTodoStore _store;

    public GetTodoHandler(InMemoryTodoStore store) => _store = store;

    public override Task<Results<Ok<TodoItem>, NotFound>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var todo = _store.Get(id);

        if (todo is null)
        {
            return Task.FromResult<Results<Ok<TodoItem>, NotFound>>(TypedResults.NotFound());
        }

        return Task.FromResult<Results<Ok<TodoItem>, NotFound>>(TypedResults.Ok(todo.ToContract()));
    }
}