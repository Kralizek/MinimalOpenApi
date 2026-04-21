using Microsoft.AspNetCore.Http.HttpResults;

using WithMinimalOpenApi.Todo.Endpoints;

namespace WithMinimalOpenApi.Endpoints;

public sealed class ListTodosHandler : ListTodosEndpointBase
{
    private readonly InMemoryTodoStore _store;

    public ListTodosHandler(InMemoryTodoStore store) => _store = store;

    public override Task<Ok<OkResponse>> HandleAsync(Parameters parameters, CancellationToken cancellationToken)
    {
        var items = _store.List(parameters.Completed)
            .Select(t => t.ToContract())
            .ToArray();

        var response = new OkResponse
        {
            Items = items,
            Count = items.Length,
        };

        return Task.FromResult(TypedResults.Ok(response));
    }
}