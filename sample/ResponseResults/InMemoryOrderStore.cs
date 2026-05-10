using MinimalOpenAPI.Samples.ResponseResults.Openapi.Contracts;

namespace MinimalOpenAPI.Samples.ResponseResults;

/// <summary>Simple in-memory store for the Response Results sample.</summary>
public sealed class InMemoryOrderStore
{
    private readonly Dictionary<Guid, Order> _orders = new();

    public Order? Get(Guid id) => _orders.GetValueOrDefault(id);

    public IReadOnlyCollection<Order> List() => _orders.Values.ToList().AsReadOnly();

    public Order Add(string externalReference, string customerName, double amount)
    {
        var id = Guid.NewGuid();
        var order = new Order
        {
            Id = id,
            ExternalReference = externalReference,
            CustomerName = customerName,
            Amount = amount,
            Status = OrderStatus.Pending
        };
        _orders[id] = order;
        return order;
    }

    public bool TryUpdateStatus(Guid id, OrderStatus status, out Order? order, out OrderStatus? currentStatus)
    {
        if (!_orders.TryGetValue(id, out var existing))
        {
            order = null;
            currentStatus = null;
            return false;
        }

        if (existing.Status == OrderStatus.Cancelled || existing.Status == status)
        {
            order = null;
            currentStatus = existing.Status;
            return false;
        }

        _orders[id] = existing with { Status = status };
        order = _orders[id];
        currentStatus = null;
        return true;
    }

    public bool Cancel(Guid id) => _orders.Remove(id);
}