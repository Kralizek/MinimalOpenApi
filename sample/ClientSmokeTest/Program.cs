using System.Net;
using System.Net.Http.Json;

using ClientSmokeTest.Clients.Backend;

using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddBackendClient(new Uri("https://example.test"));
services.AddHttpClient<BackendClient>().ConfigurePrimaryHttpMessageHandler(() => new SmokeHandler());

using var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<BackendClient>();

var todo = await client.GetTodoAsync(Guid.Empty);
if (todo.Id != Guid.Empty || todo.Title != "smoke")
	throw new InvalidOperationException("The generated client did not deserialize the expected response.");

Console.WriteLine("Generated client package smoke test passed.");

internal sealed class SmokeHandler : HttpMessageHandler
{
	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (request.Method != HttpMethod.Get || request.RequestUri?.PathAndQuery != $"/todos/{Guid.Empty}")
			throw new InvalidOperationException("The generated client sent an unexpected request.");

		return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = JsonContent.Create(new Todo { Id = Guid.Empty, Title = "smoke" })
		});
	}
}