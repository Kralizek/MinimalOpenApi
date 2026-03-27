using FluentAssertions;

namespace MinimalOpenAPI.Generator.Tests;

/// <summary>Tests for GET endpoint generation (path + query parameters).</summary>
public class GetEndpointGenerationTests
{
    private static readonly (string, string)[] AdditionalFiles =
    [
        ("openapi.yaml", OpenApiFixtures.GetClientYaml)
    ];

    [Fact]
    public void GeneratesHandlerBaseClass()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetClientEndpoint.g.cs");

        source.Should().Contain("public abstract class GetClientEndpoint");
    }

    [Fact]
    public void GeneratedHandlerHasCorrectHandleSignature()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetClientEndpoint.g.cs");

        source.Should().Contain("System.Guid tenantId");
        source.Should().Contain("System.Guid clientId");
        source.Should().Contain("bool? includeDeleted");
        source.Should().Contain("CancellationToken cancellationToken");
    }

    [Fact]
    public void GeneratedHandlerHasCorrectReturnType()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetClientEndpoint.g.cs");

        source.Should().Contain("Results<");
        source.Should().Contain("Ok<Client>");
        source.Should().Contain("NotFound>");
    }

    [Fact]
    public void GeneratesRegistrationCustomizerBaseClass()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetClientEndpointRegistration.g.cs");

        source.Should().Contain("public abstract class GetClientEndpointRegistration");
        source.Should().Contain("Configure(");
        source.Should().Contain("RouteHandlerBuilder builder");
    }

    [Fact]
    public void GeneratesDtoForClientSchema()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        source.Should().Contain("public sealed record Client(");
        source.Should().Contain("System.Guid Id");
        source.Should().Contain("string Name");
        source.Should().Contain("string? VatNumber");
    }

    [Fact]
    public void GeneratesEndpointMappingWithRouteConstraints()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");

        source.Should().Contain("/tenants/{tenantId:guid}/clients/{clientId:guid}");
        source.Should().Contain("MapGet(");
        source.Should().Contain("WithName(\"getClient\")");
    }

    [Fact]
    public void GeneratesDiRegistration()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "DependencyInjection.g.cs");

        source.Should().Contain("AddGeneratedEndpoints(");
        source.Should().Contain("GetClientEndpoint");
        source.Should().Contain("GetClientHandler");
    }

    [Fact]
    public void ReportsMissingHandlerImplementationDiagnostic()
    {
        // No handler implementation provided
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "",
            additionalFiles: AdditionalFiles);

        var diagnostics = result.Diagnostics;
        diagnostics.Should().Contain(d => d.Id == "MOA001");
    }

    [Fact]
    public void ReportsDuplicateHandlerImplementationDiagnostic()
    {
        var userSource = """
            public class HandlerA : GetClientEndpoint
            {
                public override System.Threading.Tasks.Task<object> Handle(
                    System.Guid tenantId, System.Guid clientId, bool? includeDeleted,
                    System.Threading.CancellationToken ct) => throw new System.NotImplementedException();
            }
            public class HandlerB : GetClientEndpoint
            {
                public override System.Threading.Tasks.Task<object> Handle(
                    System.Guid tenantId, System.Guid clientId, bool? includeDeleted,
                    System.Threading.CancellationToken ct) => throw new System.NotImplementedException();
            }
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: userSource,
            additionalFiles: AdditionalFiles);

        var diagnostics = result.Diagnostics;
        diagnostics.Should().Contain(d => d.Id == "MOA002");
    }

    // A minimal implementation for tests that need exactly one handler
    private const string GetClientHandlerImpl = """
        public class GetClientHandler : GetClientEndpoint
        {
            public override System.Threading.Tasks.Task<object> Handle(
                System.Guid tenantId, System.Guid clientId, bool? includeDeleted,
                System.Threading.CancellationToken ct) => throw new System.NotImplementedException();
        }
        """;
}
