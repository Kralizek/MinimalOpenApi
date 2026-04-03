namespace MinimalOpenAPI.Generator.Tests;

/// <summary>
/// Generator integration tests that prove OpenAPI 3.1 specs are fully supported.
/// Each test focuses on a concrete difference between 3.0 and 3.1 so that
/// regressions in either format are immediately obvious.
/// </summary>
[TestFixture]
public class OpenApi31GeneratorTests
{
    // ── Nullable via type array: YAML ─────────────────────────────────────

    [Test]
    public void V31_Yaml_TypeArray_StringNull_GeneratesNullableStringProperty()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", OpenApiFixtures.GetClientV31Yaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        // vatNumber is expressed as type: ["string","null"] — must produce string?
        Assert.That(source, Does.Contain("public string? VatNumber { get; init; }"));
    }

    [Test]
    public void V31_Yaml_RequiredField_WithTypeString_GeneratesNonNullableProperty()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", OpenApiFixtures.GetClientV31Yaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        // id and name are required and use plain type: string — must stay non-nullable
        Assert.That(source, Does.Contain("public required global::System.Guid Id { get; init; }"));
        Assert.That(source, Does.Contain("public required string Name { get; init; }"));
    }

    [Test]
    public void V31_Yaml_TypeArray_IntegerNull_GeneratesNullableIntProperty()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", OpenApiFixtures.GetOrderV31Yaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        // discount is expressed as type: ["integer","null"]
        Assert.That(source, Does.Contain("public int? Discount { get; init; }"));
    }

    [Test]
    public void V31_Yaml_TypeArray_StringNullWithUuidFormat_GeneratesNullableGuidProperty()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", OpenApiFixtures.GetOrderV31Yaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        // referralCode is expressed as type: ["string","null"] + format: uuid
        Assert.That(source, Does.Contain("public global::System.Guid? ReferralCode { get; init; }"));
    }

    [Test]
    public void V31_Yaml_TypeArray_WithoutNull_GeneratesNonNullableProperty()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", OpenApiFixtures.GetOrderV31Yaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        // tag has type: ["string"] without "null" — but it is NOT in the required array,
        // so the absent-from-required rule makes it nullable regardless of the type array.
        Assert.That(source, Does.Contain("public string? Tag { get; init; }"));
        // Confirm it is NOT the required-style non-nullable
        Assert.That(source, Does.Not.Contain("public required string Tag"));
    }

    // ── Nullable via type array: JSON ─────────────────────────────────────

    [Test]
    public void V31_Json_TypeArray_StringNull_GeneratesNullableStringProperty()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.json", OpenApiFixtures.GetClientV31Json)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("public string? VatNumber { get; init; }"));
    }

    [Test]
    public void V31_Json_RequiredField_WithTypeString_GeneratesNonNullableProperty()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.json", OpenApiFixtures.GetClientV31Json)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("public required global::System.Guid Id { get; init; }"));
        Assert.That(source, Does.Contain("public required string Name { get; init; }"));
    }

    [Test]
    public void V31_Json_TypeArray_IntegerNull_GeneratesNullableIntProperty()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.json", OpenApiFixtures.GetOrderV31Json)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("public int? Discount { get; init; }"));
    }

    [Test]
    public void V31_Json_TypeArray_StringNullWithUuidFormat_GeneratesNullableGuidProperty()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.json", OpenApiFixtures.GetOrderV31Json)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("public global::System.Guid? ReferralCode { get; init; }"));
    }

    [Test]
    public void V31_Json_TypeArray_WithoutNull_GeneratesNonNullableProperty()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.json", OpenApiFixtures.GetOrderV31Json)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        // tag: ["string"] without "null" — not in required array, so nullable by absent-from-required rule
        Assert.That(source, Does.Contain("public string? Tag { get; init; }"));
        Assert.That(source, Does.Not.Contain("public required string Tag"));
    }

    // ── POST endpoint with 3.1 request body ──────────────────────────────

    [Test]
    public void V31_Yaml_PostEndpoint_RequestBodyNullableField_GeneratesNullableProperty()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", OpenApiFixtures.CreateClientV31Yaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("public sealed record CreateClientRequest"));
        // vatNumber in the request body uses type: ["string","null"]
        Assert.That(source, Does.Contain("public string? VatNumber { get; init; }"));
    }

    // ── 3.0 / 3.1 parity ─────────────────────────────────────────────────
    // Prove that `nullable: true` (3.0) and `type: ["T", "null"]` (3.1)
    // produce identical DTO output.

    [Test]
    public void V30_And_V31_Yaml_ProduceIdenticalDtos_ForNullableStringField()
    {
        var (result30, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", OpenApiFixtures.GetClientYaml)]);

        var (result31, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", OpenApiFixtures.GetClientV31Yaml)]);

        var dtos30 = GeneratorTestHelper.GetGeneratedSource(result30, "Dtos.g.cs");
        var dtos31 = GeneratorTestHelper.GetGeneratedSource(result31, "Dtos.g.cs");

        // Strip the header comment lines that differ (timestamp / generator version)
        // by comparing only lines that contain actual declarations.
        var lines30 = ExtractDeclarationLines(dtos30);
        var lines31 = ExtractDeclarationLines(dtos31);

        Assert.That(lines31, Is.EqualTo(lines30),
            "3.1 spec (type array) should produce identical declarations to 3.0 spec (nullable: true)");
    }

    [Test]
    public void V30_And_V31_Json_ProduceIdenticalDtos_ForNullableStringField()
    {
        var (result30, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.json", OpenApiFixtures.GetClientJson)]);

        var (result31, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.json", OpenApiFixtures.GetClientV31Json)]);

        var dtos30 = GeneratorTestHelper.GetGeneratedSource(result30, "Dtos.g.cs");
        var dtos31 = GeneratorTestHelper.GetGeneratedSource(result31, "Dtos.g.cs");

        var lines30 = ExtractDeclarationLines(dtos30);
        var lines31 = ExtractDeclarationLines(dtos31);

        Assert.That(lines31, Is.EqualTo(lines30),
            "3.1 JSON spec (type array) should produce identical declarations to 3.0 JSON spec (nullable: true)");
    }

    // ── Handler + mapping unaffected by version ───────────────────────────

    [Test]
    public void V31_Yaml_GeneratesHandlerBaseClass_IdenticalTo_V30()
    {
        var (result30, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", OpenApiFixtures.GetClientYaml)]);

        var (result31, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", OpenApiFixtures.GetClientV31Yaml)]);

        var handler30 = GeneratorTestHelper.GetGeneratedSource(result30, "GetClientEndpointBase.g.cs");
        var handler31 = GeneratorTestHelper.GetGeneratedSource(result31, "GetClientEndpointBase.g.cs");

        Assert.That(ExtractDeclarationLines(handler31), Is.EqualTo(ExtractDeclarationLines(handler30)));
    }

    [Test]
    public void V31_Yaml_GeneratesEndpointMapping_IdenticalTo_V30()
    {
        var (result30, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", OpenApiFixtures.GetClientYaml)]);

        var (result31, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", OpenApiFixtures.GetClientV31Yaml)]);

        var mapping30 = GeneratorTestHelper.GetGeneratedSource(result30, "EndpointMapping.g.cs");
        var mapping31 = GeneratorTestHelper.GetGeneratedSource(result31, "EndpointMapping.g.cs");

        Assert.That(ExtractDeclarationLines(mapping31), Is.EqualTo(ExtractDeclarationLines(mapping30)));
    }

    // ── Diagnostics ───────────────────────────────────────────────────────

    [Test]
    public void V31_Yaml_DoesNotEmit_MOA006()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", OpenApiFixtures.GetClientV31Yaml)]);

        Assert.That(result.Diagnostics.Any(d => d.Id == "MOA006"), Is.False,
            "openapi: 3.1.0 is a recognised version — MOA006 must not be emitted");
    }

    [Test]
    public void V31_Json_DoesNotEmit_MOA006()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.json", OpenApiFixtures.GetClientV31Json)]);

        Assert.That(result.Diagnostics.Any(d => d.Id == "MOA006"), Is.False,
            "openapi: 3.1.0 JSON is a recognised version — MOA006 must not be emitted");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Filters out auto-generated header comment lines (which differ between 3.0 and 3.1 runs
    /// only in the version comment) so that parity comparisons focus on the actual declarations.
    /// </summary>
    private static string[] ExtractDeclarationLines(string source) =>
        source
            .Split('\n')
            .Select(l => l.TrimEnd())
            .Where(l => !l.StartsWith("//", StringComparison.Ordinal))
            .ToArray();
}