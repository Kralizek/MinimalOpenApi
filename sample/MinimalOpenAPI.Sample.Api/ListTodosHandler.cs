using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.Sample.Api.Openapi.Contracts;
using MinimalOpenAPI.Sample.Api.Openapi.Endpoints;

namespace MinimalOpenAPI.Sample.Api;

/// <summary>Lists all todo items, optionally filtered by completion status.</summary>
public sealed class ListTodosHandler : ListTodosEndpointBase
{
    private readonly InMemoryTodoStore _store;

    public ListTodosHandler(InMemoryTodoStore store) => _store = store;

    public override Task<Ok<Todo[]>> HandleAsync(
        ListTodosEndpointBase.Parameters parameters,
        CancellationToken cancellationToken)
    {
        var items = _store.List(parameters.IsComplete, parameters.Priority)
            .Select(GetTodoHandler.ToTodo)
            .ToArray();

        return Task.FromResult(TypedResults.Ok(items));
    }
}
