using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.Sample.Api.Openapi.Contracts;
using MinimalOpenAPI.Sample.Api.Openapi.Endpoints;

namespace MinimalOpenAPI.Sample.Api;

/// <summary>
/// Attaches an inline audit record plus a freeform note to the specified todo.
/// The <c>Request</c> type is generated from an inline <c>allOf</c> that merges the named
/// <c>TodoAudit</c> component with an inline object carrying a <c>note</c> field —
/// demonstrating that the generator correctly flattens <c>allOf</c> compositions inside
/// endpoint request-body schemas.
/// </summary>
public sealed class AnnotateTodoHandler : AnnotateTodoEndpointBase
{
    private readonly InMemoryTodoStore _store;

    public AnnotateTodoHandler(InMemoryTodoStore store) => _store = store;

    public override Task<Results<Ok<Todo>, NotFound>> HandleAsync(
        global::System.Guid id,
        Request request,
        CancellationToken cancellationToken)
    {
        var item = _store.Get(id);

        if (item is null)
            return Task.FromResult<Results<Ok<Todo>, NotFound>>(TypedResults.NotFound());

        // The annotation payload (request.Source, request.CreatedAt, request.Note) is available
        // via the fully-typed Request record whose properties span both allOf branches.
        // In this sample the values are not persisted; the handler exists to prove the generator
        // produces a correctly typed, fully populated nested record from an inline allOf.
        var todo = GetTodoHandler.ToTodo(item);
        return Task.FromResult<Results<Ok<Todo>, NotFound>>(TypedResults.Ok(todo));
    }
}