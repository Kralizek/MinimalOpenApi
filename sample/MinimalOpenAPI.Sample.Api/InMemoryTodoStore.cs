using MinimalOpenAPI.Sample.Api.Openapi.Contracts;

namespace MinimalOpenAPI.Sample.Api;

/// <summary>Simple in-memory store for the todo sample.</summary>
public sealed class InMemoryTodoStore
{
    public sealed record TodoEntry(
        Guid Id,
        string Title,
        string? Description,
        bool IsComplete,
        TodoPriority? Priority,
        DateOnly? DueDate,
        Dictionary<string, (string? Value, string? Color)>? Metadata);

    private readonly Dictionary<Guid, TodoEntry> _items = new();

    public IEnumerable<TodoEntry> List(bool? isComplete, TodoPriority? priority) =>
        _items.Values
            .Where(t => isComplete is null || t.IsComplete == isComplete)
            .Where(t => priority is null || t.Priority == priority);

    public TodoEntry? Get(Guid id) =>
        _items.GetValueOrDefault(id);

    public Guid Add(string title, string? description, TodoPriority? priority, DateOnly? dueDate,
        Dictionary<string, (string? Value, string? Color)>? metadata = null)
    {
        var id = Guid.NewGuid();
        _items[id] = new TodoEntry(id, title, description, false, priority, dueDate, metadata);
        return id;
    }

    public bool Update(Guid id, string title, string? description, bool isComplete,
        TodoPriority? priority, DateOnly? dueDate)
    {
        if (!_items.TryGetValue(id, out var existing)) return false;
        _items[id] = existing with { Title = title, Description = description, IsComplete = isComplete, Priority = priority, DueDate = dueDate };
        return true;
    }

    public bool Delete(Guid id) => _items.Remove(id);
}