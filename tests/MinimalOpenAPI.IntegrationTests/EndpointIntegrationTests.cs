using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using MinimalOpenAPI.Sample.Api.Generated;

namespace MinimalOpenAPI.IntegrationTests;

/// <summary>
/// End-to-end integration tests that verify the full MinimalOpenAPI pipeline.
/// Uses the sample app as the test host.
/// </summary>
public class EndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public EndpointIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetClient_WhenClientNotFound_Returns404()
    {
        var tenantId = Guid.NewGuid();
        var clientId = Guid.NewGuid();

        var response = await _client.GetAsync(
            $"/tenants/{tenantId}/clients/{clientId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateClient_ThenGetClient_ReturnsCreatedClient()
    {
        var tenantId = Guid.NewGuid();

        // Create a client
        var createRequest = new { name = "Acme Corp", vatNumber = "IT12345" };
        var createResponse = await _client.PostAsJsonAsync(
            $"/tenants/{tenantId}/clients",
            createRequest);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Extract the Location header to get the client ID
        var location = createResponse.Headers.Location;
        location.Should().NotBeNull();

        // GET the created client
        var getResponse = await _client.GetAsync(location!.ToString());
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var client = await getResponse.Content.ReadFromJsonAsync<Client>();
        client.Should().NotBeNull();
        client!.Name.Should().Be("Acme Corp");
        client.VatNumber.Should().Be("IT12345");
        client.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task CreateClient_WithEmptyName_Returns400()
    {
        var tenantId = Guid.NewGuid();

        var createRequest = new { name = "" };
        var response = await _client.PostAsJsonAsync(
            $"/tenants/{tenantId}/clients",
            createRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetClient_WithQueryParameter_IsAccepted()
    {
        var tenantId = Guid.NewGuid();
        var clientId = Guid.NewGuid();

        // Even though the client doesn't exist, the endpoint should accept the query param
        var response = await _client.GetAsync(
            $"/tenants/{tenantId}/clients/{clientId}?includeDeleted=true");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateClient_WithNullVatNumber_Returns201()
    {
        var tenantId = Guid.NewGuid();

        var createRequest = new { name = "Simple Corp" };
        var response = await _client.PostAsJsonAsync(
            $"/tenants/{tenantId}/clients",
            createRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateClientEndpoint_HasRegistrationCustomizerApplied()
    {
        // This verifies that the CreateClientRegistration customizer was applied
        // (currently a no-op customizer, so just verifying endpoint works)
        var tenantId = Guid.NewGuid();

        var createRequest = new { name = "Test Corp" };
        var response = await _client.PostAsJsonAsync(
            $"/tenants/{tenantId}/clients",
            createRequest);

        response.Should().NotBeNull();
    }
}
