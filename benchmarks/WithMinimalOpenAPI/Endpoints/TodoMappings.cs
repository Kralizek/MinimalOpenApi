using WithMinimalOpenApi.Todo.Contracts;

namespace WithMinimalOpenApi.Endpoints;

internal static class TodoMappings
{
    public static TodoItem ToContract(this InMemoryTodoStore.TodoEntry entry) => new()
    {
        Id = entry.Id,
        Title = entry.Title,
        Completed = entry.Completed,
        CreatedAt = entry.CreatedAt,
        DueDate = entry.DueDate,
        Notes = entry.Notes,
    };
}