namespace MinimalOpenAPI.Generator.Tests;

/// <summary>
/// Tests for inline schema handling: schemas defined directly inside
/// <c>requestBody</c> or <c>responses</c> (without a <c>$ref</c> to a named component).
/// </summary>
[TestFixture]
public class InlineSchemaGenerationTests
{
    private static readonly (string, string)[] AdditionalFiles =
    [
        ("openapi.yaml", OpenApiFixtures.CreateOrderWithInlineSchemasYaml)
    ];

    private const string CreateOrderHandlerImpl = """
        public class CreateOrderHandler : CreateOrderEndpointBase
        {
            public override System.Threading.Tasks.Task<object> Handle(
                object request,
                System.Threading.CancellationToken ct) => throw new System.NotImplementedException();
        }
        """;

    [Test]
    public void InlineRequestBodySchemaIsPromotedToNamedDto()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: CreateOrderHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        // The inline requestBody schema should be named "{PascalCase(operationId)}Request"
        Assert.That(source, Does.Contain("public sealed record CreateOrderRequest"));
        Assert.That(source, Does.Contain("[JsonPropertyName(\"productId\")]"));
        Assert.That(source, Does.Contain("[JsonPropertyName(\"quantity\")]"));
    }

    [Test]
    public void InlineResponseSchemaIsPromotedToNamedDto()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: CreateOrderHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        // The inline 201 response schema should be named "{PascalCase(operationId)}{StatusCode}Response"
        Assert.That(source, Does.Contain("public sealed record CreateOrder201Response"));
        Assert.That(source, Does.Contain("[JsonPropertyName(\"orderId\")]"));
    }

    [Test]
    public void HandlerBaseUsesPromotedInlineSchemaTypesWithFullyQualifiedNames()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: CreateOrderHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "CreateOrderEndpointBase.g.cs");

        // Request body parameter should reference the promoted DTO
        Assert.That(source, Does.Contain("global::TestProject.Contracts.CreateOrderRequest request"));
        // Return type should reference the promoted response DTO
        Assert.That(source, Does.Contain("global::TestProject.Contracts.CreateOrder201Response"));
    }

    [Test]
    public void EndpointMappingUsesPromotedInlineSchemaTypesWithFullyQualifiedNames()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: CreateOrderHandlerImpl,
            additionalFiles: AdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");

        // Lambda parameter should reference the promoted request DTO
        Assert.That(source, Does.Contain("global::TestProject.Contracts.CreateOrderRequest request"));
        // Produces<> should reference the promoted response DTO
        Assert.That(source, Does.Contain("global::TestProject.Contracts.CreateOrder201Response"));
    }
}
