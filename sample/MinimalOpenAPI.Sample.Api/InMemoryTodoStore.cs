namespace MinimalOpenAPI.Sample.Api;

/// <summary>Simple in-memory store for the todo sample.</summary>
public sealed class InMemoryTodoStore
{
    private readonly Dictionary<Guid, (string Title, string? Description, bool IsComplete)> _items = new();

    public IEnumerable<(Guid Id, string Title, string? Description, bool IsComplete)> List(bool? isComplete)
    {
        foreach (var kvp in _items)
        {
            if (isComplete is null || kvp.Value.IsComplete == isComplete)
                yield return (kvp.Key, kvp.Value.Title, kvp.Value.Description, kvp.Value.IsComplete);
        }
    }

    public (Guid Id, string Title, string? Description, bool IsComplete)? Get(Guid id)
    {
        if (_items.TryGetValue(id, out var item))
            return (id, item.Title, item.Description, item.IsComplete);
        return null;
    }

    public Guid Add(string title, string? description)
    {
        var id = Guid.NewGuid();
        _items[id] = (title, description, false);
        return id;
    }

    public bool Update(Guid id, string title, string? description, bool isComplete)
    {
        if (!_items.ContainsKey(id)) return false;
        _items[id] = (title, description, isComplete);
        return true;
    }

    public bool Delete(Guid id) => _items.Remove(id);
}
