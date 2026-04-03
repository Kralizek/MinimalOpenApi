using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.Sample.Api.Openapi.Endpoints;

namespace MinimalOpenAPI.Sample.Api;

/// <summary>Deletes a todo item.</summary>
public sealed class DeleteTodoHandler : DeleteTodoEndpointBase
{
    private readonly InMemoryTodoStore _store;

    public DeleteTodoHandler(InMemoryTodoStore store) => _store = store;

    public override Task<Results<NoContent, NotFound>> HandleAsync(
        global::System.Guid id,
        CancellationToken cancellationToken)
    {
        if (!_store.Delete(id))
            return Task.FromResult<Results<NoContent, NotFound>>(TypedResults.NotFound());

        return Task.FromResult<Results<NoContent, NotFound>>(TypedResults.NoContent());
    }
}

/// <summary>
/// Optional registration customizer for the deleteTodo endpoint.
/// Demonstrates how to configure a single endpoint (e.g. require authorization).
/// </summary>
public sealed class DeleteTodoRegistration : DeleteTodoEndpointRegistration
{
    public override void Configure(Microsoft.AspNetCore.Builder.RouteHandlerBuilder builder)
    {
        // Example: require authorization on delete
        // builder.RequireAuthorization();
    }
}