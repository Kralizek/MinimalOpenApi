namespace MinimalOpenAPI.Sample.Api;

/// <summary>Simple in-memory store for the todo sample.</summary>
public sealed class InMemoryTodoStore
{
    private readonly Dictionary<Guid, (string Title, string? Description, bool IsComplete, DateOnly? DueDate, Dictionary<string, (string? Value, string? Color)>? Metadata)> _items = new();

    public IEnumerable<(Guid Id, string Title, string? Description, bool IsComplete, DateOnly? DueDate, Dictionary<string, (string? Value, string? Color)>? Metadata)> List(bool? isComplete)
    {
        foreach (var kvp in _items)
        {
            if (isComplete is null || kvp.Value.IsComplete == isComplete)
                yield return (kvp.Key, kvp.Value.Title, kvp.Value.Description, kvp.Value.IsComplete, kvp.Value.DueDate, kvp.Value.Metadata);
        }
    }

    public (Guid Id, string Title, string? Description, bool IsComplete, DateOnly? DueDate, Dictionary<string, (string? Value, string? Color)>? Metadata)? Get(Guid id)
    {
        if (_items.TryGetValue(id, out var item))
            return (id, item.Title, item.Description, item.IsComplete, item.DueDate, item.Metadata);
        return null;
    }

    public Guid Add(string title, string? description, DateOnly? dueDate, Dictionary<string, (string? Value, string? Color)>? metadata = null)
    {
        var id = Guid.NewGuid();
        _items[id] = (title, description, false, dueDate, metadata);
        return id;
    }

    public bool Update(Guid id, string title, string? description, bool isComplete, DateOnly? dueDate)
    {
        if (!_items.TryGetValue(id, out var existing)) return false;
        _items[id] = (title, description, isComplete, dueDate, existing.Metadata);
        return true;
    }

    public bool Delete(Guid id) => _items.Remove(id);
}