using MinimalOpenAPI.Abstractions.Models;
using MinimalOpenAPI.Parser.Json;
using MinimalOpenAPI.Parser.Yaml;

namespace MinimalOpenAPI.Generator.Tests;

/// <summary>Tests for <c>IOpenApiParser.CanParse</c> — format and version-based parser selection.</summary>
[TestFixture]
public class CanParseTests
{
    // ── YamlOpenApiParser ─────────────────────────────────────────────────

    [Test]
    public void YamlParser_CanParse_Returns_True_For_Yaml_Format()
    {
        var parser = new YamlOpenApiParser();

        Assert.That(parser.CanParse(new OpenApiParserRequest(OpenApiFormat.Yaml, null)), Is.True);
    }

    [Test]
    public void YamlParser_CanParse_Returns_True_Regardless_Of_Version()
    {
        var parser = new YamlOpenApiParser();

        Assert.That(parser.CanParse(new OpenApiParserRequest(OpenApiFormat.Yaml, new Version(3, 0, 0))), Is.True);
        Assert.That(parser.CanParse(new OpenApiParserRequest(OpenApiFormat.Yaml, new Version(3, 1, 0))), Is.True);
        Assert.That(parser.CanParse(new OpenApiParserRequest(OpenApiFormat.Yaml, new Version(4, 0, 0))), Is.True);
    }

    [TestCase(OpenApiFormat.Json)]
    [TestCase(OpenApiFormat.Unknown)]
    public void YamlParser_CanParse_Returns_False_For_Non_Yaml_Format(OpenApiFormat format)
    {
        var parser = new YamlOpenApiParser();

        Assert.That(parser.CanParse(new OpenApiParserRequest(format, null)), Is.False);
    }

    // ── JsonOpenApiParser ─────────────────────────────────────────────────

    [Test]
    public void JsonParser_CanParse_Returns_True_For_Json_Format()
    {
        var parser = new JsonOpenApiParser();

        Assert.That(parser.CanParse(new OpenApiParserRequest(OpenApiFormat.Json, null)), Is.True);
    }

    [Test]
    public void JsonParser_CanParse_Returns_True_Regardless_Of_Version()
    {
        var parser = new JsonOpenApiParser();

        Assert.That(parser.CanParse(new OpenApiParserRequest(OpenApiFormat.Json, new Version(3, 0, 0))), Is.True);
        Assert.That(parser.CanParse(new OpenApiParserRequest(OpenApiFormat.Json, new Version(3, 1, 0))), Is.True);
        Assert.That(parser.CanParse(new OpenApiParserRequest(OpenApiFormat.Json, new Version(4, 0, 0))), Is.True);
    }

    [TestCase(OpenApiFormat.Yaml)]
    [TestCase(OpenApiFormat.Unknown)]
    public void JsonParser_CanParse_Returns_False_For_Non_Json_Format(OpenApiFormat format)
    {
        var parser = new JsonOpenApiParser();

        Assert.That(parser.CanParse(new OpenApiParserRequest(format, null)), Is.False);
    }

    // ── Version-targeted parser pattern ──────────────────────────────────

    [Test]
    public void Version_Targeted_Parser_Pattern_Accepts_Its_Major_Version_Only()
    {
        // Demonstrates how a future version-targeted parser would implement CanParse.
        // Only accepts YAML documents whose major version is 4.
        static bool canParseYamlV4(OpenApiParserRequest request) =>
            request.Format == OpenApiFormat.Yaml && request.Version?.Major == 4;

        Assert.That(canParseYamlV4(new OpenApiParserRequest(OpenApiFormat.Yaml, new Version(4, 0, 0))), Is.True);
        Assert.That(canParseYamlV4(new OpenApiParserRequest(OpenApiFormat.Yaml, new Version(4, 1, 0))), Is.True);
        Assert.That(canParseYamlV4(new OpenApiParserRequest(OpenApiFormat.Yaml, new Version(3, 1, 0))), Is.False);
        Assert.That(canParseYamlV4(new OpenApiParserRequest(OpenApiFormat.Yaml, null)), Is.False);
        Assert.That(canParseYamlV4(new OpenApiParserRequest(OpenApiFormat.Json, new Version(4, 0, 0))), Is.False);
    }

    // ── Generator-level selection ─────────────────────────────────────────

    [Test]
    public void Generator_Emits_MOA005_For_Unsupported_Extension()
    {
        const string content = """
            openapi: "3.0.0"
            info:
              title: Test
              version: "1.0"
            paths: {}
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.toml", content)]);

        Assert.That(result.Diagnostics.Any(d => d.Id == "MOA005"), Is.True,
            "Expected MOA005 error for unsupported file extension");
    }

    [Test]
    public void Generator_Does_Not_Emit_MOA005_For_Yaml_Extension()
    {
        const string content = """
            openapi: "3.0.0"
            info:
              title: Test
              version: "1.0"
            paths: {}
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", content)]);

        Assert.That(result.Diagnostics.Any(d => d.Id == "MOA005"), Is.False,
            "Should not emit MOA005 for .yaml extension");
    }

    [Test]
    public void Generator_Does_Not_Emit_MOA005_For_Json_Extension()
    {
        const string content = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {}
            }
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.json", content)]);

        Assert.That(result.Diagnostics.Any(d => d.Id == "MOA005"), Is.False,
            "Should not emit MOA005 for .json extension");
    }
}