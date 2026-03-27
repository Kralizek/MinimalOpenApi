using Microsoft.AspNetCore.Http.HttpResults;
using MinimalOpenAPI.Sample.Api.Generated;

namespace MinimalOpenAPI.Sample.Api;

/// <summary>Simple in-memory store for the sample.</summary>
public sealed class InMemoryClientStore
{
    private readonly Dictionary<Guid, Client> _clients = new();

    public Client? Get(Guid tenantId, Guid clientId)
    {
        _clients.TryGetValue(clientId, out var client);
        return client?.TenantId == tenantId ? client : null;
    }

    public void Save(Client client) => _clients[client.Id] = client;
}
