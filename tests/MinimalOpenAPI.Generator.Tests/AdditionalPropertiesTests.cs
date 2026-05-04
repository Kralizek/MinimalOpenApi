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

        [Test]
        public void InlineFreeFormRequestBodyMapsToJsonElementDictionary()
        {
            const string InlineFreeMapYaml = """
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
                              type: object
                              additionalProperties: true
                      responses:
                        "204":
                          description: No content
                """;

            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: NoOpHandlerImpl,
                additionalFiles: [("openapi.yaml", InlineFreeMapYaml)]);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "CreateItemEndpointBase.g.cs");

            // Free-form maps (additionalProperties: true) default to Dictionary<string, JsonElement>
            // because payloads are assumed to be JSON.
            Assert.That(source, Does.Contain(
                "global::System.Collections.Generic.Dictionary<string, global::System.Text.Json.JsonElement>"));
        }
    }

    // ── Inline complex value type ──────────────────────────────────────────

    [TestFixture]
    public class InlineComplexValueType
    {
        // ── Component schema (Dtos.g.cs) ──────────────────────────────────

        [TestFixture]
        public class YamlParser
        {
            private static readonly (string, string)[] AdditionalFiles =
            [
                ("openapi.yaml", OpenApiFixtures.GetResourceWithInlineComplexAdditionalPropertiesYaml)
            ];

            [Test]
            public void ValueRecordIsGenerated()
            {
                var (result, _) = GeneratorTestHelper.RunGenerator(
                    userSource: NoOpHandlerImpl,
                    additionalFiles: AdditionalFiles);

                var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

                Assert.That(source, Does.Contain("public sealed record ResourceTagsValue"));
            }

            [Test]
            public void ValueRecordHasCorrectProperties()
            {
                var (result, _) = GeneratorTestHelper.RunGenerator(
                    userSource: NoOpHandlerImpl,
                    additionalFiles: AdditionalFiles);

                var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

                Assert.That(source, Does.Contain("public string? Label { get; init; }"));
                Assert.That(source, Does.Contain("public int? Weight { get; init; }"));
            }

            [Test]
            public void TagsPropertyMapsToTypedDictionary()
            {
                var (result, _) = GeneratorTestHelper.RunGenerator(
                    userSource: NoOpHandlerImpl,
                    additionalFiles: AdditionalFiles);

                var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

                Assert.That(source, Does.Contain(
                    "global::System.Collections.Generic.Dictionary<string, ResourceTagsValue>"));
            }

            [Test]
            public void ValueRecordIsEmittedBeforeParentRecord()
            {
                var (result, _) = GeneratorTestHelper.RunGenerator(
                    userSource: NoOpHandlerImpl,
                    additionalFiles: AdditionalFiles);

                var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

                var valuePos = source.IndexOf("public sealed record ResourceTagsValue", StringComparison.Ordinal);
                var parentPos = source.IndexOf($"public sealed record Resource{System.Environment.NewLine}", StringComparison.Ordinal);

                Assert.That(valuePos, Is.GreaterThan(-1));
                Assert.That(parentPos, Is.GreaterThan(-1));
                Assert.That(valuePos, Is.LessThan(parentPos));
            }
        }

        [TestFixture]
        public class JsonParser
        {
            [Test]
            public void JsonParserProducesSameOutputAsYamlParser()
            {
                var (yamlResult, _) = GeneratorTestHelper.RunGenerator(
                    userSource: NoOpHandlerImpl,
                    additionalFiles: [("openapi.yaml", OpenApiFixtures.GetResourceWithInlineComplexAdditionalPropertiesYaml)]);

                var (jsonResult, _) = GeneratorTestHelper.RunGenerator(
                    userSource: NoOpHandlerImpl,
                    additionalFiles: [("openapi.json", OpenApiFixtures.GetResourceWithInlineComplexAdditionalPropertiesJson)]);

                var yamlSource = GeneratorTestHelper.GetGeneratedSource(yamlResult, "Dtos.g.cs");
                var jsonSource = GeneratorTestHelper.GetGeneratedSource(jsonResult, "Dtos.g.cs");

                Assert.That(jsonSource, Is.EqualTo(yamlSource));
            }
        }

        // ── Inline request body schema (Handler base) ──────────────────────

        [TestFixture]
        public class InlineRequestBody
        {
            private const string InlineComplexValueYaml = """
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
                              type: object
                              properties:
                                name:
                                  type: string
                                tags:
                                  type: object
                                  additionalProperties:
                                    type: object
                                    properties:
                                      label:
                                        type: string
                                      weight:
                                        type: integer
                      responses:
                        "201":
                          description: Created
                """;

            [Test]
            public void ValueRecordIsEmittedAsNestedType()
            {
                var (result, _) = GeneratorTestHelper.RunGenerator(
                    userSource: NoOpHandlerImpl,
                    additionalFiles: [("openapi.yaml", InlineComplexValueYaml)]);

                var source = GeneratorTestHelper.GetGeneratedSource(result, "CreateItemEndpointBase.g.cs");

                Assert.That(source, Does.Contain("public sealed record RequestTagsValue"));
            }

            [Test]
            public void RequestTagsPropertyMapsToTypedDictionary()
            {
                var (result, _) = GeneratorTestHelper.RunGenerator(
                    userSource: NoOpHandlerImpl,
                    additionalFiles: [("openapi.yaml", InlineComplexValueYaml)]);

                var source = GeneratorTestHelper.GetGeneratedSource(result, "CreateItemEndpointBase.g.cs");

                Assert.That(source, Does.Contain(
                    "global::System.Collections.Generic.Dictionary<string, RequestTagsValue>"));
            }

            [Test]
            public void ValueRecordIsEmittedBeforeRequestRecord()
            {
                var (result, _) = GeneratorTestHelper.RunGenerator(
                    userSource: NoOpHandlerImpl,
                    additionalFiles: [("openapi.yaml", InlineComplexValueYaml)]);

                var source = GeneratorTestHelper.GetGeneratedSource(result, "CreateItemEndpointBase.g.cs");

                var valuePos = source.IndexOf("public sealed record RequestTagsValue", StringComparison.Ordinal);
                var requestPos = source.IndexOf($"public sealed record Request{System.Environment.NewLine}", StringComparison.Ordinal);

                Assert.That(valuePos, Is.GreaterThan(-1));
                Assert.That(requestPos, Is.GreaterThan(-1));
                Assert.That(valuePos, Is.LessThan(requestPos));
            }
        }
    }
}