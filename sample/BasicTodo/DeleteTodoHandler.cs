using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.Samples.BasicTodo.Openapi.Endpoints;

namespace MinimalOpenAPI.Samples.BasicTodo;

/// <summary>Permanently removes the todo item with the specified identifier.</summary>
public sealed class DeleteTodoHandler : DeleteTodoEndpointBase
{
    private readonly InMemoryTodoStore _store;

    public DeleteTodoHandler(InMemoryTodoStore store) => _store = store;

    public override Task<Results<NoContent, NotFound>> HandleAsync(
        global::System.Guid id,
        CancellationToken cancellationToken)
    {
        return _store.Delete(id)
            ? Task.FromResult<Results<NoContent, NotFound>>(TypedResults.NoContent())
            : Task.FromResult<Results<NoContent, NotFound>>(TypedResults.NotFound());
    }
}