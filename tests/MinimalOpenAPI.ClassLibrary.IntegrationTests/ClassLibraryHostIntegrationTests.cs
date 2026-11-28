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

    // Documents declared in referenced libraries are copied to the host's (and this test project's)
    // output by the SDK's standard copy-to-output mechanism and served at their PublishAs paths (#106).
    // billing.yaml and the host's document are both named openapi.yaml and must stay distinct.
    [TestCase("/openapi/catalog.yaml", "Catalog/catalog.yaml")]
    [TestCase("/openapi/billing.yaml", "Billing/openapi.yaml")]
    [TestCase("/openapi/inventory.yaml", "Inventory.Contracts/inventory.yaml")]
    [TestCase("/openapi/health.yaml", "Host/openapi.yaml")]
    public async Task AuthoredDocuments_AreServedAtTheirPublishAsPaths(string route, string sampleFile)
    {
        var response = await _client.GetAsync(route);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var authored = await File.ReadAllTextAsync(Path.Combine(SampleDirectory, sampleFile));
        Assert.That(Normalize(await response.Content.ReadAsStringAsync()), Is.EqualTo(Normalize(authored)),
            "the authored document is served, not a regenerated one");
    }

    private static string SampleDirectory
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MinimalOpenApi.slnx")))
                directory = directory.Parent;
            return Path.Combine(directory!.FullName, "sample", "ClassLibrary");
        }
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd();

    [Test]
    public async Task HostOwnDocumentEndpoint_IsMapped()
    {
        var response = await _client.GetAsync("/health");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(json.RootElement.GetProperty("status").GetString(), Is.EqualTo("ok"));
    }
}