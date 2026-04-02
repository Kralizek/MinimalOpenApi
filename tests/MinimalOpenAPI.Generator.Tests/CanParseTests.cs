using MinimalOpenAPI.Parser.Json;
using MinimalOpenAPI.Parser.Yaml;

namespace MinimalOpenAPI.Generator.Tests;

/// <summary>Tests for <c>IOpenApiParser.CanParse</c> — format and version-based parser selection.</summary>
[TestFixture]
public class CanParseTests
{
    private const string MinimalYaml = """
        openapi: "3.0.0"
        info:
          title: Test
          version: "1.0"
        paths: {}
        """;

    private const string MinimalJson = """
        {
          "openapi": "3.0.0",
          "info": { "title": "Test", "version": "1.0" },
          "paths": {}
        }
        """;

    // ── YamlOpenApiParser ─────────────────────────────────────────────────

    [TestCase("/specs/openapi.yaml")]
    [TestCase("/specs/openapi.YAML")]
    [TestCase("/specs/openapi.yml")]
    [TestCase("/specs/openapi.YML")]
    public void YamlParser_CanParse_Returns_True_For_Yaml_Extensions(string filePath)
    {
        var parser = new YamlOpenApiParser();

        Assert.That(parser.CanParse(filePath, MinimalYaml), Is.True);
    }

    [TestCase("/specs/openapi.json")]
    [TestCase("/specs/openapi.toml")]
    [TestCase("/specs/openapi.xml")]
    [TestCase("/specs/openapi")]
    public void YamlParser_CanParse_Returns_False_For_Non_Yaml_Extensions(string filePath)
    {
        var parser = new YamlOpenApiParser();

        Assert.That(parser.CanParse(filePath, MinimalYaml), Is.False);
    }

    // ── JsonOpenApiParser ─────────────────────────────────────────────────

    [TestCase("/specs/openapi.json")]
    [TestCase("/specs/openapi.JSON")]
    public void JsonParser_CanParse_Returns_True_For_Json_Extension(string filePath)
    {
        var parser = new JsonOpenApiParser();

        Assert.That(parser.CanParse(filePath, MinimalJson), Is.True);
    }

    [TestCase("/specs/openapi.yaml")]
    [TestCase("/specs/openapi.yml")]
    [TestCase("/specs/openapi.toml")]
    [TestCase("/specs/openapi")]
    public void JsonParser_CanParse_Returns_False_For_Non_Json_Extensions(string filePath)
    {
        var parser = new JsonOpenApiParser();

        Assert.That(parser.CanParse(filePath, MinimalJson), Is.False);
    }

    // ── Generator-level selection ─────────────────────────────────────────

    [Test]
    public void Generator_Emits_MOA005_For_Unsupported_Extension()
    {
        const string yaml = MinimalYaml;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.toml", yaml)]);

        Assert.That(result.Diagnostics.Any(d => d.Id == "MOA005"), Is.True,
            "Expected MOA005 error for unsupported file extension");
    }

    [Test]
    public void Generator_Does_Not_Emit_MOA005_For_Yaml_Extension()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", MinimalYaml)]);

        Assert.That(result.Diagnostics.Any(d => d.Id == "MOA005"), Is.False,
            "Should not emit MOA005 for .yaml extension");
    }

    [Test]
    public void Generator_Does_Not_Emit_MOA005_For_Json_Extension()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.json", MinimalJson)]);

        Assert.That(result.Diagnostics.Any(d => d.Id == "MOA005"), Is.False,
            "Should not emit MOA005 for .json extension");
    }
}
