using Microsoft.AspNetCore.Http.HttpResults;
using MinimalOpenAPI.Sample.Api.Generated;

namespace MinimalOpenAPI.Sample.Api;

/// <summary>Updates an existing todo item.</summary>
public sealed class UpdateTodoHandler : UpdateTodoEndpoint
{
    private readonly InMemoryTodoStore _store;

    public UpdateTodoHandler(InMemoryTodoStore store) => _store = store;

    public override Task<Results<Ok<Todo>, BadRequest, NotFound>> HandleAsync(
        global::System.Guid id,
        Todo request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Task.FromResult<Results<Ok<Todo>, BadRequest, NotFound>>(TypedResults.BadRequest());

        if (!_store.Update(id, request.Title, request.Description, request.IsComplete))
            return Task.FromResult<Results<Ok<Todo>, BadRequest, NotFound>>(TypedResults.NotFound());

        var todo = new Todo { Id = id, Title = request.Title, Description = request.Description, IsComplete = request.IsComplete };
        return Task.FromResult<Results<Ok<Todo>, BadRequest, NotFound>>(TypedResults.Ok(todo));
    }
}
