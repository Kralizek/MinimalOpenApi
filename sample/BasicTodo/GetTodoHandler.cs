using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.Samples.BasicTodo.Openapi.Contracts;
using MinimalOpenAPI.Samples.BasicTodo.Openapi.Endpoints;

namespace MinimalOpenAPI.Samples.BasicTodo;

/// <summary>Returns the todo item with the specified identifier.</summary>
public sealed class GetTodoHandler : GetTodoEndpointBase
{
    private readonly InMemoryTodoStore _store;

    public GetTodoHandler(InMemoryTodoStore store) => _store = store;

    public override Task<Results<Ok<Todo>, NotFound>> HandleAsync(
        global::System.Guid id,
        CancellationToken cancellationToken)
    {
        var todo = _store.Get(id);
        return todo is null
            ? Task.FromResult<Results<Ok<Todo>, NotFound>>(TypedResults.NotFound())
            : Task.FromResult<Results<Ok<Todo>, NotFound>>(TypedResults.Ok(todo));
    }
}