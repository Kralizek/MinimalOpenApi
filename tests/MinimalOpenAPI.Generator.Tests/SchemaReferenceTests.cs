using MinimalOpenAPI.Parser.Json;
using MinimalOpenAPI.Parser.Yaml;

namespace MinimalOpenAPI.Generator.Tests;

[TestFixture]
public sealed class SchemaReferenceTests
{
    private const string LocalType = "global::TestProject.Openapi.Contracts.Item";

    [Test]
    public async Task Json_external_schema_reference_is_preserved_and_reported()
    {
        const string reference = "other.yaml#/components/schemas/Item";
        var content = JsonTemplate.Replace("REFERENCE", reference);
        var document = await new JsonOpenApiParser().ParseAsync(content);

        Assert.That(document.Operations.Single().Responses.Single().Schema!.Reference, Is.EqualTo(reference));
        AssertReferenceDiagnostic(content, "openapi.json", reference);
    }

    [Test]
    public async Task Yaml_external_schema_reference_is_preserved_and_reported()
    {
        const string reference = "other.yaml#/components/schemas/Item";
        var content = YamlTemplate.Replace("REFERENCE", reference);
        var document = await new YamlOpenApiParser().ParseAsync(content);

        Assert.That(document.Operations.Single().Responses.Single().Schema!.Reference, Is.EqualTo(reference));
        AssertReferenceDiagnostic(content, "openapi.yaml", reference);
    }

    [TestCase("https://example.test/common.yaml#/components/schemas/Item")]
    [TestCase("file:///tmp/common.yaml#/components/schemas/Item")]
    [TestCase("#/components/parameters/Item")]
    public void Unsupported_schema_reference_forms_report_MOA015(string reference)
        => AssertReferenceDiagnostic(JsonTemplate.Replace("REFERENCE", reference), "openapi.json", reference);

    [Test]
    public void Component_parameter_schema_reference_is_validated()
    {
        const string reference = "other.yaml#/components/schemas/Filter";
        const string content = """
            {
              "openapi": "3.0.3",
              "paths": {"/items": {"get": {
                "parameters": [{"$ref": "#/components/parameters/Filter"}],
                "responses": {"204": {"description": "OK"}}
              }}},
              "components": {"parameters": {"Filter": {
                "name": "filter",
                "in": "query",
                "schema": {"$ref": "other.yaml#/components/schemas/Filter"}
              }}}
            }
            """;

        AssertReferenceDiagnostic(content, "openapi.json", reference);
    }

    [Test]
    public void Missing_local_component_schema_reports_MOA015()
        => AssertReferenceDiagnostic(
            JsonTemplate.Replace("REFERENCE", "#/components/schemas/Missing"),
            "openapi.json",
            "Missing");

    [Test]
    public async Task Json_local_schema_reference_resolves_to_component_name()
    {
        var content = JsonTemplate.Replace("REFERENCE", "#/components/schemas/Item");
        var document = await new JsonOpenApiParser().ParseAsync(content);

        Assert.That(document.Operations.Single().Responses.Single().Schema!.Reference, Is.EqualTo("Item"));
        Assert.That(GenerateHandler(content, "openapi.json"), Does.Contain(LocalType));
    }

    [Test]
    public async Task Yaml_local_schema_reference_resolves_to_component_name()
    {
        var content = YamlTemplate.Replace("REFERENCE", "#/components/schemas/Item");
        var document = await new YamlOpenApiParser().ParseAsync(content);

        Assert.That(document.Operations.Single().Responses.Single().Schema!.Reference, Is.EqualTo("Item"));
        Assert.That(GenerateHandler(content, "openapi.yaml"), Does.Contain(LocalType));
    }

    [Test]
    public async Task Json_local_schema_reference_decodes_json_pointer_tokens()
    {
        const string content = """
            {
              "openapi": "3.0.3",
              "paths": {"/items": {"get": {"responses": {"200": {
                "description": "OK",
                "content": {"application/json": {"schema": {"$ref": "#/components/schemas/Foo~1Bar~0Baz"}}}
              }}}}},
              "components": {"schemas": {"Foo/Bar~Baz": {"type": "string"}}}
            }
            """;

        var document = await new JsonOpenApiParser().ParseAsync(content);
        Assert.That(document.Operations.Single().Responses.Single().Schema!.Reference, Is.EqualTo("Foo/Bar~Baz"));

        var (result, _) = GeneratorTestHelper.RunGenerator(string.Empty, [("openapi.json", content)]);
        Assert.That(result.Diagnostics.Any(diagnostic => diagnostic.Id == "MOA015"), Is.False);
    }

    [Test]
    public async Task Yaml_local_schema_reference_decodes_json_pointer_tokens()
    {
        const string content = """
            openapi: 3.0.3
            paths:
              /items:
                get:
                  responses:
                    '200':
                      description: OK
                      content:
                        application/json:
                          schema:
                            $ref: '#/components/schemas/Foo~1Bar~0Baz'
            components:
              schemas:
                'Foo/Bar~Baz':
                  type: string
            """;

        var document = await new YamlOpenApiParser().ParseAsync(content);
        Assert.That(document.Operations.Single().Responses.Single().Schema!.Reference, Is.EqualTo("Foo/Bar~Baz"));

        var (result, _) = GeneratorTestHelper.RunGenerator(string.Empty, [("openapi.yaml", content)]);
        Assert.That(result.Diagnostics.Any(diagnostic => diagnostic.Id == "MOA015"), Is.False);
    }

    private static void AssertReferenceDiagnostic(string content, string path, string reference)
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(string.Empty, [(path, content)]);
        var diagnostics = result.Diagnostics.Where(diagnostic => diagnostic.Id == "MOA015").ToArray();

        Assert.That(diagnostics, Has.Length.EqualTo(1));
        Assert.That(diagnostics[0].GetMessage(), Does.Contain(reference));
        Assert.That(result.Results.Single().GeneratedSources, Is.Empty);
    }

    private static string GenerateHandler(string content, string path)
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(string.Empty, [(path, content)]);
        Assert.That(result.Results.Single().Exception, Is.Null);
        var dtoSource = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");
        Assert.That(dtoSource, Does.Contain("public sealed record Item"));
        return GeneratorTestHelper.GetGeneratedSource(result, "GetItemEndpointBase.g.cs");
    }

    private const string JsonTemplate = """
        {
          "openapi": "3.0.3",
          "info": {"title": "Reference regression", "version": "1.0.0"},
          "paths": {"/items": {"get": {
            "operationId": "getItem",
            "responses": {"200": {
              "description": "OK",
              "content": {"application/json": {"schema": {"$ref": "REFERENCE"}}}
            }}
          }}},
          "components": {"schemas": {"Item": {
            "type": "object",
            "properties": {"localOnly": {"type": "string"}}
          }}}
        }
        """;

    private const string YamlTemplate = """
        openapi: 3.0.3
        info:
          title: Reference regression
          version: 1.0.0
        paths:
          /items:
            get:
              operationId: getItem
              responses:
                '200':
                  description: OK
                  content:
                    application/json:
                      schema:
                        $ref: 'REFERENCE'
        components:
          schemas:
            Item:
              type: object
              properties:
                localOnly:
                  type: string
        """;
}