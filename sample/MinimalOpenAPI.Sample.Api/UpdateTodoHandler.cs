using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.Sample.Api.Openapi.Contracts;
using MinimalOpenAPI.Sample.Api.Openapi.Endpoints;

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

        if (!_store.Update(id, request.Title, request.Description, request.IsComplete, request.Priority, request.DueDate))
            return Task.FromResult<Results<Ok<Todo>, BadRequest, NotFound>>(TypedResults.NotFound());

        var item = _store.Get(id)!.Value;
        var todo = new Todo
        {
            Id = id,
            Title = item.Title,
            Description = item.Description,
            IsComplete = item.IsComplete,
            Priority = item.Priority,
            DueDate = item.DueDate,
            Metadata = item.Metadata?.ToDictionary(
                kvp => kvp.Key,
                kvp => new TodoMetadataValue { Value = kvp.Value.Value, Color = kvp.Value.Color }),
        };
        return Task.FromResult<Results<Ok<Todo>, BadRequest, NotFound>>(TypedResults.Ok(todo));
    }
}