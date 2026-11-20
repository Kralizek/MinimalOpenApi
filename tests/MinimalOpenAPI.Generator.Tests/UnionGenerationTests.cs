namespace MinimalOpenAPI.Generator.Tests;

[TestFixture]
public sealed class UnionGenerationTests
{
    [Test]
    public void Json_primitive_oneOf_emits_a_native_union()
    {
        const string spec = """
            {
              "openapi": "3.1.0",
              "components": {
                "schemas": {
                  "Primitive": {
                    "oneOf": [
                      {"type": "integer"},
                      {"type": "boolean"}
                    ]
                  }
                }
              }
            }
            """;

        var (result, _) = RunGenerator(spec, "openapi.json");
        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("public union Primitive(int, bool);"));
        Assert.That(source, Does.Contain("[global::System.Text.Json.Serialization.JsonUnion]"));
        Assert.That(source, Does.Not.Contain("JsonUnionTypeStructuralClassifier"));
        Assert.That(result.Diagnostics.Any(diagnostic => diagnostic.Id == "MOA019"), Is.False);
        Assert.That(result.GeneratedTrees.Any(source => source.FilePath.EndsWith("UnionPolyfills.g.cs", StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void Yaml_object_anyOf_emits_a_structural_classifier()
    {
        const string spec = """
            openapi: 3.1.0
            components:
              schemas:
                Cat:
                  type: object
                  required: [lives]
                  properties:
                    lives: { type: integer }
                Dog:
                  type: object
                  required: [breed]
                  properties:
                    breed: { type: string }
                Pet:
                  anyOf:
                    - $ref: '#/components/schemas/Cat'
                    - $ref: '#/components/schemas/Dog'
            """;

        var (result, _) = RunGenerator(spec, "openapi.yaml");
        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("[global::System.Text.Json.Serialization.JsonUnion(TypeClassifier = typeof(global::System.Text.Json.Serialization.JsonUnionTypeStructuralClassifier))]"));
        Assert.That(source, Does.Contain("public union Pet(Cat, Dog);"));
        Assert.That(result.Diagnostics.Any(diagnostic => diagnostic.Id == "MOA019"), Is.False);
    }

    [Test]
    public void Json_discriminator_emits_authored_values_and_target_types()
    {
        const string spec = """
            {
              "openapi": "3.1.0",
              "components": {
                "schemas": {
                  "Cat": {
                    "type": "object",
                    "properties": {"kind": {"type": "string"}, "lives": {"type": "integer"}}
                  },
                  "Dog": {
                    "type": "object",
                    "properties": {"kind": {"type": "string"}, "breed": {"type": "string"}}
                  },
                  "Pet": {
                    "oneOf": [
                      {"$ref": "#/components/schemas/Cat"},
                      {"$ref": "#/components/schemas/Dog"}
                    ],
                    "discriminator": {
                      "propertyName": "kind",
                      "mapping": {
                        "feline": "#/components/schemas/Cat",
                        "canine": "#/components/schemas/Dog"
                      }
                    }
                  }
                }
              }
            }
            """;

        var (result, _) = RunGenerator(spec, "openapi.json");
        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("public union Pet(Cat, Dog);"));
        Assert.That(source, Does.Contain("\"feline\" => typeof(Cat)"));
        Assert.That(source, Does.Contain("\"canine\" => typeof(Dog)"));
        Assert.That(source, Does.Contain("ValueTextEquals(\"kind\"u8)"));
        Assert.That(result.Diagnostics.Any(diagnostic => diagnostic.Id == "MOA019"), Is.False);
    }

    [Test]
    public void Inline_union_inside_array_property_generates_a_named_type()
    {
        const string spec = """
            openapi: "3.1.0"
            components:
              schemas:
                Container:
                  type: object
                  properties:
                    value:
                      type: array
                      items:
                        oneOf:
                          - type: boolean
                          - type: integer
            """;

        var (result, _) = RunGenerator(spec, "openapi.yaml");

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(source, Does.Contain("public union ContainerValueItemUnion(bool, int);"));
        Assert.That(source, Does.Contain("ContainerValueItemUnion[]? Value"));
    }

    [Test]
    public void Parameter_reference_to_union_reports_binding_diagnostic()
    {
        const string spec = """
            openapi: 3.1.0
            paths:
              /search:
                get:
                  operationId: search
                  parameters:
                    - $ref: '#/components/parameters/Term'
                  responses:
                    '204': { description: OK }
            components:
              parameters:
                Term:
                  name: term
                  in: query
                  schema:
                    $ref: '#/components/schemas/Value'
              schemas:
                Value:
                  oneOf:
                    - type: boolean
                    - type: integer
            """;

        var (result, _) = RunGenerator(spec, "openapi.yaml");
        var message = result.Diagnostics.Single(diagnostic => diagnostic.Id == "MOA019").GetMessage();
        Assert.That(message, Does.Contain("operations/search/parameters/term").And.Contain("only supported in JSON payloads"));
    }

    [TestCase("openapi.yaml")]
    [TestCase("openapi.json")]
    public void Union_alongside_reference_is_not_discarded(string fileName)
    {
        const string spec = """
            {
              "openapi": "3.1.0",
              "components": {
                "schemas": {
                  "Target": {"type": "object"},
                  "Value": {
                    "$ref": "#/components/schemas/Target",
                    "oneOf": [{"type": "integer"}, {"type": "boolean"}]
                  }
                }
              }
            }
            """;

        var (result, _) = RunGenerator(spec, fileName);
        var message = result.Diagnostics.Single(diagnostic => diagnostic.Id == "MOA019").GetMessage();
        Assert.That(message, Does.Contain("$ref").And.Contain("components/schemas/Value"));
    }

    [TestCase("Ignore", "EnvelopeValueUnion", "EnvelopeValueUnion")]
    [TestCase("Auto", "EnvelopeRequestValueUnion", "EnvelopeResponseValueUnion")]
    [TestCase("Split", "EnvelopeRequestValueUnion", "EnvelopeResponseValueUnion")]
    public void Inline_union_uses_directional_case_records(string mode, string requestType, string responseType)
    {
        const string spec = """
            openapi: 3.1.0
            paths:
              /echo:
                post:
                  operationId: echo
                  requestBody:
                    content:
                      application/json:
                        schema: { $ref: '#/components/schemas/Envelope' }
                  responses:
                    '200':
                      description: OK
                      content:
                        application/json:
                          schema: { $ref: '#/components/schemas/Envelope' }
            components:
              schemas:
                Envelope:
                  type: object
                  properties:
                    value:
                      oneOf:
                        - type: object
                          required: [name]
                          properties:
                            name: { type: string }
                            id: { type: integer, readOnly: true }
                        - type: object
                          required: [code]
                          properties:
                            code: { type: integer }
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", spec)], readWriteSchemaHandling: mode);
        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");
        Assert.That(result.Diagnostics.Any(diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error), Is.False);
        foreach (var typeName in new[] { requestType, responseType }.Distinct())
            Assert.That(source, Does.Contain($"public union {typeName}({typeName}Case1, {typeName}Case2);"));

        var requestCase = source.Split($"public sealed record {requestType}Case1", StringSplitOptions.None)[1].Split('}')[0..2];
        var responseCase = source.Split($"public sealed record {responseType}Case1", StringSplitOptions.None)[1].Split('}')[0..2];
        Assert.That(requestCase[1].Contains("Id", StringComparison.Ordinal), Is.EqualTo(mode == "Ignore"));
        Assert.That(responseCase[1], Does.Contain("Id"));
    }

    [Test]
    public void Ambiguous_object_alternatives_report_MOA019()
    {
        const string spec = """
            openapi: "3.1.0"
            components:
              schemas:
                Cat:
                  type: object
                  properties:
                    name: { type: string }
                Dog:
                  type: object
                  properties:
                    name: { type: string }
                Pet:
                  oneOf:
                    - $ref: '#/components/schemas/Cat'
                    - $ref: '#/components/schemas/Dog'
            """;

        var (result, _) = RunGenerator(spec, "openapi.yaml");

        Assert.That(result.Diagnostics.Single(diagnostic => diagnostic.Id == "MOA019").GetMessage(),
            Does.Contain("distinct required properties or an authored discriminator mapping"));
        Assert.That(result.GeneratedTrees.Any(source => source.FilePath.EndsWith("Dtos.g.cs", StringComparison.Ordinal)), Is.False);
    }

    [Test]
    public void String_and_number_alternatives_report_MOA019()
    {
        const string spec = """
            {
              "openapi": "3.1.0",
              "components": {
                "schemas": {
                  "Value": {
                    "anyOf": [
                      {"type": "string"},
                      {"type": "integer"}
                    ]
                  }
                }
              }
            }
            """;

        var (result, _) = RunGenerator(spec, "openapi.json");

        Assert.That(result.Diagnostics.Single(diagnostic => diagnostic.Id == "MOA019").GetMessage(),
            Does.Contain("ambiguous under ASP.NET Core JSON web defaults"));
        Assert.That(result.GeneratedTrees.Any(source => source.FilePath.EndsWith("Dtos.g.cs", StringComparison.Ordinal)), Is.False);
    }

    [Test]
    public void Nullable_union_schema_reports_MOA019()
    {
        const string spec = """
            openapi: "3.1.0"
            components:
              schemas:
                NullableValue:
                  nullable: true
                  oneOf:
                    - type: integer
                    - type: boolean
            """;

        var (result, _) = RunGenerator(spec, "openapi.yaml");

        Assert.That(result.Diagnostics.Single(diagnostic => diagnostic.Id == "MOA019").GetMessage(),
            Does.Contain("nullable union schemas are not supported"));
    }

    [Test]
    public void Scalar_union_with_discriminator_reports_MOA019()
    {
        const string spec = """
            openapi: "3.1.0"
            components:
              schemas:
                Value:
                  anyOf:
                    - type: integer
                    - type: boolean
                  discriminator:
                    propertyName: kind
            """;

        var (result, _) = RunGenerator(spec, "openapi.yaml");

        Assert.That(result.Diagnostics.Single(diagnostic => diagnostic.Id == "MOA019").GetMessage(),
            Does.Contain("discriminators are only supported for object alternatives"));
    }

    private static (Microsoft.CodeAnalysis.GeneratorDriverRunResult Result, Microsoft.CodeAnalysis.Compilation OutputCompilation)
        RunGenerator(string spec, string fileName)
        => GeneratorTestHelper.RunGenerator("// no handler", [(fileName, spec)]);
}