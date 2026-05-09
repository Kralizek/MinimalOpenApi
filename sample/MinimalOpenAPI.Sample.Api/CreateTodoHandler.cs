using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.Sample.Api.Openapi.Contracts;
using MinimalOpenAPI.Sample.Api.Openapi.Endpoints;

namespace MinimalOpenAPI.Sample.Api;

/// <summary>Creates a new todo item.</summary>
public sealed class CreateTodoHandler : CreateTodoEndpointBase
{
    private readonly InMemoryTodoStore _store;

    public CreateTodoHandler(InMemoryTodoStore store) => _store = store;

    public override Task<Results<Created<Todo>, BadRequestProblem>> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Task.FromResult<Results<Created<Todo>, BadRequestProblem>>(
                new BadRequestProblem(new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Title = "Invalid request",
                    Detail = "The request is invalid."
                }));
        }

        // The inline request Metadata uses RequestMetadataValue; the store uses a raw (Value, Color) tuple.
        var storeMetadata = request.Metadata?.ToDictionary(
            kvp => kvp.Key,
            kvp => (kvp.Value?.Value, kvp.Value?.Color));

        var id = _store.Add(request.Title, request.Description, request.Priority, request.DueDate, storeMetadata);

        var todo = GetTodoHandler.ToTodo(_store.Get(id)!);
        return Task.FromResult<Results<Created<Todo>, BadRequestProblem>>(TypedResults.Created($"/todos/{id}", todo));
    }
}