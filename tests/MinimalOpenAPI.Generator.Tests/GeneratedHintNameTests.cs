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
            additionalFiles: [("openapi.yaml", OpenApiFixtures.GetClientYaml)]);

        var generatedPaths = result.GeneratedTrees
            .Select(t => t.FilePath.Replace('\\', '/'))
            .ToArray();

        Assert.That(generatedPaths.Any(p => p.EndsWith("MinimalOpenApi/Openapi/Schemas/Openapi.Dtos.g.cs", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(generatedPaths.Any(p => p.EndsWith("MinimalOpenApi/Openapi/Operations/Openapi.GetClientEndpointBase.g.cs", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(generatedPaths.Any(p => p.EndsWith("MinimalOpenApi/Openapi/Operations/Openapi.GetClientEndpointRegistration.g.cs", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(generatedPaths.Any(p => p.EndsWith("MinimalOpenApi/Openapi/Infrastructure/Openapi.DependencyInjection.g.cs", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(generatedPaths.Any(p => p.EndsWith("MinimalOpenApi/Openapi/Infrastructure/Openapi.EndpointMapping.g.cs", StringComparison.OrdinalIgnoreCase)), Is.True);
    }
}