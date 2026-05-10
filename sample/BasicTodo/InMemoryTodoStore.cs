using MinimalOpenAPI.Samples.BasicTodo.Openapi.Contracts;

namespace MinimalOpenAPI.Samples.BasicTodo;

/// <summary>Simple in-memory store for the Basic Todo sample.</summary>
public sealed class InMemoryTodoStore
{
    private readonly Dictionary<Guid, Todo> _items = new();

    public IReadOnlyCollection<Todo> List() => _items.Values.ToList().AsReadOnly();

    public Todo? Get(Guid id) => _items.GetValueOrDefault(id);

    public Todo Add(string title, string? description)
    {
        var id = Guid.NewGuid();
        var todo = new Todo { Id = id, Title = title, Description = description, IsComplete = false };
        _items[id] = todo;
        return todo;
    }

    public bool Delete(Guid id) => _items.Remove(id);
}