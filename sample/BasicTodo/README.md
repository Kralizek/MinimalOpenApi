# BasicTodo

This is the **recommended starting point** for learning MinimalOpenAPI. It demonstrates the minimal contract-first workflow using a simple Todo API.

## What it demonstrates

- Authoring a minimal `openapi.yaml` contract
- Referencing an OpenAPI document in the project file with `<OpenApi Include="openapi.yaml" />`
- Calling `AddMinimalOpenApi()` and `MapMinimalOpenApiEndpoints()` in `Program.cs`
- Implementing generated endpoint base classes (`ListTodosEndpointBase`, `CreateTodoEndpointBase`, etc.)
- Using a small in-memory store for business logic
- Inline request body schema for `POST /todos` (no separate component needed for simple shapes)

## Interesting files

| File | What to look at |
|------|----------------|
| `openapi.yaml` | The API contract — source of truth |
| `BasicTodo.csproj` | How to reference the OpenAPI file and import the targets |
| `Program.cs` | Minimal `AddMinimalOpenApi()` + `MapMinimalOpenApiEndpoints()` setup |
| `ListTodosHandler.cs` | Implementing a generated base class |
| `CreateTodoHandler.cs` | Inline request body; generated `Request` record |
| `GetTodoHandler.cs` | Path parameter; typed `Results<Ok<Todo>, NotFound>` return |

## Generated types worth looking at

After building the project, the generator emits:

- `MinimalOpenAPI.Samples.BasicTodo.Openapi.Contracts.Todo` — the `Todo` DTO record
- `MinimalOpenAPI.Samples.BasicTodo.Openapi.Contracts.CreateTodoEndpointBase.Request` — the inline request body record
- `MinimalOpenAPI.Samples.BasicTodo.Openapi.Endpoints.ListTodosEndpointBase` — abstract base class for `GET /todos`
- `MinimalOpenAPI.Samples.BasicTodo.Openapi.Endpoints.CreateTodoEndpointBase` — abstract base class for `POST /todos`
- `MinimalOpenAPI.Samples.BasicTodo.Openapi.Endpoints.GetTodoEndpointBase` — abstract base class for `GET /todos/{id}`
- `MinimalOpenAPI.Samples.BasicTodo.Openapi.Endpoints.DeleteTodoEndpointBase` — abstract base class for `DELETE /todos/{id}`

To inspect generated files, see the [GeneratedFiles sample](../GeneratedFiles/README.md).

## How to run

```shell
cd sample/BasicTodo
dotnet run
```

Then try:

```shell
# Create a todo
curl -X POST http://localhost:5000/todos -H "Content-Type: application/json" -d '{"title":"Buy milk"}'

# List todos
curl http://localhost:5000/todos

# Get a specific todo (replace {id} with the id from the create response)
curl http://localhost:5000/todos/{id}

# Delete a todo
curl -X DELETE http://localhost:5000/todos/{id}
```

---

← [Back to sample catalog](../README.md)
