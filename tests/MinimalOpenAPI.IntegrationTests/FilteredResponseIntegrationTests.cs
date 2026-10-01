global using Microsoft.AspNetCore.Builder;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using MinimalOpenAPI.IntegrationTests.FilteredResponses.Endpoints;

namespace MinimalOpenAPI.IntegrationTests;

public sealed class GetFilteredEndpoint : GetFilteredEndpointBase
{
    public override Task<Ok> HandleAsync(CancellationToken cancellationToken)
        => Task.FromResult(TypedResults.Ok());
}

[TestFixture]
public class FilteredResponseIntegrationTests
{
    [Test]
    public async Task FilteredHandlerResponse_RemainsInEndpointMetadata()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/filtered-response-test");
        Assert.That((int)response.StatusCode, Is.EqualTo(200));

        var endpoint = factory.Services.GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(route => route.RoutePattern.RawText == "/filtered-response-test");
        var statuses = endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>()
            .Select(metadata => metadata.StatusCode).Distinct();
        Assert.That(statuses, Is.EquivalentTo(new[] { 200, 401, 403 }));
    }
}