using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.Samples.BasicTodo.Openapi.Contracts;
using MinimalOpenAPI.Samples.BasicTodo.Openapi.Endpoints;

namespace MinimalOpenAPI.Samples.BasicTodo;

/// <summary>Creates a new todo item and returns the created resource.</summary>
public sealed class CreateTodoHandler : CreateTodoEndpointBase
{
    private readonly InMemoryTodoStore _store;

    public CreateTodoHandler(InMemoryTodoStore store) => _store = store;

    public override Task<Created<Todo>> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var todo = _store.Add(request.Title, request.Description);
        return Task.FromResult(TypedResults.Created($"/todos/{todo.Id}", todo));
    }
}