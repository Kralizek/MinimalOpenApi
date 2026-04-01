using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.Sample.Api.Contracts;
using MinimalOpenAPI.Sample.Api.Endpoints;

namespace MinimalOpenAPI.Sample.Api;

/// <summary>Updates an existing todo item.</summary>
public sealed class UpdateTodoHandler : UpdateTodoEndpointBase
{
    private readonly InMemoryTodoStore _store;

    public UpdateTodoHandler(InMemoryTodoStore store) => _store = store;

    public override Task<Results<Ok<Todo>, BadRequest, NotFound>> HandleAsync(Guid id, Request request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Task.FromResult<Results<Ok<Todo>, BadRequest, NotFound>>(TypedResults.BadRequest());

        if (!_store.Update(id, request.Title, request.Description, request.IsComplete, request.DueDate))
            return Task.FromResult<Results<Ok<Todo>, BadRequest, NotFound>>(TypedResults.NotFound());

        var todo = new Todo { Id = id, Title = request.Title, Description = request.Description, IsComplete = request.IsComplete, DueDate = request.DueDate };
        return Task.FromResult<Results<Ok<Todo>, BadRequest, NotFound>>(TypedResults.Ok(todo));
    }
}