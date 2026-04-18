namespace WithoutMinimalOpenApi.Endpoints;

public sealed class InMemoryTodoStore
{
    public sealed record TodoEntry(
        Guid Id,
        string Title,
        bool Completed,
        DateTimeOffset CreatedAt,
        DateOnly? DueDate,
        string? Notes);

    private readonly Dictionary<Guid, TodoEntry> _items = new();

    public IEnumerable<TodoEntry> List(bool? completed) =>
        _items.Values
            .Where(t => completed is null || t.Completed == completed);

    public TodoEntry? Get(Guid id) =>
        _items.GetValueOrDefault(id);

    public TodoEntry Add(string title, DateOnly dueDate, string? notes)
    {
        var todo = new TodoEntry(
            Guid.NewGuid(),
            title,
            Completed: false,
            CreatedAt: DateTimeOffset.UtcNow,
            DueDate: dueDate,
            Notes: notes);

        _items[todo.Id] = todo;
        return todo;
    }

    public TodoEntry? Update(Guid id, string? title, bool? completed, DateOnly? dueDate, string? notes)
    {
        if (!_items.TryGetValue(id, out var existing))
        {
            return null;
        }

        var updated = existing with
        {
            Title = title ?? existing.Title,
            Completed = completed ?? existing.Completed,
            DueDate = dueDate ?? existing.DueDate,
            Notes = notes ?? existing.Notes,
        };

        _items[id] = updated;
        return updated;
    }

    public bool Delete(Guid id) => _items.Remove(id);
}