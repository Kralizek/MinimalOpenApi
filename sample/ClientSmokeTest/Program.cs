using ClientSmokeTest.Generated;

using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddBackendClient(new Uri("https://example.test"));

using var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<BackendClient>();

_ = client.GetTodoAsync(Guid.Empty);
_ = new Todo { Id = Guid.Empty, Title = "smoke" };