using Microsoft.AspNetCore.Http.HttpResults;

using WithMinimalOpenApi.Endpoints;
using WithMinimalOpenApi.Todo.Contracts;
using WithMinimalOpenApi.Todo.Endpoints;

namespace WithMinimalOpenApi.Todo.Endpoints;

public sealed class CreateTodoHandler : CreateTodoEndpointBase
{
    private readonly InMemoryTodoStore _store;

    public CreateTodoHandler(InMemoryTodoStore store) => _store = store;

    public override Task<Created<TodoItem>> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var id = _store.Add(request.Title, request.DueDate, request.Notes);
        var todo = _store.Get(id)!.ToContract();

        return Task.FromResult(TypedResults.Created($"/todos/{id}", todo));
    }
}