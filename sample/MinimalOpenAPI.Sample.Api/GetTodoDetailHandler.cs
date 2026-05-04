using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.Sample.Api.Openapi.Contracts;
using MinimalOpenAPI.Sample.Api.Openapi.Endpoints;

namespace MinimalOpenAPI.Sample.Api;

/// <summary>
/// Returns the todo item together with an inline audit snapshot.
/// The response type (<c>OkResponse</c>) is generated from an inline <c>allOf</c> that
/// merges the named <c>Todo</c> component with an inline audit object — demonstrating
/// that the generator correctly flattens <c>allOf</c> compositions inside endpoint
/// response schemas.
/// </summary>
public sealed class GetTodoDetailHandler : GetTodoDetailEndpointBase
{
    private readonly InMemoryTodoStore _store;

    public GetTodoDetailHandler(InMemoryTodoStore store) => _store = store;

    public override Task<Results<Ok<OkResponse>, NotFound>> HandleAsync(
        global::System.Guid id,
        CancellationToken cancellationToken)
    {
        var item = _store.Get(id);

        if (item is null)
            return Task.FromResult<Results<Ok<OkResponse>, NotFound>>(TypedResults.NotFound());

        var response = new OkResponse
        {
            Id = item.Id,
            Title = item.Title,
            Description = item.Description,
            IsComplete = item.IsComplete,
            Priority = item.Priority,
            DueDate = item.DueDate,
            Metadata = item.Metadata?.ToDictionary(
                kvp => kvp.Key,
                kvp => new OkResponseMetadataValue { Value = kvp.Value.Value, Color = kvp.Value.Color }),
            // Audit field contributed by the inline allOf branch
            AuditedAt = DateTimeOffset.UtcNow,
        };

        return Task.FromResult<Results<Ok<OkResponse>, NotFound>>(TypedResults.Ok(response));
    }
}