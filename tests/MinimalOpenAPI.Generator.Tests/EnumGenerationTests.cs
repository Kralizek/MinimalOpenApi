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
}