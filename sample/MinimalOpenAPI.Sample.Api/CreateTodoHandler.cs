using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.Sample.Api.Contracts;
using MinimalOpenAPI.Sample.Api.Endpoints;

namespace MinimalOpenAPI.Sample.Api;

/// <summary>Creates a new todo item.</summary>
public sealed class CreateTodoHandler : CreateTodoEndpointBase
{
    private readonly InMemoryTodoStore _store;

    public CreateTodoHandler(InMemoryTodoStore store) => _store = store;

    public override Task<Results<Created<Todo>, BadRequest>> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Task.FromResult<Results<Created<Todo>, BadRequest>>(TypedResults.BadRequest());

        var id = _store.Add(request.Title, request.Description, request.DueDate);
        var todo = new Todo { Id = id, Title = request.Title, Description = request.Description, IsComplete = false, DueDate = request.DueDate };

        return Task.FromResult<Results<Created<Todo>, BadRequest>>(
            TypedResults.Created($"/todos/{id}", todo));
    }
}