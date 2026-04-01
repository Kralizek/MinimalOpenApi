using System.Net;
using System.Net.Http.Json;

using MinimalOpenAPI.Sample.Api.Contracts;

namespace MinimalOpenAPI.IntegrationTests;

/// <summary>
/// End-to-end integration tests that verify the full MinimalOpenAPI pipeline.
/// Uses the sample app as the test host.
/// </summary>
[TestFixture]
public class EndpointIntegrationTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task ListTodos_ReturnsEmptyArrayInitially()
    {
        var response = await _client.GetAsync("/todos");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var todos = await response.Content.ReadFromJsonAsync<Todo[]>();
        Assert.That(todos, Is.Not.Null);
    }

    [Test]
    public async Task GetTodo_WhenNotFound_Returns404()
    {
        var response = await _client.GetAsync($"/todos/{Guid.NewGuid()}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task CreateTodo_ThenGetTodo_ReturnsCreatedTodo()
    {
        var createRequest = new { title = "Buy groceries", description = "Milk and eggs", isComplete = false };
        var createResponse = await _client.PostAsJsonAsync("/todos", createRequest);

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var location = createResponse.Headers.Location;
        Assert.That(location, Is.Not.Null);

        var getResponse = await _client.GetAsync(location!.ToString());
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var todo = await getResponse.Content.ReadFromJsonAsync<Todo>();
        Assert.That(todo, Is.Not.Null);
        Assert.That(todo!.Title, Is.EqualTo("Buy groceries"));
        Assert.That(todo.Description, Is.EqualTo("Milk and eggs"));
        Assert.That(todo.IsComplete, Is.False);
    }

    [Test]
    public async Task CreateTodo_WithEmptyTitle_Returns400()
    {
        var createRequest = new { title = "" };
        var response = await _client.PostAsJsonAsync("/todos", createRequest);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task UpdateTodo_WhenTodoExists_ReturnsUpdatedTodo()
    {
        // Create a todo first
        var createRequest = new { title = "Initial title", isComplete = false };
        var createResponse = await _client.PostAsJsonAsync("/todos", createRequest);
        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var created = await createResponse.Content.ReadFromJsonAsync<Todo>();
        Assert.That(created, Is.Not.Null);

        // Update it
        var updateRequest = new { title = "Updated title", description = "Added description", isComplete = true };
        var updateResponse = await _client.PutAsJsonAsync($"/todos/{created!.Id}", updateRequest);

        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var updated = await updateResponse.Content.ReadFromJsonAsync<Todo>();
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.Title, Is.EqualTo("Updated title"));
        Assert.That(updated.IsComplete, Is.True);
    }

    [Test]
    public async Task UpdateTodo_WhenNotFound_Returns404()
    {
        var updateRequest = new { title = "Something", isComplete = false };
        var response = await _client.PutAsJsonAsync($"/todos/{Guid.NewGuid()}", updateRequest);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task DeleteTodo_WhenTodoExists_Returns204()
    {
        var createRequest = new { title = "To be deleted", isComplete = false };
        var createResponse = await _client.PostAsJsonAsync("/todos", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<Todo>();

        var deleteResponse = await _client.DeleteAsync($"/todos/{created!.Id}");

        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task DeleteTodo_WhenNotFound_Returns404()
    {
        var response = await _client.DeleteAsync($"/todos/{Guid.NewGuid()}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task ListTodos_FilteredByIsComplete_ReturnsOnlyMatchingItems()
    {
        // Create one complete and one incomplete todo
        await _client.PostAsJsonAsync("/todos", new { title = "List-filter-incomplete", isComplete = false });
        var createResponse = await _client.PostAsJsonAsync("/todos", new { title = "List-filter-complete", isComplete = false });
        var created = await createResponse.Content.ReadFromJsonAsync<Todo>();
        await _client.PutAsJsonAsync($"/todos/{created!.Id}", new { title = "List-filter-complete", isComplete = true });

        var response = await _client.GetAsync("/todos?isComplete=true");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var todos = await response.Content.ReadFromJsonAsync<Todo[]>();
        Assert.That(todos, Is.Not.Null);
        Assert.That(todos!.All(t => t.IsComplete), Is.True);
    }

    [Test]
    public async Task GetOpenApiSchema_ReturnsYamlWithCorrectContentType()
    {
        var response = await _client.GetAsync("/.openapi/schemas/1.0.0/openapi.yaml");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/yaml"));
        var body = await response.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("openapi:"));
    }
}