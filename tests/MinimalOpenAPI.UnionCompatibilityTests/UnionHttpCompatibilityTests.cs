using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

using MinimalOpenAPI.UnionCompatibilityTests.Generated.Contracts;

namespace MinimalOpenAPI.UnionCompatibilityTests;

[TestFixture]
public sealed class UnionHttpCompatibilityTests
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolver = JsonTypeInfoResolver.Combine(
                UnionJsonContext.Default,
                new DefaultJsonTypeInfoResolver()));

        _app = builder.Build();
        _app.MapPost("/primitive", static (Primitive payload, HttpContext context) => Echo(context, payload));
        _app.MapPost("/animal", static (Animal payload, HttpContext context) => Echo(context, payload));
        _app.MapPost("/event", static (Event payload, HttpContext context) => Echo(context, payload));
        _app.MapPost("/generated-pet", static (GeneratedPet payload, HttpContext context) =>
            EchoGenerated(context, payload, payload.Value?.GetType().Name));
        _app.MapPost("/generated-event", static (GeneratedEvent payload, HttpContext context) =>
            EchoGenerated(context, payload, payload.Value?.GetType().Name));
        _app.MapPost("/generated-value", static (GeneratedValue payload, HttpContext context) =>
            EchoGenerated(context, payload, payload.Value?.GetType().Name));

        await _app.StartAsync();
        var address = _app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.Single();
        _client = new HttpClient { BaseAddress = new Uri(address) };
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Test]
    public void UsesSystemTextJson11AndTheExpectedUnionMarker()
    {
        var assembly = typeof(JsonSerializer).Assembly;
        Assert.That(assembly.GetName().Version?.Major, Is.EqualTo(11));
#if NET10_0
        Assert.That(Path.GetFullPath(assembly.Location), Does.StartWith(Path.GetFullPath(AppContext.BaseDirectory)));
#else
        Assert.That(Path.GetFullPath(assembly.Location), Does.Not.StartWith(Path.GetFullPath(AppContext.BaseDirectory)));
#endif
        Assert.That(
            typeof(Primitive).IsDefined(typeof(System.Runtime.CompilerServices.UnionAttribute), inherit: false),
            Is.True);
    }

    [TestCase("42", "Int32")]
    [TestCase("true", "Boolean")]
    public Task PrimitiveCases_RoundTripThroughMinimalApi(string json, string expectedCase)
        => AssertRoundTripAsync("/primitive", json, expectedCase);

    [TestCase("""{"name":"Rex","breed":"Husky"}""", "Dog")]
    [TestCase("""{"name":"Mog","lives":9}""", "Cat")]
    public Task StructuralCases_RoundTripThroughMinimalApi(string json, string expectedCase)
        => AssertRoundTripAsync("/animal", json, expectedCase);

    [TestCase("""{"name":"Unknown"}""")]
    [TestCase("""{"name":"Unknown","breed":"Husky","lives":9}""")]
    public Task StructuralCasesWithoutOneDistinctMatch_ReturnBadRequest(string json)
        => AssertRejectedAsync("/animal", json);

    [TestCase("""{"id":42,"kind":"created"}""", "Created")]
    [TestCase("""{"kind":"deleted","id":42}""", "Deleted")]
    public Task DiscriminatorCases_RoundTripWithoutChangingTheWireShape(string json, string expectedCase)
        => AssertRoundTripAsync("/event", json, expectedCase);

    [TestCase("""{"id":42}""")]
    [TestCase("""{"id":42,"kind":"unknown"}""")]
    [TestCase("""{"kind":"created","kind":"deleted","id":42}""")]
    public Task MissingUnknownOrDuplicateDiscriminators_ReturnBadRequest(string json)
        => AssertRejectedAsync("/event", json);

    [TestCase("""{"name":"Rex","breed":"Husky"}""", "GeneratedDog")]
    [TestCase("""{"name":"Mog","lives":9}""", "GeneratedCat")]
    public Task GeneratedStructuralUnion_RoundTripsThroughMinimalApi(string json, string expectedCase)
        => AssertRoundTripAsync("/generated-pet", json, expectedCase);

    [TestCase("""{"id":42,"kind":"created"}""", "GeneratedCreated")]
    [TestCase("""{"kind":"deleted","id":42}""", "GeneratedDeleted")]
    public Task GeneratedDiscriminatorUnion_PreservesAuthoredWireShape(string json, string expectedCase)
        => AssertRoundTripAsync("/generated-event", json, expectedCase);

    [TestCase("42", "Int32")]
    [TestCase("true", "Boolean")]
    public Task GeneratedPrimitiveUnion_RoundTripsThroughMinimalApi(string json, string expectedCase)
        => AssertRoundTripAsync("/generated-value", json, expectedCase);

    [TestCase("""{"name":"Unknown"}""")]
    [TestCase("""{"name":"Unknown","breed":"Husky","lives":9}""")]
    public Task GeneratedStructuralUnion_RejectsAmbiguousPayloads(string json)
        => AssertRejectedAsync("/generated-pet", json);

    [TestCase("""{"id":42}""")]
    [TestCase("""{"id":42,"kind":"unknown"}""")]
    [TestCase("""{"kind":"created","kind":"deleted","id":42}""")]
    public Task GeneratedDiscriminatorUnion_RejectsInvalidDiscriminators(string json)
        => AssertRejectedAsync("/generated-event", json);

    private async Task AssertRoundTripAsync(string route, string json, string expectedCase)
    {
        using var response = await PostJsonAsync(route, json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Headers.GetValues("X-Union-Case").Single(), Is.EqualTo(expectedCase));

        var actualJson = await response.Content.ReadAsStringAsync();
        Assert.That(JsonNode.DeepEquals(JsonNode.Parse(json), JsonNode.Parse(actualJson)), Is.True);
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
    }

    private async Task AssertRejectedAsync(string route, string json)
    {
        using var response = await PostJsonAsync(route, json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    private async Task<HttpResponseMessage> PostJsonAsync(string route, string json)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _client.PostAsync(route, content);
    }

    private static IResult Echo<T>(HttpContext context, T payload)
    {
        context.Response.Headers["X-Union-Case"] = payload switch
        {
            Primitive value => value.Value?.GetType().Name ?? "none",
            Animal value => value.Value?.GetType().Name ?? "none",
            Event value => value.Value?.GetType().Name ?? "none",
            _ => throw new InvalidOperationException($"Unexpected union type '{typeof(T)}'.")
        };

        return TypedResults.Ok(payload);
    }

    private static IResult EchoGenerated<T>(HttpContext context, T payload, string? caseName)
    {
        context.Response.Headers["X-Union-Case"] = caseName ?? "none";
        return TypedResults.Ok(payload);
    }
}