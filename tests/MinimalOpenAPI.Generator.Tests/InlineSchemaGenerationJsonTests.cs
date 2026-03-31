namespace MinimalOpenAPI.Generator.Tests;

/// <summary>
/// Tests for inline schema handling from a JSON OpenAPI spec: schemas defined directly inside
/// <c>requestBody</c> or <c>responses</c> (without a <c>$ref</c> to a named component).
/// </summary>
[TestFixture]
public class InlineSchemaGenerationJsonTests
{
    private static readonly (string, string)[] AdditionalFiles =
    [
        ("openapi.json", OpenApiFixtures.CreateOrderWithInlineSchemasJson)
    ];

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

        Assert.That(source, Does.Contain("public sealed record CreatedResponse"));
        Assert.That(source, Does.Contain("[global::System.Text.Json.Serialization.JsonPropertyName(\"orderId\")]"));
    }

    [Test]
    public void InlineSchemasAreNotGeneratedInDtosFile()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: CreateOrderHandlerImpl,
            additionalFiles: AdditionalFiles);

        var dtosFile = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.EndsWith("Dtos.g.cs", StringComparison.OrdinalIgnoreCase));
        Assert.That(dtosFile, Is.Null,
            "Inline schemas must NOT produce top-level entries in Dtos.g.cs; " +
            "they live as nested types in the endpoint base class.");
    }
}