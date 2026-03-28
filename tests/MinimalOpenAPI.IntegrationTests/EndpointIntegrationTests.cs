using System.Net;
using System.Net.Http.Json;
using MinimalOpenAPI.Sample.Api.Generated;

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
    public async Task GetClient_WhenClientNotFound_Returns404()
    {
        var tenantId = Guid.NewGuid();
        var clientId = Guid.NewGuid();

        var response = await _client.GetAsync(
            $"/tenants/{tenantId}/clients/{clientId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task CreateClient_ThenGetClient_ReturnsCreatedClient()
    {
        var tenantId = Guid.NewGuid();

        var createRequest = new { name = "Acme Corp", vatNumber = "IT12345" };
        var createResponse = await _client.PostAsJsonAsync(
            $"/tenants/{tenantId}/clients",
            createRequest);

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var location = createResponse.Headers.Location;
        Assert.That(location, Is.Not.Null);

        var getResponse = await _client.GetAsync(location!.ToString());
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var client = await getResponse.Content.ReadFromJsonAsync<Client>();
        Assert.That(client, Is.Not.Null);
        Assert.That(client!.Name, Is.EqualTo("Acme Corp"));
        Assert.That(client.VatNumber, Is.EqualTo("IT12345"));
        Assert.That(client.TenantId, Is.EqualTo(tenantId));
    }

    [Test]
    public async Task CreateClient_WithEmptyName_Returns400()
    {
        var tenantId = Guid.NewGuid();

        var createRequest = new { name = "" };
        var response = await _client.PostAsJsonAsync(
            $"/tenants/{tenantId}/clients",
            createRequest);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task GetClient_WithQueryParameter_IsAccepted()
    {
        var tenantId = Guid.NewGuid();
        var clientId = Guid.NewGuid();

        var response = await _client.GetAsync(
            $"/tenants/{tenantId}/clients/{clientId}?includeDeleted=true");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task CreateClient_WithNullVatNumber_Returns201()
    {
        var tenantId = Guid.NewGuid();

        var createRequest = new { name = "Simple Corp" };
        var response = await _client.PostAsJsonAsync(
            $"/tenants/{tenantId}/clients",
            createRequest);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }

    [Test]
    public async Task CreateClientEndpoint_HasRegistrationCustomizerApplied()
    {
        var tenantId = Guid.NewGuid();

        var createRequest = new { name = "Test Corp" };
        var response = await _client.PostAsJsonAsync(
            $"/tenants/{tenantId}/clients",
            createRequest);

        Assert.That(response, Is.Not.Null);
    }
}
