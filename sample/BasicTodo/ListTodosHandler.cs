using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.Samples.BasicTodo.Openapi.Contracts;
using MinimalOpenAPI.Samples.BasicTodo.Openapi.Endpoints;

namespace MinimalOpenAPI.Samples.BasicTodo;

/// <summary>Returns all todo items from the in-memory store.</summary>
public sealed class ListTodosHandler : ListTodosEndpointBase
{
    private readonly InMemoryTodoStore _store;

    public ListTodosHandler(InMemoryTodoStore store) => _store = store;

    public override Task<Ok<Todo[]>> HandleAsync(CancellationToken cancellationToken)
        => Task.FromResult(TypedResults.Ok(_store.List().ToArray()));
}