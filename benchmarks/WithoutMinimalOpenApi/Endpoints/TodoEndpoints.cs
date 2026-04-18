using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace WithoutMinimalOpenApi.Endpoints;

public static class TodoEndpoints
{
    public static IEndpointRouteBuilder MapTodoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/todos");

        group.MapGet("", ListTodos);
        group.MapPost("", CreateTodo);
        group.MapGet("/{id:guid}", GetTodo);
        group.MapPut("/{id:guid}", UpdateTodo);
        group.MapDelete("/{id:guid}", DeleteTodo);

        return app;
    }

    private static Ok<ListTodosResponse> ListTodos(
        [FromServices] InMemoryTodoStore store,
        [FromQuery(Name = "completed")] bool? completed)
    {
        var items = store.List(completed)
            .Select(ToResponse)
            .ToArray();

        return TypedResults.Ok(new ListTodosResponse(items, items.Length));
    }

    private static Created<TodoItemResponse> CreateTodo(
        [FromServices] InMemoryTodoStore store,
        CreateTodoRequest request)
    {
        var created = store.Add(request.Title, request.DueDate, request.Notes);
        return TypedResults.Created($"/todos/{created.Id}", ToResponse(created));
    }

    private static Results<Ok<TodoItemResponse>, NotFound> GetTodo(
        [FromServices] InMemoryTodoStore store,
        Guid id)
    {
        var todo = store.Get(id);
        return todo is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(ToResponse(todo));
    }

    private static Results<Ok<TodoItemResponse>, NotFound> UpdateTodo(
        [FromServices] InMemoryTodoStore store,
        Guid id,
        UpdateTodoRequest request)
    {
        var todo = store.Update(id, request.Title, request.Completed, request.DueDate, request.Notes);
        return todo is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(ToResponse(todo));
    }

    private static Results<NoContent, NotFound> DeleteTodo(
        [FromServices] InMemoryTodoStore store,
        Guid id)
    {
        return store.Delete(id)
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }

    private static TodoItemResponse ToResponse(InMemoryTodoStore.TodoEntry item) =>
        new(item.Id, item.Title, item.Completed, item.CreatedAt, item.DueDate, item.Notes);
}

public sealed record CreateTodoRequest(string Title, DateOnly DueDate, string? Notes);
public sealed record UpdateTodoRequest(string? Title, bool? Completed, DateOnly? DueDate, string? Notes);
public sealed record TodoItemResponse(Guid Id, string Title, bool Completed, DateTimeOffset CreatedAt, DateOnly? DueDate, string? Notes);
public sealed record ListTodosResponse(IReadOnlyList<TodoItemResponse> Items, int Count);