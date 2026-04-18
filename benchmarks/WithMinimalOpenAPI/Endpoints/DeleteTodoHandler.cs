using Microsoft.AspNetCore.Http.HttpResults;

using WithMinimalOpenApi.Todo.Endpoints;

namespace WithMinimalOpenApi.Endpoints;

public sealed class DeleteTodoHandler : DeleteTodoEndpointBase
{
    private readonly InMemoryTodoStore _store;

    public DeleteTodoHandler(InMemoryTodoStore store) => _store = store;

    public override Task<Results<NoContent, NotFound>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!_store.Delete(id))
        {
            return Task.FromResult<Results<NoContent, NotFound>>(TypedResults.NotFound());
        }

        return Task.FromResult<Results<NoContent, NotFound>>(TypedResults.NoContent());
    }
}