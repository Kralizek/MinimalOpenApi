using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;

using MinimalOpenAPIClient.IntegrationTests.Clients.ClientTest;

using NUnit.Framework;

namespace MinimalOpenAPIClient.IntegrationTests;

[TestFixture]
public sealed class GeneratedClientTests
{
    [Test]
    public async Task Primitive_response_is_deserialized()
    {
        var client = CreateClient(new StubHandler((request, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(42) })));

        Assert.That(await client.GetCountAsync(), Is.EqualTo(42));
    }

    [Test]
    public async Task Same_component_used_in_request_and_response_keeps_directional_shapes_separate()
    {
        var client = CreateClient(new StubHandler(async (request, cancellationToken) =>
        {
            var json = await request.Content!.ReadAsStringAsync(cancellationToken);
            Assert.That(json, Does.Contain("\"password\":\"secret\""));
            Assert.That(json, Does.Not.Contain("\"id\""));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"account":{"id":42,"name":null,"password":"ignored","parent":{"id":7,"name":"parent"}}}""")
            };
        }));
        AccountEnvelopeResponse response = await client.CreateAccountAsync(new AccountEnvelopeRequest
        {
            Account = new AccountRequest { Password = "secret", Name = null }
        });
        Assert.That(response.Account.Id, Is.EqualTo(42));
        Assert.That(response.Account.Name, Is.Null);
        Assert.That(response.Account.Parent!.Id, Is.EqualTo(7));
        Assert.That(typeof(AccountEnvelopeRequest).GetProperty("Account")!.PropertyType, Is.EqualTo(typeof(AccountRequest)));
        Assert.That(typeof(AccountEnvelopeResponse).GetProperty("Account")!.PropertyType, Is.EqualTo(typeof(AccountResponse)));
        Assert.That(response.Account.GetType(), Is.Not.EqualTo(typeof(AccountRequest)));
        Assert.That(typeof(AccountRequest).GetProperty("Password"), Is.Not.Null);
        Assert.That(typeof(AccountResponse).GetProperty("Id"), Is.Not.Null);
        Assert.That(typeof(AccountResponse).GetProperty("Password"), Is.Null);
        Assert.That(typeof(AccountRequest).GetProperty("Id"), Is.Null);
        Assert.That(typeof(AccountRequest).GetProperty("Parent")!.PropertyType, Is.EqualTo(typeof(AccountRequest)));
        Assert.That(typeof(AccountResponse).GetProperty("Parent")!.PropertyType, Is.EqualTo(typeof(AccountResponse)));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Referenced_arrays_and_dates_use_wire_formats(bool includeTags)
    {
        var handler = new StubHandler((request, _) =>
        {
            Assert.That(request.RequestUri!.PathAndQuery, Is.EqualTo(
                "/v1/wire/2026-09-09?at=2026-09-09T12%3A30%3A00.0000000%2B02%3A00" +
                (includeTags ? "&tags=one&tags=two" : "") + "&a%26b=value"));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("null") });
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/v1?key=value") };
        var client = new ClientTestClient(httpClient);
        Assert.That(await client.GetWireAsync(new DateOnly(2026, 9, 9),
            new DateTimeOffset(2026, 9, 9, 12, 30, 0, TimeSpan.FromHours(2)),
            includeTags ? ["one", "two"] : null, "value"), Is.Null);
        _ = new ClientTestClient(httpClient);
        Assert.That(httpClient.BaseAddress.AbsoluteUri, Is.EqualTo("https://example.test/v1?key=value"));
    }

    [Test]
    public async Task Nullable_response_accepts_json_null()
    {
        var client = CreateClient(new StubHandler((request, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("null") })));

        Assert.That(await client.GetNullableCountAsync(), Is.Null);
    }

    [Test]
    public void Unset_optional_properties_are_omitted_but_required_nullable_properties_are_retained()
    {
        Assert.That(JsonSerializer.Serialize(new CreateTodoRequestDetails { Priority = 7 }),
            Is.EqualTo("{\"priority\":7}"));
        Assert.That(JsonSerializer.Serialize(new AccountRequest { Password = "secret", Name = null }),
            Does.Contain("\"name\":null"));
    }

    [Test]
    public async Task Get_serializes_path_query_header_and_enum_and_deserializes_response()
    {
        var id = Guid.NewGuid();
        var handler = new StubHandler((request, _) =>
        {
            Assert.That(request.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.RequestUri!.PathAndQuery,
                Is.EqualTo($"/todos/{id}?includeDetails=true&labels=one&labels=two&mode=fast-mode"));
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
            mode: GetTodoMode.FastMode,
            xTrace: "trace-123");

        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.EqualTo(id));
            Assert.That(result.Title, Is.EqualTo("Generated"));
            Assert.That(result.Completed, Is.True);
        });
    }

    [Test]
    public async Task Enum_member_name_collisions_preserve_distinct_wire_values()
    {
        var id = Guid.NewGuid();
        var handler = new StubHandler((request, _) =>
        {
            Assert.That(request.RequestUri!.Query, Does.Contain("mode=fast_mode"));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { id, title = "Generated", completed = false })
            });
        });

        var client = CreateClient(handler);
        await client.GetTodoAsync(id, null, null, GetTodoMode.FastMode2, null);
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
    public void Schema_fingerprints_distinguish_requiredness_and_additional_properties()
    {
        _ = new FingerprintTest { Value = "required" };
        _ = new FingerprintTestModel
        {
            AdditionalProperties = new Dictionary<string, JsonElement>()
        };
    }

    [Test]
    public void Infrastructure_type_names_are_reserved_from_component_dtos()
    {
        _ = new ClientTestClientModel();
        _ = new ClientTestClientExceptionModel();
        _ = new ClientTestClientServiceCollectionExtensionsModel();
    }

    [Test]
    public async Task Normalized_operation_id_collisions_get_unique_method_names()
    {
        var seen = new List<string>();
        var handler = new StubHandler((request, _) =>
        {
            seen.Add(request.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        });

        var client = CreateClient(handler);
        await client.GetUserAsync();
        await client.GetUser2Async();

        Assert.That(seen, Is.EqualTo(new[] { "/collision-one", "/collision-two" }));
    }

    [Test]
    public async Task Method_parameter_names_do_not_collide_with_body_or_cancellation_token()
    {
        var handler = new StubHandler((request, _) =>
        {
            Assert.That(request.RequestUri!.Query, Is.EqualTo("?body=one&cancellationToken=two"));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        });

        var client = CreateClient(handler);
        await client.ParameterCollisionAsync(
            body2: "one",
            cancellationToken2: "two",
            @class: null,
            path2: null,
            body: new ParameterCollisionRequest { Value = "payload" });
    }

    [Test]
    public async Task Operation_level_parameter_overrides_path_level_parameter()
    {
        var handler = new StubHandler((request, _) =>
        {
            Assert.That(request.RequestUri!.AbsolutePath, Is.EqualTo("/parameter-override/42"));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        });

        var client = CreateClient(handler);
        await client.ParameterOverrideAsync(42);
    }

    [Test]
    public void Reserved_names_and_escaped_enum_values_preserve_wire_contracts()
    {
        var value = new NameCollision { NameCollision2 = "name", AdditionalProperties2 = "value" };
        Assert.That(JsonSerializer.Serialize(value), Does.Contain("\"additionalProperties\":\"value\""));
        Assert.That(JsonSerializer.Deserialize<WireValue>("\"line\\nbreak\""), Is.EqualTo(WireValue.LineBreak));
        _ = new HttpClientModel();
        _ = WireValue.WireValue2;
    }

    [Test]
    public async Task Relative_routes_preserve_a_base_address_path()
    {
        var handler = new StubHandler((request, _) =>
        {
            Assert.That(request.RequestUri!.AbsolutePath, Is.EqualTo("/v1/collision-one"));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        });

        var client = CreateClient(handler, new Uri("https://example.test/v1"));
        await client.GetUserAsync();
    }

    [Test]
    public async Task Optional_request_body_is_omitted_when_null()
    {
        var handler = new StubHandler((request, _) =>
        {
            Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request.RequestUri!.PathAndQuery, Is.EqualTo("/optional-body"));
            Assert.That(request.Content, Is.Null);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        });

        var client = CreateClient(handler);
        await client.OptionalBodyAsync();
    }

    [Test]
    public async Task Nullable_array_query_parameter_can_be_null()
    {
        var handler = new StubHandler((request, _) =>
        {
            Assert.That(request.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.RequestUri!.PathAndQuery, Is.EqualTo("/nullable-array"));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        });

        var client = CreateClient(handler);
        await client.NullableArrayAsync(null);
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
            await client.GetTodoAsync(Guid.NewGuid(), null, null, null, null));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(exception.ResponseBody, Is.EqualTo("missing"));
        });
    }

    [Test]
    public void Typed_success_with_empty_content_throws_json_exception()
    {
        var handler = new StubHandler((request, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = null
            }));

        var client = CreateClient(handler);

        Assert.ThrowsAsync<JsonException>(async () =>
            await client.GetTodoAsync(Guid.NewGuid(), null, null, null, null));
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

    private static ClientTestClient CreateClient(HttpMessageHandler handler, Uri? baseAddress = null)
        => new(new HttpClient(handler) { BaseAddress = baseAddress ?? new Uri("https://example.test") });

    private sealed class StubHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => handler(request, cancellationToken);
    }
}