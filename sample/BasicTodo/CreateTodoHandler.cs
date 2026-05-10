using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

using MinimalOpenAPI.Samples.BasicTodo.Openapi.Contracts;
using MinimalOpenAPI.Samples.BasicTodo.Openapi.Endpoints;

namespace MinimalOpenAPI.Samples.BasicTodo;

/// <summary>Creates a new todo item and returns the created resource.</summary>
public sealed class CreateTodoHandler : CreateTodoEndpointBase
{
    private readonly InMemoryTodoStore _store;

    public CreateTodoHandler(InMemoryTodoStore store) => _store = store;

    public override Task<Results<Created<Todo>, BadRequestProblem>> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Task.FromResult<Results<Created<Todo>, BadRequestProblem>>(
                new BadRequestProblem(new ProblemDetails
                {
                    Title = "Invalid request",
                    Detail = "The request is invalid."
                }));
        }

        var todo = _store.Add(request.Title, request.Description);
        return Task.FromResult<Results<Created<Todo>, BadRequestProblem>>(TypedResults.Created($"/todos/{todo.Id}", todo));
    }
}