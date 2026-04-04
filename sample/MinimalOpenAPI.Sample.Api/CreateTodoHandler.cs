using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.Sample.Api.Openapi.Contracts;
using MinimalOpenAPI.Sample.Api.Openapi.Endpoints;

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

        // Convert inline request metadata value type to the component schema value type.
        var metadata = request.Metadata?.ToDictionary(
            kvp => kvp.Key,
            kvp => new TodoMetadataValue { Value = kvp.Value?.Value, Color = kvp.Value?.Color });

        var id = _store.Add(request.Title, request.Description, request.Priority, request.DueDate,
            metadata?.ToDictionary(kvp => kvp.Key, kvp => (kvp.Value.Value, kvp.Value.Color)));

        var todo = new Todo
        {
            Id = id,
            Title = request.Title,
            Description = request.Description,
            IsComplete = false,
            Priority = request.Priority,
            DueDate = request.DueDate,
            Metadata = metadata,
        };

        return Task.FromResult<Results<Created<Todo>, BadRequest>>(
            TypedResults.Created($"/todos/{id}", todo));
    }
}