using System.Net;
using System.Net.Http.Json;

using MinimalOpenAPI.Samples.BasicTodo.Openapi.Contracts;

namespace MinimalOpenAPI.IntegrationTests;

/// <summary>
/// End-to-end integration tests that verify the full MinimalOpenAPI pipeline
/// using the BasicTodo sample as the test host.
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
        var createRequest = new { title = "Buy groceries", description = "Milk and eggs" };
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
    public async Task DeleteTodo_WhenTodoExists_Returns204()
    {
        var createRequest = new { title = "To be deleted" };
        var createResponse = await _client.PostAsJsonAsync("/todos", createRequest);
        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
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
    public async Task CreateTodo_ThenListTodos_IncludesCreatedItem()
    {
        var createRequest = new { title = "Integration test todo" };
        var createResponse = await _client.PostAsJsonAsync("/todos", createRequest);
        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var created = await createResponse.Content.ReadFromJsonAsync<Todo>();

        var listResponse = await _client.GetAsync("/todos");
        Assert.That(listResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var todos = await listResponse.Content.ReadFromJsonAsync<Todo[]>();
        Assert.That(todos, Is.Not.Null);
        Assert.That(todos!.Any(t => t.Id == created!.Id), Is.True);
    }
}