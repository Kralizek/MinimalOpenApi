namespace MinimalOpenAPI.Generator.Tests;

[TestFixture]
public class AllOfCompositionTests
{
    [Test]
    public void Component_AllOf_Ref_And_Inline_Flattens_Properties_And_Required()
    {
        const string spec = """
            openapi: "3.0.0"
            info: { title: test, version: "1.0.0" }
            paths: {}
            components:
              schemas:
                Base:
                  type: object
                  properties:
                    id: { type: string, format: uuid }
                    common: { type: string }
                  required: [id]
                Composed:
                  allOf:
                    - $ref: '#/components/schemas/Base'
                    - type: object
                      properties:
                        name: { type: string }
                      required: [name]
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", spec)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");
        Assert.That(source, Does.Contain("public sealed record Composed"));
        Assert.That(source, Does.Contain("public required global::System.Guid Id { get; init; }"));
        Assert.That(source, Does.Contain("public string? Common { get; init; }"));
        Assert.That(source, Does.Contain("public required string Name { get; init; }"));
    }

    [Test]
    public void Component_AllOf_TwoRefs_Flattens_Both()
    {
        const string spec = """
            openapi: "3.0.0"
            info: { title: test, version: "1.0.0" }
            paths: {}
            components:
              schemas:
                NamePart:
                  type: object
                  properties:
                    firstName: { type: string }
                  required: [firstName]
                AgePart:
                  type: object
                  properties:
                    age: { type: integer }
                  required: [age]
                Person:
                  allOf:
                    - $ref: '#/components/schemas/NamePart'
                    - $ref: '#/components/schemas/AgePart'
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", spec)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");
        Assert.That(source, Does.Contain("public required string FirstName { get; init; }"));
        Assert.That(source, Does.Contain("public required int Age { get; init; }"));
    }

    [Test]
    public void Inline_Property_AllOf_Generates_Nested_Record()
    {
        const string spec = """
            openapi: "3.0.0"
            info: { title: test, version: "1.0.0" }
            paths: {}
            components:
              schemas:
                Container:
                  type: object
                  properties:
                    payload:
                      allOf:
                        - type: object
                          properties:
                            code: { type: string }
                          required: [code]
                        - type: object
                          properties:
                            count: { type: integer }
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", spec)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");
        Assert.That(source, Does.Contain("public sealed record ContainerPayload"));
        Assert.That(source, Does.Contain("public required string Code { get; init; }"));
        Assert.That(source, Does.Contain("public int? Count { get; init; }"));
        Assert.That(source, Does.Contain("public ContainerPayload? Payload { get; init; }"));
    }

    [Test]
    public void Schema_Sibling_Properties_Are_Merged_With_AllOf()
    {
        const string spec = """
            openapi: "3.0.0"
            info: { title: test, version: "1.0.0" }
            paths: {}
            components:
              schemas:
                Base:
                  type: object
                  properties:
                    id: { type: integer }
                  required: [id]
                SiblingComposed:
                  allOf:
                    - $ref: '#/components/schemas/Base'
                  type: object
                  properties:
                    local: { type: string }
                  required: [local]
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", spec)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");
        Assert.That(source, Does.Contain("public required int Id { get; init; }"));
        Assert.That(source, Does.Contain("public required string Local { get; init; }"));
    }

    [Test]
    public void Nested_AllOf_Is_Recursively_Flattened()
    {
        const string spec = """
            openapi: "3.0.0"
            info: { title: test, version: "1.0.0" }
            paths: {}
            components:
              schemas:
                Base:
                  type: object
                  properties:
                    id: { type: string }
                  required: [id]
                Extra:
                  type: object
                  properties:
                    level: { type: integer }
                  required: [level]
                Deep:
                  allOf:
                    - $ref: '#/components/schemas/Base'
                    - type: object
                      allOf:
                        - $ref: '#/components/schemas/Extra'
                        - type: object
                          properties:
                            nested: { type: string }
                          required: [nested]
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", spec)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");
        Assert.That(source, Does.Contain("public required string Id { get; init; }"));
        Assert.That(source, Does.Contain("public required int Level { get; init; }"));
        Assert.That(source, Does.Contain("public required string Nested { get; init; }"));
    }

    [Test]
    public void Incompatible_AllOf_Property_Emits_Diagnostic_And_Uses_Object()
    {
        const string spec = """
            openapi: "3.0.0"
            info: { title: test, version: "1.0.0" }
            paths: {}
            components:
              schemas:
                Conflicted:
                  allOf:
                    - type: object
                      properties:
                        value: { type: string }
                      required: [value]
                    - type: object
                      properties:
                        value: { type: integer }
                      required: [value]
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", spec)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");
        Assert.That(result.Diagnostics.Any(d => d.Id == "MOA007"), Is.True);
        Assert.That(source, Does.Contain("public required object Value { get; init; }"));
    }

    [Test]
    public void Json_Parser_AllOf_Is_Also_Flattened()
    {
        const string spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "test", "version": "1.0.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "Base": {
                    "type": "object",
                    "properties": {
                      "id": { "type": "string" }
                    },
                    "required": ["id"]
                  },
                  "Composed": {
                    "allOf": [
                      { "$ref": "#/components/schemas/Base" },
                      {
                        "type": "object",
                        "properties": {
                          "name": { "type": "string" }
                        },
                        "required": ["name"]
                      }
                    ]
                  }
                }
              }
            }
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.json", spec)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");
        Assert.That(source, Does.Contain("public required string Id { get; init; }"));
        Assert.That(source, Does.Contain("public required string Name { get; init; }"));
    }
}
