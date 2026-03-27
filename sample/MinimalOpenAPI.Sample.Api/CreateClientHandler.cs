using Microsoft.AspNetCore.Http.HttpResults;
using MinimalOpenAPI.Sample.Api.Generated;

namespace MinimalOpenAPI.Sample.Api;

/// <summary>Concrete implementation of the generated CreateClientEndpoint handler.</summary>
public sealed class CreateClientHandler : CreateClientEndpoint
{
    private readonly InMemoryClientStore _store;

    public CreateClientHandler(InMemoryClientStore store)
    {
        _store = store;
    }

    public override async Task<Results<Created<Client>, BadRequest>> Handle(
        global::System.Guid tenantId,
        CreateClientRequest request,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(request.Name))
            return TypedResults.BadRequest();

        var client = new Client(
            Guid.NewGuid(),
            tenantId,
            request.Name,
            request.VatNumber);

        _store.Save(client);

        return TypedResults.Created(
            $"/tenants/{tenantId}/clients/{client.Id}",
            client);
    }
}
