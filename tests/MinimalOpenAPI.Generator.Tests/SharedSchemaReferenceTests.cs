using MinimalOpenAPI.Parser.Json;
using MinimalOpenAPI.Parser.Yaml;

namespace MinimalOpenAPI.Generator.Tests;

[TestFixture]
public sealed class SharedSchemaReferenceTests
{
    private const string LocalType = "global::TestProject.Openapi.Contracts.Item";

    [Test]
    public async Task Json_external_schema_reference_does_not_bind_to_same_named_local_server_dto()
    {
        const string reference = "other.yaml#/components/schemas/Item";
        var content = JsonTemplate.Replace("REFERENCE", reference);
        var document = await new JsonOpenApiParser().ParseAsync(content);
        Assert.That(document.Schemas.ContainsKey("Item"), Is.True);
        Assert.That(document.Operations.Single().Responses.Single().Schema!.Reference, Is.EqualTo(reference));

        var handlerSource = GenerateHandlerWithLocalItem(content, "openapi.json");
        Assert.That(handlerSource, Does.Not.Contain(LocalType));
        Assert.That(handlerSource, Does.Contain(reference));
    }

    [Test]
    public async Task Json_local_schema_reference_binds_to_local_server_dto()
    {
        var content = JsonTemplate.Replace("REFERENCE", "#/components/schemas/Item");
        var document = await new JsonOpenApiParser().ParseAsync(content);
        Assert.That(document.Schemas.ContainsKey("Item"), Is.True);
        Assert.That(document.Operations.Single().Responses.Single().Schema!.Reference, Is.EqualTo("Item"));

        var handlerSource = GenerateHandlerWithLocalItem(content, "openapi.json");
        Assert.That(handlerSource, Does.Contain(LocalType));
    }

    [Test]
    public async Task Yaml_external_schema_reference_does_not_bind_to_same_named_local_server_dto()
    {
        const string reference = "other.yaml#/components/schemas/Item";
        var content = YamlTemplate.Replace("REFERENCE", reference);
        var document = await new YamlOpenApiParser().ParseAsync(content);
        Assert.That(document.Schemas.ContainsKey("Item"), Is.True);
        Assert.That(document.Operations.Single().Responses.Single().Schema!.Reference, Is.EqualTo(reference));

        var handlerSource = GenerateHandlerWithLocalItem(content, "openapi.yaml");
        Assert.That(handlerSource, Does.Not.Contain(LocalType));
        Assert.That(handlerSource, Does.Contain(reference));
    }

    [Test]
    public async Task Yaml_local_schema_reference_binds_to_local_server_dto()
    {
        var content = YamlTemplate.Replace("REFERENCE", "#/components/schemas/Item");
        var document = await new YamlOpenApiParser().ParseAsync(content);
        Assert.That(document.Schemas.ContainsKey("Item"), Is.True);
        Assert.That(document.Operations.Single().Responses.Single().Schema!.Reference, Is.EqualTo("Item"));

        var handlerSource = GenerateHandlerWithLocalItem(content, "openapi.yaml");
        Assert.That(handlerSource, Does.Contain(LocalType));
    }

    private static string GenerateHandlerWithLocalItem(string content, string path)
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(string.Empty, [(path, content)]);
        Assert.That(result.Results.Single().Exception, Is.Null);
        var dtoSource = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");
        Assert.That(dtoSource, Does.Contain("public sealed record Item"));
        Assert.That(dtoSource, Does.Contain("LocalOnly"));
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