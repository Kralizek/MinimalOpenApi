using MinimalOpenAPI.Samples.ResponseResults.Openapi.Contracts;

namespace MinimalOpenAPI.Samples.ResponseResults;

/// <summary>Simple in-memory store for the Response Results sample.</summary>
public sealed class InMemoryOrderStore
{
    private readonly Dictionary<Guid, Order> _orders = new();

    public Order? Get(Guid id) => _orders.GetValueOrDefault(id);

    public Order Add(string externalReference, string customerName, double amount)
    {
        var id = Guid.NewGuid();
        var order = new Order
        {
            Id = id,
            ExternalReference = externalReference,
            CustomerName = customerName,
            Amount = amount
        };
        _orders[id] = order;
        return order;
    }

    public bool Cancel(Guid id) => _orders.Remove(id);
}