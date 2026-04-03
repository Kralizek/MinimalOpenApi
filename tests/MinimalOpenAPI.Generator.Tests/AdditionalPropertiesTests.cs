namespace MinimalOpenAPI.Generator.Tests;

/// <summary>
/// Tests verifying that OpenAPI <c>additionalProperties</c> schemas are mapped to
/// <c>Dictionary&lt;string, T&gt;</c> on generated DTO record properties.
/// </summary>
[TestFixture]
public class AdditionalPropertiesTests
{
    private const string NoOpHandlerImpl = "// no handler needed";

    // ── YAML ──────────────────────────────────────────────────────────────

    [TestFixture]
    public class YamlParser
    {
        private static readonly (string, string)[] AdditionalFiles =
        [
            ("openapi.yaml", OpenApiFixtures.GetResourceWithAdditionalPropertiesYaml)
        ];

        [Test]
        public void RequiredDictionaryPropertyMapsToCorrectType()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: NoOpHandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

            Assert.That(source, Does.Contain(
                "public required global::System.Collections.Generic.Dictionary<string, string> Labels { get; init; }"));
        }

        [Test]
        public void OptionalDictionaryPropertyIsMappedAsNullable()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: NoOpHandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

            Assert.That(source, Does.Contain(
                "public global::System.Collections.Generic.Dictionary<string, int>? Metadata { get; init; }"));
        }

        [Test]
        public void DictionaryPropertyHasJsonPropertyNameAttribute()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: NoOpHandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

            Assert.That(source, Does.Contain("[JsonPropertyName(\"labels\")]"));
            Assert.That(source, Does.Contain("[JsonPropertyName(\"metadata\")]"));
        }

        [Test]
        public void NoDictionaryRecordIsGeneratedForAdditionalPropertiesSchema()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: NoOpHandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

            // No inline record named after the property should be generated.
            Assert.That(source, Does.Not.Contain("public sealed record ResourceLabels"));
            Assert.That(source, Does.Not.Contain("public sealed record ResourceMetadata"));
        }

        [Test]
        public void ResourceRecordIsGenerated()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: NoOpHandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

            Assert.That(source, Does.Contain("public sealed record Resource"));
        }
    }

    // ── JSON ──────────────────────────────────────────────────────────────

    [TestFixture]
    public class JsonParser
    {
        private static readonly (string, string)[] AdditionalFiles =
        [
            ("openapi.json", OpenApiFixtures.GetResourceWithAdditionalPropertiesJson)
        ];

        [Test]
        public void RequiredDictionaryPropertyMapsToCorrectType()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: NoOpHandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

            Assert.That(source, Does.Contain(
                "public required global::System.Collections.Generic.Dictionary<string, string> Labels { get; init; }"));
        }

        [Test]
        public void OptionalDictionaryPropertyIsMappedAsNullable()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: NoOpHandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

            Assert.That(source, Does.Contain(
                "public global::System.Collections.Generic.Dictionary<string, int>? Metadata { get; init; }"));
        }

        [Test]
        public void JsonParserProducesSameOutputAsYamlParser()
        {
            var (yamlResult, _) = GeneratorTestHelper.RunGenerator(
                userSource: NoOpHandlerImpl,
                additionalFiles: [("openapi.yaml", OpenApiFixtures.GetResourceWithAdditionalPropertiesYaml)]);

            var (jsonResult, _) = GeneratorTestHelper.RunGenerator(
                userSource: NoOpHandlerImpl,
                additionalFiles: [("openapi.json", OpenApiFixtures.GetResourceWithAdditionalPropertiesJson)]);

            var yamlSource = GeneratorTestHelper.GetGeneratedSource(yamlResult, "Dtos.g.cs");
            var jsonSource = GeneratorTestHelper.GetGeneratedSource(jsonResult, "Dtos.g.cs");

            Assert.That(jsonSource, Is.EqualTo(yamlSource));
        }
    }

    // ── Boolean additionalProperties ───────────────────────────────────────

    [TestFixture]
    public class BooleanAdditionalProperties
    {
        private const string WithPropertiesYaml = """
            openapi: "3.0.0"
            info:
              title: Test API
              version: "1.0.0"
            paths:
              /items:
                get:
                  operationId: getItems
                  responses:
                    "200":
                      description: OK
                      content:
                        application/json:
                          schema:
                            $ref: '#/components/schemas/Item'
            components:
              schemas:
                Item:
                  type: object
                  required:
                    - name
                  properties:
                    name:
                      type: string
                  additionalProperties: true
            """;

        private const string PureMapYaml = """
            openapi: "3.0.0"
            info:
              title: Test API
              version: "1.0.0"
            paths:
              /items:
                post:
                  operationId: createItem
                  requestBody:
                    required: true
                    content:
                      application/json:
                        schema:
                          $ref: '#/components/schemas/FreeMap'
                  responses:
                    "204":
                      description: No content
            components:
              schemas:
                FreeMap:
                  type: object
                  additionalProperties: true
            """;

        [Test]
        public void RecordWithPropertiesAndBooleanAdditionalPropertiesEmitsExtensionData()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: NoOpHandlerImpl,
                additionalFiles: [("openapi.yaml", WithPropertiesYaml)]);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

            // Named properties are still emitted.
            Assert.That(source, Does.Contain("public sealed record Item"));
            Assert.That(source, Does.Contain("public required string Name { get; init; }"));
            // Extension-data property captures free-form extra keys.
            Assert.That(source, Does.Contain("[JsonExtensionData]"));
            Assert.That(source, Does.Contain(
                "public global::System.Collections.Generic.Dictionary<string, global::System.Text.Json.JsonElement>? Extensions { get; init; }"));
        }

        [Test]
        public void PureBooleanAdditionalPropertiesSchemaDoesNotGenerateRecord()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: NoOpHandlerImpl,
                additionalFiles: [("openapi.yaml", PureMapYaml)]);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

            // No named record should be generated for a schema that only has additionalProperties: true.
            Assert.That(source, Does.Not.Contain("public sealed record FreeMap"));
        }
    }
}