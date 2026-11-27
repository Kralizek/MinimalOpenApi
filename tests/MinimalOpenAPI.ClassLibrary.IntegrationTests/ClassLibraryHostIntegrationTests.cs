using System.Net;
using System.Text.Json;

namespace MinimalOpenAPI.ClassLibrary.IntegrationTests;

/// <summary>
/// Contracts and handlers declared in referenced class libraries are mapped by a host that only
/// calls <c>AddMinimalOpenApi()</c> and <c>MapMinimalOpenApiEndpoints()</c> (#127, #129).
/// </summary>
[TestFixture]
public class ClassLibraryHostIntegrationTests
{
    private WebApplicationFactory<MinimalOpenAPI.Samples.ClassLibrary.Host.Program> _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WebApplicationFactory<MinimalOpenAPI.Samples.ClassLibrary.Host.Program>();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task CatalogLibraryEndpoint_IsMapped()
    {
        var response = await _client.GetAsync("/catalog/items/42");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(json.RootElement.GetProperty("id").GetInt32(), Is.EqualTo(42));
        Assert.That(json.RootElement.GetProperty("name").GetString(), Is.EqualTo("Item 42"));
    }

    [Test]
    public async Task ExplicitEntryPointCalls_AreIdempotent()
    {
        // The host's generated module initializer already registered the library; calling the generated
        // entry point again (as a library referencing another library would) must not map its routes twice.
        Contracts.Catalog.CatalogMinimalOpenApi.Register();
        Billing.Billing.BillingMinimalOpenApi.Register();

        using var factory = new WebApplicationFactory<MinimalOpenAPI.Samples.ClassLibrary.Host.Program>();
        using var client = factory.CreateClient();

        Assert.That((await client.GetAsync("/catalog/items/1")).StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await client.GetAsync("/billing/invoices/1")).StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task BillingLibraryEndpoint_IsMappedWithItsEndpointConfiguration()
    {
        var response = await _client.GetAsync("/billing/invoices/2");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Headers.TryGetValues("X-Billing-Configured", out var values), Is.True);
        Assert.That(values, Is.EqualTo(new[] { "true" }));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(json.RootElement.GetProperty("total").GetDouble(), Is.EqualTo(21.0));
    }

    [Test]
    public async Task LayeredDocument_IsServedByHandlerAndConfigurationImplementedInTheHost()
    {
        // inventory.yaml: DTOs generated in Inventory.Contracts, endpoints in Inventory.Api,
        // handler and endpoint configuration implemented in the host.
        var response = await _client.GetAsync("/inventory/stock/abc");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Headers.TryGetValues("X-Inventory-Configured", out var values), Is.True);
        Assert.That(values, Is.EqualTo(new[] { "true" }));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(json.RootElement.GetProperty("sku").GetString(), Is.EqualTo("abc"));
        Assert.That(json.RootElement.GetProperty("quantity").GetInt32(), Is.EqualTo(3));
    }

    [Test]
    public async Task LayeredDocument_HostHandlerResultsAreReturned()
    {
        var response = await _client.GetAsync("/inventory/stock/unknown");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task HostOwnDocumentEndpoint_IsMapped()
    {
        var response = await _client.GetAsync("/health");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(json.RootElement.GetProperty("status").GetString(), Is.EqualTo("ok"));
    }
}