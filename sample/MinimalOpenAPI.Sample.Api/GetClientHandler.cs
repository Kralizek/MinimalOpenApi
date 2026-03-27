using Microsoft.AspNetCore.Http.HttpResults;
using MinimalOpenAPI.Sample.Api.Generated;

namespace MinimalOpenAPI.Sample.Api;

/// <summary>Concrete implementation of the generated GetClientEndpoint handler.</summary>
public sealed class GetClientHandler : GetClientEndpoint
{
    private readonly InMemoryClientStore _store;

    public GetClientHandler(InMemoryClientStore store)
    {
        _store = store;
    }

    public override async Task<Results<Ok<Client>, NotFound>> Handle(
        global::System.Guid tenantId,
        global::System.Guid clientId,
        bool? includeDeleted,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        var client = _store.Get(tenantId, clientId);

        if (client is null)
            return TypedResults.NotFound();

        return TypedResults.Ok(client);
    }
}
