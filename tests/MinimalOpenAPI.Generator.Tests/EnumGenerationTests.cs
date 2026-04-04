namespace MinimalOpenAPI.Generator.Tests;

/// <summary>
/// Tests for enum generation: top-level <c>enum</c> schemas and inline enum properties.
/// </summary>
[TestFixture]
public class EnumGenerationTests
{
    // ── Top-level enum schema (YAML) ──────────────────────────────────────

    [Test]
    public void TopLevelEnumSchema_GeneratesEnumType_Yaml()
    {
        var additionalFiles = new[] { ("openapi.yaml", OpenApiFixtures.GetOrderWithEnumYaml) };
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: additionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("public enum OrderStatus"));
    }

    [Test]
    public void TopLevelEnumSchema_HasJsonStringEnumConverterAttribute_Yaml()
    {
        var additionalFiles = new[] { ("openapi.yaml", OpenApiFixtures.GetOrderWithEnumYaml) };
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: additionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("[JsonConverter(typeof(JsonStringEnumConverter))]"));
    }

    [Test]
    public void TopLevelEnumSchema_HasCorrectMembers_Yaml()
    {
        var additionalFiles = new[] { ("openapi.yaml", OpenApiFixtures.GetOrderWithEnumYaml) };
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: additionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("Pending"));
        Assert.That(source, Does.Contain("Active"));
        Assert.That(source, Does.Contain("Cancelled"));
    }

    [Test]
    public void TopLevelEnumSchema_DoesNotGenerateRecordForEnumSchema_Yaml()
    {
        var additionalFiles = new[] { ("openapi.yaml", OpenApiFixtures.GetOrderWithEnumYaml) };
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: additionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        // The enum schema must NOT produce a record.
        Assert.That(source, Does.Not.Contain("public sealed record OrderStatus"));
    }

    [Test]
    public void ObjectSchema_WithRefToEnumProperty_UsesEnumType_Yaml()
    {
        var additionalFiles = new[] { ("openapi.yaml", OpenApiFixtures.GetOrderWithEnumYaml) };
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: additionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        // The Order record's Status property must use the OrderStatus enum type.
        Assert.That(source, Does.Contain("public sealed record Order"));
        Assert.That(source, Does.Contain("public required OrderStatus Status { get; init; }"));
    }

    // ── Inline enum property (YAML) ───────────────────────────────────────

    [Test]
    public void InlineEnumProperty_GeneratesDerivedEnumType_Yaml()
    {
        var additionalFiles = new[] { ("openapi.yaml", OpenApiFixtures.GetProductWithInlineEnumYaml) };
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: additionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        // Inline enum on Product.category → derived name "ProductCategory"
        Assert.That(source, Does.Contain("public enum ProductCategory"));
    }

    [Test]
    public void InlineEnumProperty_HasJsonStringEnumConverterAttribute_Yaml()
    {
        var additionalFiles = new[] { ("openapi.yaml", OpenApiFixtures.GetProductWithInlineEnumYaml) };
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: additionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("[JsonConverter(typeof(JsonStringEnumConverter))]"));
    }

    [Test]
    public void InlineEnumProperty_HasCorrectMembers_Yaml()
    {
        var additionalFiles = new[] { ("openapi.yaml", OpenApiFixtures.GetProductWithInlineEnumYaml) };
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: additionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("Electronics"));
        Assert.That(source, Does.Contain("Clothing"));
        Assert.That(source, Does.Contain("Food"));
    }

    [Test]
    public void InlineEnumProperty_PropertyUsesGeneratedEnumType_Yaml()
    {
        var additionalFiles = new[] { ("openapi.yaml", OpenApiFixtures.GetProductWithInlineEnumYaml) };
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: additionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("public sealed record Product"));
        Assert.That(source, Does.Contain("public required ProductCategory Category { get; init; }"));
    }

    // ── Top-level enum schema (JSON) ──────────────────────────────────────

    [Test]
    public void TopLevelEnumSchema_GeneratesEnumType_Json()
    {
        var additionalFiles = new[] { ("openapi.json", OpenApiFixtures.GetOrderWithEnumJson) };
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: additionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("public enum OrderStatus"));
        Assert.That(source, Does.Contain("[JsonConverter(typeof(JsonStringEnumConverter))]"));
    }

    [Test]
    public void TopLevelEnumSchema_HasCorrectMembers_Json()
    {
        var additionalFiles = new[] { ("openapi.json", OpenApiFixtures.GetOrderWithEnumJson) };
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: additionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("Pending"));
        Assert.That(source, Does.Contain("Active"));
        Assert.That(source, Does.Contain("Cancelled"));
    }

    [Test]
    public void ObjectSchema_WithRefToEnumProperty_UsesEnumType_Json()
    {
        var additionalFiles = new[] { ("openapi.json", OpenApiFixtures.GetOrderWithEnumJson) };
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: additionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("public sealed record Order"));
        Assert.That(source, Does.Contain("public required OrderStatus Status { get; init; }"));
    }

    // ── Inline enum property (JSON) ───────────────────────────────────────

    [Test]
    public void InlineEnumProperty_GeneratesDerivedEnumType_Json()
    {
        var additionalFiles = new[] { ("openapi.json", OpenApiFixtures.GetProductWithInlineEnumJson) };
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: additionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("public enum ProductCategory"));
        Assert.That(source, Does.Contain("[JsonConverter(typeof(JsonStringEnumConverter))]"));
    }

    [Test]
    public void InlineEnumProperty_PropertyUsesGeneratedEnumType_Json()
    {
        var additionalFiles = new[] { ("openapi.json", OpenApiFixtures.GetProductWithInlineEnumJson) };
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: additionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("public sealed record Product"));
        Assert.That(source, Does.Contain("public required ProductCategory Category { get; init; }"));
    }

    // ── Numeric enum values ───────────────────────────────────────────────

    [Test]
    public void NumericEnumValues_GenerateValidCSharpMemberNames()
    {
        var additionalFiles = new[] { ("openapi.yaml", OpenApiFixtures.GetOrderWithNumericEnumYaml) };
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: additionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        // Integer values 0, 1, 2 must be prefixed with "Value" to form valid C# identifiers.
        Assert.That(source, Does.Contain("public enum OrderPriority"));
        Assert.That(source, Does.Contain("Value0"));
        Assert.That(source, Does.Contain("Value1"));
        Assert.That(source, Does.Contain("Value2"));
    }

    [Test]
    public void NumericEnumValues_DoNotProduceDigitLeadingIdentifier()
    {
        var additionalFiles = new[] { ("openapi.yaml", OpenApiFixtures.GetOrderWithNumericEnumYaml) };
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: additionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        // Neither a bare digit nor an underscore-prefixed digit must appear as a member name.
        Assert.That(source, Does.Not.Contain("    0,"));
        Assert.That(source, Does.Not.Contain("    1,"));
        Assert.That(source, Does.Not.Contain("    _0"));
        Assert.That(source, Does.Not.Contain("    _1"));
    }

    // ── [ExcludeFromCodeCoverage] must not be placed on enum declarations ─

    [Test]
    public void TopLevelEnumSchema_DoesNotEmitExcludeFromCodeCoverageOnEnum()
    {
        // [ExcludeFromCodeCoverage] is invalid on enum declarations (CS0592).
        // Only [GeneratedCode] should appear immediately before the enum keyword.
        var additionalFiles = new[] { ("openapi.yaml", OpenApiFixtures.GetOrderWithEnumYaml) };
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: additionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        // Grab the block around the enum declaration and confirm no ExcludeFromCodeCoverage
        // attribute appears in it.
        var enumIndex = source.IndexOf("public enum OrderStatus", StringComparison.Ordinal);
        Assert.That(enumIndex, Is.GreaterThan(0), "Expected to find enum declaration");
        var preceding = source[..enumIndex];
        var lastNewline = preceding.LastIndexOf('\n');
        var block = lastNewline >= 0 ? preceding[lastNewline..] : preceding;
        Assert.That(block, Does.Not.Contain("ExcludeFromCodeCoverage"));
    }

    // ── $ref enum in Parameters record must use fully-qualified type ──────

    [Test]
    public void RefEnumQueryParameter_ParametersRecord_UsesFullyQualifiedType()
    {
        var additionalFiles = new[] { ("openapi.yaml", OpenApiFixtures.ListOrdersWithEnumQueryParamYaml) };
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: additionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "ListOrdersEndpointBase.g.cs");

        // The Parameters record must reference OrderStatus with the full contracts namespace.
        Assert.That(source, Does.Contain("global::TestProject.Openapi.Contracts.OrderStatus"));
    }

    // ── $ref enum in inline request body must use fully-qualified type ────

    [Test]
    public void RefEnumInInlineRequestBody_UsesFullyQualifiedType()
    {
        var additionalFiles = new[] { ("openapi.yaml", OpenApiFixtures.CreateOrderWithEnumRequestBodyYaml) };
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: additionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "CreateOrderEndpointBase.g.cs");

        // The inline Request record must reference OrderStatus with the full contracts namespace.
        Assert.That(source, Does.Contain("global::TestProject.Openapi.Contracts.OrderStatus"));
    }
}