using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.Sample.Api.Openapi.Contracts;
using MinimalOpenAPI.Sample.Api.Openapi.Endpoints;

namespace MinimalOpenAPI.Sample.Api;

/// <summary>Returns the todo item with the specified identifier.</summary>
public sealed class GetTodoHandler : GetTodoEndpointBase
{
    private readonly InMemoryTodoStore _store;

    public GetTodoHandler(InMemoryTodoStore store) => _store = store;

    public override Task<Results<Ok<Todo>, NotFound>> HandleAsync(
        global::System.Guid id,
        CancellationToken cancellationToken)
    {
        var item = _store.Get(id);

        if (item is null)
            return Task.FromResult<Results<Ok<Todo>, NotFound>>(TypedResults.NotFound());

        var todo = ToTodo(item);
        return Task.FromResult<Results<Ok<Todo>, NotFound>>(TypedResults.Ok(todo));
    }

    internal static Todo ToTodo(InMemoryTodoStore.TodoEntry t) => new()
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
    };
}
