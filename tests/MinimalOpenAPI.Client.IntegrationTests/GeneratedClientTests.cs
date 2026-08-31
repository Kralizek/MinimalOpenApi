using System.Net;
using System.Net.Http.Json;

using Microsoft.Extensions.DependencyInjection;

using MinimalOpenAPIClient.IntegrationTests.Clients.ClientTest;

using NUnit.Framework;

namespace MinimalOpenAPIClient.IntegrationTests;

[TestFixture]
public sealed class GeneratedClientTests
{
    [Test]
    public async Task Get_serializes_path_query_and_header_and_deserializes_response()
    {
        var id = Guid.NewGuid();
        var handler = new StubHandler((request, _) =>
        {
            Assert.That(request.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.RequestUri!.PathAndQuery,
                Is.EqualTo($"/todos/{id}?includeDetails=true&labels=one&labels=two"));
            Assert.That(request.Headers.GetValues("x-trace").Single(), Is.EqualTo("trace-123"));

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { id, title = "Generated", completed = true })
            });
        });

        var client = CreateClient(handler);
        var result = await client.GetTodoAsync(
            id,
            includeDetails: true,
            labels: ["one", "two"],
            xTrace: "trace-123");

        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.EqualTo(id));
            Assert.That(result.Title, Is.EqualTo("Generated"));
            Assert.That(result.Completed, Is.True);
        });
    }

    [Test]
    public async Task Post_serializes_generated_request_graph_and_deserializes_component_response()
    {
        var id = Guid.NewGuid();
        var handler = new StubHandler(async (request, cancellationToken) =>
        {
            Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request.RequestUri!.PathAndQuery, Is.EqualTo("/todos"));

            var json = await request.Content!.ReadAsStringAsync(cancellationToken);
            Assert.That(json, Does.Contain("\"title\":\"A todo\""));
            Assert.That(json, Does.Contain("\"priority\":7"));
            Assert.That(json, Does.Contain("\"note\":\"nested\""));

            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = JsonContent.Create(new { id, title = "A todo", completed = false })
            };
        });

        var client = CreateClient(handler);
        var response = await client.CreateTodoAsync(new CreateTodoRequest
        {
            Title = "A todo",
            Details = new CreateTodoRequestDetails
            {
                Priority = 7,
                Note = "nested"
            }
        });

        Assert.That(response.Id, Is.EqualTo(id));
    }

    [Test]
    public async Task Inline_request_and_response_graphs_are_flattened_without_name_collisions()
    {
        var handler = new StubHandler((request, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { details = new { score = 0.75 } })
            }));

        var client = CreateClient(handler);
        var response = await client.PreviewTodoAsync(new PreviewTodoRequest
        {
            Details = new PreviewTodoRequestDetails { Source = "draft" }
        });

        Assert.That(response.Details.Score, Is.EqualTo(0.75));
    }

    [Test]
    public void Non_success_response_throws_typed_exception_with_response_body()
    {
        var handler = new StubHandler((request, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("missing")
            }));

        var client = CreateClient(handler);

        var exception = Assert.ThrowsAsync<ClientTestClientException>(async () =>
            await client.GetTodoAsync(Guid.NewGuid(), null, null, null));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(exception.ResponseBody, Is.EqualTo("missing"));
        });
    }

    [Test]
    public async Task Empty_success_response_is_supported()
    {
        var handler = new StubHandler((request, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)));

        var client = CreateClient(handler);
        await client.DeleteTodoAsync(Guid.NewGuid());
    }

    [Test]
    public void Generated_client_can_be_registered_with_http_client_factory()
    {
        var services = new ServiceCollection();
        services.AddClientTestClient(new Uri("https://example.test"));

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<ClientTestClient>();

        Assert.That(client, Is.Not.Null);
    }

    private static ClientTestClient CreateClient(HttpMessageHandler handler)
        => new(new HttpClient(handler) { BaseAddress = new Uri("https://example.test") });

    private sealed class StubHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => handler(request, cancellationToken);
    }
}