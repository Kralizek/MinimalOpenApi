using Microsoft.CodeAnalysis;

namespace MinimalOpenAPI.Generator.Tests;

/// <summary>
/// Responses declared with <c>$ref: '#/components/responses/{name}'</c> must generate the same code
/// as the same responses written inline (#120); unresolvable response references report MOA019.
/// </summary>
[TestFixture]
public class ComponentResponseReferenceTests
{
    private const string ItemSchema = """
            Item:
              type: object
              required: [id]
              properties:
                id: { type: string, format: uuid }
        """;

    private const string ReferencedYaml = """
        openapi: 3.1.0
        info: { title: Repro, version: 1.0.0 }
        paths:
          /items/{id}:
            get:
              operationId: getItem
              parameters:
                - { name: id, in: path, required: true, schema: { type: string, format: uuid } }
              responses:
                "200":
                  $ref: '#/components/responses/ItemFound'
                "404":
                  $ref: '#/components/responses/NotFound'
                "409":
                  $ref: '#/components/responses/ConflictAlias'
        components:
          schemas:
        """ + "\n" + ItemSchema + """

          responses:
            ItemFound:
              description: The item.
              content:
                application/json:
                  schema:
                    $ref: '#/components/schemas/Item'
            NotFound:
              description: Not found.
              content:
                application/problem+json: {}
            ConflictAlias:
              $ref: '#/components/responses/Conflict'
            Conflict:
              description: Conflict.
              content:
                application/problem+json: {}
        """;

    private const string InlineYaml = """
        openapi: 3.1.0
        info: { title: Repro, version: 1.0.0 }
        paths:
          /items/{id}:
            get:
              operationId: getItem
              parameters:
                - { name: id, in: path, required: true, schema: { type: string, format: uuid } }
              responses:
                "200":
                  description: The item.
                  content:
                    application/json:
                      schema:
                        $ref: '#/components/schemas/Item'
                "404":
                  description: Not found.
                  content:
                    application/problem+json: {}
                "409":
                  description: Conflict.
                  content:
                    application/problem+json: {}
        components:
          schemas:
        """ + "\n" + ItemSchema;

    private const string ReferencedJson = """
        {
          "openapi": "3.1.0",
          "info": { "title": "Repro", "version": "1.0.0" },
          "paths": {
            "/items/{id}": {
              "get": {
                "operationId": "getItem",
                "parameters": [
                  { "name": "id", "in": "path", "required": true, "schema": { "type": "string", "format": "uuid" } }
                ],
                "responses": {
                  "200": { "$ref": "#/components/responses/ItemFound" },
                  "404": { "$ref": "#/components/responses/NotFound" },
                  "409": { "$ref": "#/components/responses/ConflictAlias" }
                }
              }
            }
          },
          "components": {
            "schemas": {
              "Item": {
                "type": "object",
                "required": ["id"],
                "properties": { "id": { "type": "string", "format": "uuid" } }
              }
            },
            "responses": {
              "ItemFound": {
                "description": "The item.",
                "content": { "application/json": { "schema": { "$ref": "#/components/schemas/Item" } } }
              },
              "NotFound": {
                "description": "Not found.",
                "content": { "application/problem+json": {} }
              },
              "ConflictAlias": { "$ref": "#/components/responses/Conflict" },
              "Conflict": {
                "description": "Conflict.",
                "content": { "application/problem+json": {} }
              }
            }
          }
        }
        """;

    private static readonly string[] GeneratedFiles = ["GetItemEndpointBase.g.cs", "EndpointMapping.g.cs"];

    [TestCase("openapi.yaml", ReferencedYaml, TestName = "Yaml_ReferencedResponses_GenerateSameCodeAsInline")]
    [TestCase("openapi.json", ReferencedJson, TestName = "Json_ReferencedResponses_GenerateSameCodeAsInline")]
    public void ReferencedResponses_GenerateSameCodeAsInline(string fileName, string referenced)
    {
        var (referencedResult, _) = GeneratorTestHelper.RunGenerator("", [(fileName, referenced)]);
        var (inlineResult, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", InlineYaml)]);

        Assert.That(referencedResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error), Is.Empty);
        foreach (var file in GeneratedFiles)
        {
            Assert.That(
                GeneratorTestHelper.GetGeneratedSource(referencedResult, file),
                Is.EqualTo(GeneratorTestHelper.GetGeneratedSource(inlineResult, file)),
                file);
        }

        var handler = GeneratorTestHelper.GetGeneratedSource(referencedResult, "GetItemEndpointBase.g.cs");
        Assert.That(handler, Does.Contain("Ok<global::TestProject.Openapi.Contracts.Item>"));
        Assert.That(handler, Does.Contain("NotFoundProblem"));
        Assert.That(handler, Does.Contain("ConflictProblem"));

        var mapping = GeneratorTestHelper.GetGeneratedSource(referencedResult, "EndpointMapping.g.cs");
        Assert.That(mapping, Does.Contain(".Produces<global::TestProject.Openapi.Contracts.Item>(global::Microsoft.AspNetCore.Http.StatusCodes.Status200OK)"));
        Assert.That(mapping, Does.Contain("(global::Microsoft.AspNetCore.Http.StatusCodes.Status404NotFound, \"application/problem+json\")"));
    }

    [Test]
    public void ReferencedResponse_WithUnresolvedSchemaReference_ReportsMOA017()
    {
        var yaml = ReferencedYaml.Replace("$ref: '#/components/schemas/Item'", "$ref: '#/components/schemas/Missing'");

        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", yaml)]);

        Assert.That(result.Diagnostics.Select(d => d.Id), Does.Contain("MOA017"));
    }

    [TestCase("#/components/responses/Missing", TestName = "Missing component response reports MOA019")]
    [TestCase("other.yaml#/components/responses/ItemFound", TestName = "External response reference reports MOA019")]
    [TestCase("#/components/schemas/Item", TestName = "Non-response local reference reports MOA019")]
    [TestCase("#/components/responses/Cycle", TestName = "Cyclic response reference reports MOA019")]
    public void UnresolvedResponseReference_ReportsMOA019(string reference)
    {
        var yaml = ReferencedYaml
            .Replace("$ref: '#/components/responses/NotFound'", $"$ref: '{reference}'")
            .Replace("""
                    ConflictAlias:
                """, """
                    Cycle:
                      $ref: '#/components/responses/CycleBack'
                    CycleBack:
                      $ref: '#/components/responses/Cycle'
                    ConflictAlias:
                """);

        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", yaml)]);

        var diagnostic = result.Diagnostics.Single();
        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Id, Is.EqualTo("MOA019"));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.GetMessage(), Does.Contain("404").And.Contain("getItem"));
            Assert.That(result.GeneratedTrees.Any(t => t.FilePath.EndsWith("GetItemEndpointBase.g.cs", StringComparison.Ordinal)), Is.False,
                "no code may be generated from an unresolved response reference");
        });
    }

    [Test]
    public void Json_UnresolvedResponseReference_ReportsMOA019()
    {
        var json = ReferencedJson.Replace("\"#/components/responses/NotFound\"", "\"#/components/responses/Missing\"");

        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.json", json)]);

        Assert.That(result.Diagnostics.Select(d => d.Id), Is.EqualTo(new[] { "MOA019" }));
    }
}