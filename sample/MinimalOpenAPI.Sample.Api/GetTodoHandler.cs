using Microsoft.AspNetCore.Http.HttpResults;
using MinimalOpenAPI.Sample.Api.Generated;

namespace MinimalOpenAPI.Sample.Api;

/// <summary>Returns the todo item with the specified identifier.</summary>
public sealed class GetTodoHandler : GetTodoEndpoint
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

        var todo = new Todo { Id = item.Value.Id, Title = item.Value.Title, Description = item.Value.Description, IsComplete = item.Value.IsComplete };
        return Task.FromResult<Results<Ok<Todo>, NotFound>>(TypedResults.Ok(todo));
    }
}
