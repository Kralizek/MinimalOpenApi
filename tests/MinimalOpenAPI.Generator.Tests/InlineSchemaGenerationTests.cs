namespace MinimalOpenAPI.Generator.Tests;

/// <summary>
/// Tests for inline schema handling: schemas defined directly inside
/// <c>requestBody</c> or <c>responses</c> (without a <c>$ref</c> to a named component).
/// These schemas become nested <c>sealed record</c> types inside the endpoint base class
/// rather than top-level types in the Contracts namespace.
/// </summary>
[TestFixture]
public class InlineSchemaGenerationTests
{
    private static readonly (string, string)[] AdditionalFiles =
    [
        ("openapi.yaml", OpenApiFixtures.CreateOrderWithInlineSchemasYaml)
    ];

    // A handler implementation that uses the correct strongly-typed nested record types.
    // This verifies the generated base class is usable with the expected inline DTO names.
    private const string CreateOrderHandlerImpl = """
        public class CreateOrderHandler : CreateOrderEndpointBase
        {
            public override System.Threading.Tasks.Task<
                global::Microsoft.AspNetCore.Http.HttpResults.Results<
                    global::Microsoft.AspNetCore.Http.HttpResults.Created<CreatedResponse>,
                    global::Microsoft.AspNetCore.Http.HttpResults.BadRequest>> HandleAsync(
                Request request,
                System.Threading.CancellationToken ct) => throw new System.NotImplementedException();
        }
        """;

    [Test]
    public void InlineRequestBodySchemaBecomesNestedRecordInBaseClass()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: CreateOrderHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "CreateOrderEndpointBase.g.cs");

        // Nested record "Request" should be inside the endpoint base class.
        Assert.That(source, Does.Contain("public sealed record Request"));
        Assert.That(source, Does.Contain("[global::System.Text.Json.Serialization.JsonPropertyName(\"productId\")]"));
        Assert.That(source, Does.Contain("[global::System.Text.Json.Serialization.JsonPropertyName(\"quantity\")]"));
    }

    [Test]
    public void InlineResponseSchemaBecomesNestedRecordInBaseClass()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: CreateOrderHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "CreateOrderEndpointBase.g.cs");

        // Nested record "CreatedResponse" (status 201) should be inside the endpoint base class.
        Assert.That(source, Does.Contain("public sealed record CreatedResponse"));
        Assert.That(source, Does.Contain("[global::System.Text.Json.Serialization.JsonPropertyName(\"orderId\")]"));
    }

    [Test]
    public void HandleAsyncUsesShortNestedTypeNamesInsideBaseClass()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: CreateOrderHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "CreateOrderEndpointBase.g.cs");

        // HandleAsync uses the short nested type names (they are in scope inside the class).
        Assert.That(source, Does.Contain("Request request"));
        Assert.That(source, Does.Contain("Created<CreatedResponse>"));
    }

    [Test]
    public void EndpointMappingUsesFullyQualifiedNestedTypeNames()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: CreateOrderHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");

        // From outside the class, the nested types must be fully qualified.
        Assert.That(source, Does.Contain("global::TestProject.Endpoints.CreateOrderEndpointBase.Request request"));
        Assert.That(source, Does.Contain("global::TestProject.Endpoints.CreateOrderEndpointBase.CreatedResponse"));
    }

    [Test]
    public void InlineSchemasAreNotGeneratedInDtosFile()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: CreateOrderHandlerImpl,
            additionalFiles: AdditionalFiles);

        // No component schemas → no Dtos.g.cs file at all.
        var dtosFile = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.EndsWith("Dtos.g.cs", StringComparison.OrdinalIgnoreCase));
        Assert.That(dtosFile, Is.Null,
            "Inline schemas must NOT produce top-level entries in Dtos.g.cs; " +
            "they live as nested types in the endpoint base class.");
    }
}