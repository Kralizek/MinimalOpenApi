namespace MinimalOpenAPI.Generator.Tests;

[TestFixture]
public class GeneratedHintNameTests
{
    private const string GetClientHandlerImpl = """
        public class GetClientHandler : GetClientEndpointBase
        {
            public override System.Threading.Tasks.Task<object> HandleAsync(
                System.Guid tenantId,
                System.Guid clientId,
                bool? includeDeleted,
                System.Threading.CancellationToken ct) => throw new System.NotImplementedException();
        }
        """;

    [Test]
    public void GeneratedSourcesUseStableStructuredHintNames()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetClientHandlerImpl,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.GetClientYaml)],
            specNameOverride: "Clients");

        var generatedPaths = result.GeneratedTrees
            .Select(t => t.FilePath.Replace('\\', '/'))
            .ToArray();

        Assert.That(generatedPaths.Any(p => p.EndsWith("MinimalOpenApi/Clients/Schemas/Clients.Dtos.g.cs", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(generatedPaths.Any(p => p.EndsWith("MinimalOpenApi/Clients/Operations/Clients.GetClientEndpointBase.g.cs", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(generatedPaths.Any(p => p.EndsWith("MinimalOpenApi/Clients/Operations/Clients.GetClientEndpointRegistration.g.cs", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(generatedPaths.Any(p => p.EndsWith("MinimalOpenApi/Clients/Infrastructure/Clients.DependencyInjection.g.cs", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(generatedPaths.Any(p => p.EndsWith("MinimalOpenApi/Clients/Infrastructure/Clients.EndpointMapping.g.cs", StringComparison.OrdinalIgnoreCase)), Is.True);
    }
}