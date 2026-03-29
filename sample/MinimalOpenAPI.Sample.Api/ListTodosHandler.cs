using Microsoft.AspNetCore.Http.HttpResults;
using MinimalOpenAPI.Sample.Api.Contracts;
using MinimalOpenAPI.Sample.Api.Endpoints;

namespace MinimalOpenAPI.Sample.Api;

/// <summary>Lists all todo items, optionally filtered by completion status.</summary>
public sealed class ListTodosHandler : ListTodosEndpointBase
{
    private readonly InMemoryTodoStore _store;

    public ListTodosHandler(InMemoryTodoStore store) => _store = store;

    public override Task<Ok<Todo[]>> HandleAsync(
        bool? isComplete,
        CancellationToken cancellationToken)
    {
        var items = _store.List(isComplete)
            .Select(t => new Todo { Id = t.Id, Title = t.Title, Description = t.Description, IsComplete = t.IsComplete })
            .ToArray();

        return Task.FromResult(TypedResults.Ok(items));
    }
}
