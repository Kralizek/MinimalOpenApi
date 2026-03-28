using Microsoft.AspNetCore.Http.HttpResults;
using MinimalOpenAPI.Sample.Api.Generated;

namespace MinimalOpenAPI.Sample.Api;

/// <summary>Creates a new todo item.</summary>
public sealed class CreateTodoHandler : CreateTodoEndpoint
{
    private readonly InMemoryTodoStore _store;

    public CreateTodoHandler(InMemoryTodoStore store) => _store = store;

    public override Task<Results<Created<Todo>, BadRequest>> HandleAsync(
        CreateTodoRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Task.FromResult<Results<Created<Todo>, BadRequest>>(TypedResults.BadRequest());

        var id = _store.Add(request.Title, request.Description);
        var todo = new Todo { Id = id, Title = request.Title, Description = request.Description, IsComplete = false };

        return Task.FromResult<Results<Created<Todo>, BadRequest>>(
            TypedResults.Created($"/todos/{id}", todo));
    }
}
