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
            .Select(t => new Todo
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                IsComplete = t.IsComplete,
                Priority = t.Priority,
                DueDate = t.DueDate,
                Metadata = t.Metadata?.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new TodoMetadataValue { Value = kvp.Value.Value, Color = kvp.Value.Color }),
            })
            .ToArray();

        return Task.FromResult(TypedResults.Ok(items));
    }
}