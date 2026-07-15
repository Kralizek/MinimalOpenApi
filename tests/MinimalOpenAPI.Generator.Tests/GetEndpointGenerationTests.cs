namespace MinimalOpenAPI.Generator.Tests;

/// <summary>Tests for GET endpoint generation (path + query parameters).</summary>
[TestFixture]
public class GetEndpointGenerationTests
{
    private static readonly (string, string)[] AdditionalFiles =
    [
        ("openapi.yaml", OpenApiFixtures.GetClientYaml)
    ];

    [Test]
    public void GeneratesHandlerBaseClass()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetClientEndpointBase.g.cs");

        Assert.That(source, Does.Contain("public class GetClientEndpointBase"));
        Assert.That(source, Does.Contain("public virtual"));
        Assert.That(source, Does.Contain("HandleAsync("));
        Assert.That(source, Does.Contain("NotImplementedException"));
    }

    [Test]
    public void GeneratedHandlerHasCorrectHandleSignature()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetClientEndpointBase.g.cs");

        Assert.That(source, Does.Contain("System.Guid tenantId"));
        Assert.That(source, Does.Contain("System.Guid clientId"));
        Assert.That(source, Does.Contain("Parameters parameters"));
        Assert.That(source, Does.Contain("CancellationToken cancellationToken"));
        Assert.That(source, Does.Contain("HandleAsync("));
    }

    [Test]
    public void GeneratesParametersNestedRecord()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetClientEndpointBase.g.cs");

        Assert.That(source, Does.Contain("public sealed record Parameters"));
        Assert.That(source, Does.Contain("[global::Microsoft.AspNetCore.Mvc.FromQuery(Name = \"includeDeleted\")]"));
        Assert.That(source, Does.Contain("public bool? IncludeDeleted { get; init; }"));
    }

    [Test]
    public void GeneratesEndpointMappingWithAsParameters()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");

        Assert.That(source, Does.Contain("[global::Microsoft.AspNetCore.Http.AsParameters] global::TestProject.Openapi.Endpoints.GetClientEndpointBase.Parameters parameters"));
    }

    [Test]
    public void GeneratedHandlerHasCorrectReturnType()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetClientEndpointBase.g.cs");

        Assert.That(source, Does.Contain("Results<"));
        Assert.That(source, Does.Contain("Ok<global::TestProject.Openapi.Contracts.Client>"));
        Assert.That(source, Does.Contain("NotFound>"));
    }

    [Test]
    public void GeneratesEndpointConfigurationBaseClass()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetClientEndpointConfigurationBase.g.cs");

        Assert.That(source, Does.Contain("public abstract class GetClientEndpointConfigurationBase"));
        Assert.That(source, Does.Contain("public abstract void Configure("));
        Assert.That(source, Does.Contain("RouteHandlerBuilder endpoint"));
        Assert.That(source, Does.Not.Contain("EndpointRegistration"));
    }

    [Test]
    public void AppliesEndpointConfigurationAfterContractMetadata()
    {
        const string endpointConfiguration = """
            public sealed class GetClientEndpointConfiguration : GetClientEndpointConfigurationBase
            {
                public override void Configure(
                    global::Microsoft.AspNetCore.Builder.RouteHandlerBuilder endpoint)
                {
                }
            }
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetClientHandlerImpl + System.Environment.NewLine + endpointConfiguration,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");
        var contractMetadataIndex = source.IndexOf(".Produces", System.StringComparison.Ordinal);
        var applicationConfigurationIndex = source.IndexOf("Configuration?.Configure(", System.StringComparison.Ordinal);

        Assert.That(contractMetadataIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(applicationConfigurationIndex, Is.GreaterThan(contractMetadataIndex));
    }

    [Test]
    public void GeneratesDtoForClientSchema()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("public sealed record Client"));
        Assert.That(source, Does.Contain("public required global::System.Guid Id { get; init; }"));
        Assert.That(source, Does.Contain("public required string Name { get; init; }"));
        Assert.That(source, Does.Contain("public string? VatNumber { get; init; }"));
        Assert.That(source, Does.Contain("[JsonPropertyName(\"id\")]"));
        Assert.That(source, Does.Contain("[JsonPropertyName(\"name\")]"));
    }

    [Test]
    public void GeneratesEndpointMappingWithRouteConstraints()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");

        Assert.That(source, Does.Contain("/tenants/{tenantId:guid}/clients/{clientId:guid}"));
        Assert.That(source, Does.Contain("MapGet("));
        Assert.That(source, Does.Contain("WithName(\"getClient\")"));
        // The generated class is internal; the public MapMinimalOpenApiEndpoints lives in the runtime.
        Assert.That(source, Does.Contain("internal static class MinimalOpenApiGeneratedEndpointRouteBuilderExtensions"));
        Assert.That(source, Does.Contain("internal static"));
        Assert.That(source, Does.Contain("MapEndpoints("));
    }

    [Test]
    public void GeneratesEndpointMappingWithSummaryDescriptionAndTags()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");

        Assert.That(source, Does.Contain(".WithSummary(\"Get a specific client\")"));
        Assert.That(source, Does.Contain(".WithDescription(\"Returns the client with the specified identifier.\")"));
        Assert.That(source, Does.Contain(".WithTags(\"clients\")"));
    }

    [Test]
    public void GeneratesDiRegistration()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "DependencyInjection.g.cs");

        Assert.That(source, Does.Contain("AddGeneratedEndpoints("));
        Assert.That(source, Does.Contain("GetClientEndpointBase"));
        Assert.That(source, Does.Contain("GetClientHandler"));
        // ModuleInitializer must also register the endpoint mapping delegate.
        Assert.That(source, Does.Contain("RegisterEndpointMapping("));
        Assert.That(source, Does.Contain("MapEndpoints("));
    }

    [Test]
    public void GeneratesDiRegistration_WithoutSchemaId_DoesNotEmitRegisterSchemaFile()
    {
        // No schemaId means there is no generated internal copied path to register.
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetClientHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "DependencyInjection.g.cs");

        Assert.That(source, Does.Not.Contain("RegisterSchemaFile("));
    }

    [Test]
    public void GeneratesDiRegistration_WithSchemaId_EmitsRegisterSchemaFile()
    {
        // schemaId present -> RegisterSchemaFile must be emitted with the internal copied path.
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetClientHandlerImpl,
            additionalFiles: AdditionalFiles,
            schemaId: "987654321");

        var source = GeneratorTestHelper.GetGeneratedSource(result, "DependencyInjection.g.cs");

        Assert.That(source, Does.Contain("RegisterSchemaFile("));
        Assert.That(source, Does.Contain("openapi/schemas/987654321/openapi.yaml"));
    }

    [Test]
    public void GeneratesDiRegistration_WithPublishAs_EmitsRegisterSchemaFileWithExactPublicPath()
    {
        // PublishAs is passed through verbatim.
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetClientHandlerImpl,
            additionalFiles: AdditionalFiles,
            schemaId: "987654321",
            publishAs: "/contracts/public/v1/openapi.yaml");

        var source = GeneratorTestHelper.GetGeneratedSource(result, "DependencyInjection.g.cs");

        Assert.That(source, Does.Contain(
            "RegisterSchemaFile(\"openapi/schemas/987654321/openapi.yaml\", \"/contracts/public/v1/openapi.yaml\", null, null)"));
    }

    [Test]
    public void GeneratesDiRegistration_WithDisplayMetadata_EmitsRegisterSchemaFileWithDisplayArguments()
    {
        // Display metadata should be forwarded without parsing the file.
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetClientHandlerImpl,
            additionalFiles: AdditionalFiles,
            schemaId: "987654321",
            publishAs: "/openapi/schema.yaml",
            displayName: "Todo API",
            displayVersion: "1.0.0");

        var source = GeneratorTestHelper.GetGeneratedSource(result, "DependencyInjection.g.cs");

        Assert.That(source, Does.Contain(
            "RegisterSchemaFile(\"openapi/schemas/987654321/openapi.yaml\", \"/openapi/schema.yaml\", \"Todo API\", \"1.0.0\")"));
    }

    [Test]
    public void ReportsMissingHandlerImplementationDiagnostic()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "",
            additionalFiles: AdditionalFiles);

        Assert.That(result.Diagnostics, Has.Some.Matches<Microsoft.CodeAnalysis.Diagnostic>(d =>
            d.Id == "MOA001" &&
            d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Warning));
    }

    [Test]
    public void MissingHandlerDiagnosticIncludesFullyQualifiedTypeName()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "",
            additionalFiles: AdditionalFiles);

        var moa001 = result.Diagnostics.FirstOrDefault(d => d.Id == "MOA001");
        Assert.That(moa001, Is.Not.Null, "MOA001 should be emitted");
        Assert.That(moa001!.GetMessage(),
            Does.Contain("TestProject.Openapi.Endpoints.GetClientEndpointBase"),
            "MOA001 message must include the fully-qualified type name so users know exactly what to inherit from");
    }

    [Test]
    public void MissingHandlerDiagnosticHasOpenApiFileLocation()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "",
            additionalFiles: AdditionalFiles);

        var moa001 = result.Diagnostics.FirstOrDefault(d => d.Id == "MOA001");
        Assert.That(moa001, Is.Not.Null, "MOA001 should be emitted");
        Assert.That(moa001!.Location, Is.Not.EqualTo(Microsoft.CodeAnalysis.Location.None),
            "MOA001 must carry a source location so it is visible in the IDE Error List");
        Assert.That(moa001.Location.GetLineSpan().Path,
            Does.EndWith("openapi.yaml"),
            "MOA001 location should point to the OpenAPI spec file");
    }

    [Test]
    public void ReportsDuplicateHandlerImplementationDiagnostic()
    {
        var userSource = """
            public class HandlerA : GetClientEndpointBase
            {
                public override System.Threading.Tasks.Task<object> Handle(
                    System.Guid tenantId, System.Guid clientId, bool? includeDeleted,
                    System.Threading.CancellationToken ct) => throw new System.NotImplementedException();
            }
            public class HandlerB : GetClientEndpointBase
            {
                public override System.Threading.Tasks.Task<object> Handle(
                    System.Guid tenantId, System.Guid clientId, bool? includeDeleted,
                    System.Threading.CancellationToken ct) => throw new System.NotImplementedException();
            }
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: userSource,
            additionalFiles: AdditionalFiles);

        Assert.That(result.Diagnostics, Has.Some.Matches<Microsoft.CodeAnalysis.Diagnostic>(d => d.Id == "MOA002"));
    }

    [Test]
    public void DuplicateHandlerDiagnosticHasOpenApiFileLocation()
    {
        var userSource = """
            public class HandlerA : GetClientEndpointBase
            {
                public override System.Threading.Tasks.Task<object> Handle(
                    System.Guid tenantId, System.Guid clientId, bool? includeDeleted,
                    System.Threading.CancellationToken ct) => throw new System.NotImplementedException();
            }
            public class HandlerB : GetClientEndpointBase
            {
                public override System.Threading.Tasks.Task<object> Handle(
                    System.Guid tenantId, System.Guid clientId, bool? includeDeleted,
                    System.Threading.CancellationToken ct) => throw new System.NotImplementedException();
            }
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: userSource,
            additionalFiles: AdditionalFiles);

        var moa002 = result.Diagnostics.FirstOrDefault(d => d.Id == "MOA002");
        Assert.That(moa002, Is.Not.Null, "MOA002 should be emitted");
        Assert.That(moa002!.Location, Is.Not.EqualTo(Microsoft.CodeAnalysis.Location.None),
            "MOA002 must carry a source location so it is visible in the IDE Error List");
        Assert.That(moa002.Location.GetLineSpan().Path,
            Does.EndWith("openapi.yaml"),
            "MOA002 location should point to the OpenAPI spec file");
    }

    // A minimal implementation for tests that need exactly one handler
    private const string GetClientHandlerImpl = """
        public class GetClientHandler : GetClientEndpointBase
        {
            public override System.Threading.Tasks.Task<object> Handle(
                System.Guid tenantId, System.Guid clientId, bool? includeDeleted,
                System.Threading.CancellationToken ct) => throw new System.NotImplementedException();
        }
        """;
}