using MinimalOpenAPI.Abstractions.Models;
using MinimalOpenAPI.Parser.Json;
using MinimalOpenAPI.Parser.Yaml;

namespace MinimalOpenAPI.Generator.Tests;

/// <summary>Tests for OpenAPI version detection and 3.1 schema normalisation.</summary>
[TestFixture]
public class VersionDetectionTests
{
    // ── YAML parser ───────────────────────────────────────────────────────

    [Test]
    public async Task YamlParser_Detects_V3_0()
    {
        const string yaml = """
            openapi: "3.0.0"
            info:
              title: Test
              version: "1.0"
            paths: {}
            """;

        var doc = await new YamlOpenApiParser().ParseAsync(yaml);

        Assert.That(doc.OpenApiVersion, Is.EqualTo(new Version(3, 0, 0)));
    }

    [Test]
    public async Task YamlParser_Detects_V3_1()
    {
        const string yaml = """
            openapi: "3.1.0"
            info:
              title: Test
              version: "1.0"
            paths: {}
            """;

        var doc = await new YamlOpenApiParser().ParseAsync(yaml);

        Assert.That(doc.OpenApiVersion, Is.EqualTo(new Version(3, 1, 0)));
    }

    [Test]
    public async Task YamlParser_Returns_Null_When_Version_Absent()
    {
        const string yaml = """
            info:
              title: Test
              version: "1.0"
            paths: {}
            """;

        var doc = await new YamlOpenApiParser().ParseAsync(yaml);

        Assert.That(doc.OpenApiVersion, Is.Null);
    }

    [Test]
    public async Task YamlParser_Returns_Parsed_Version_For_Unrecognised_Version()
    {
        const string yaml = """
            openapi: "4.0.0"
            info:
              title: Test
              version: "1.0"
            paths: {}
            """;

        var doc = await new YamlOpenApiParser().ParseAsync(yaml);

        // Version is parsed and stored even when not explicitly supported
        Assert.That(doc.OpenApiVersion, Is.EqualTo(new Version(4, 0, 0)));
    }

    [Test]
    public async Task YamlParser_Normalises_3_1_Type_Array_To_Nullable()
    {
        const string yaml = """
            openapi: "3.1.0"
            info:
              title: Test
              version: "1.0"
            paths: {}
            components:
              schemas:
                Item:
                  type: object
                  properties:
                    name:
                      type: string
                    nickname:
                      type:
                        - string
                        - "null"
            """;

        var doc = await new YamlOpenApiParser().ParseAsync(yaml);

        var item = doc.Schemas["Item"];
        Assert.That(item.Properties["name"].Type, Is.EqualTo("string"));
        Assert.That(item.Properties["name"].Nullable, Is.False);
        Assert.That(item.Properties["nickname"].Type, Is.EqualTo("string"));
        Assert.That(item.Properties["nickname"].Nullable, Is.True);
    }

    [Test]
    public async Task YamlParser_Preserves_3_0_Nullable_Keyword()
    {
        const string yaml = """
            openapi: "3.0.0"
            info:
              title: Test
              version: "1.0"
            paths: {}
            components:
              schemas:
                Item:
                  type: object
                  properties:
                    name:
                      type: string
                      nullable: true
            """;

        var doc = await new YamlOpenApiParser().ParseAsync(yaml);

        var item = doc.Schemas["Item"];
        Assert.That(item.Properties["name"].Type, Is.EqualTo("string"));
        Assert.That(item.Properties["name"].Nullable, Is.True);
    }

    [Test]
    public async Task YamlParser_Reads_ReadOnly_And_WriteOnly()
    {
        const string yaml = """
            openapi: "3.0.0"
            info:
              title: Test
              version: "1.0"
            paths: {}
            components:
              schemas:
                Item:
                  type: object
                  properties:
                    id:
                      type: string
                      readOnly: true
                    password:
                      type: string
                      writeOnly: true
            """;

        var doc = await new YamlOpenApiParser().ParseAsync(yaml);

        var item = doc.Schemas["Item"];
        Assert.That(item.Properties["id"].ReadOnly, Is.True);
        Assert.That(item.Properties["id"].WriteOnly, Is.False);
        Assert.That(item.Properties["password"].WriteOnly, Is.True);
        Assert.That(item.Properties["password"].ReadOnly, Is.False);
    }

    [Test]
    public async Task YamlParser_Defaults_ReadOnly_And_WriteOnly_To_False()
    {
        const string yaml = """
            openapi: "3.0.0"
            info:
              title: Test
              version: "1.0"
            paths: {}
            components:
              schemas:
                Item:
                  type: object
                  properties:
                    name:
                      type: string
            """;

        var doc = await new YamlOpenApiParser().ParseAsync(yaml);

        var item = doc.Schemas["Item"];
        Assert.That(item.Properties["name"].ReadOnly, Is.False);
        Assert.That(item.Properties["name"].WriteOnly, Is.False);
    }

    // ── JSON parser ───────────────────────────────────────────────────────

    [Test]
    public async Task JsonParser_Detects_V3_0()
    {
        const string json = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {}
            }
            """;

        var doc = await new JsonOpenApiParser().ParseAsync(json);

        Assert.That(doc.OpenApiVersion, Is.EqualTo(new Version(3, 0, 0)));
    }

    [Test]
    public async Task JsonParser_Detects_V3_1()
    {
        const string json = """
            {
              "openapi": "3.1.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {}
            }
            """;

        var doc = await new JsonOpenApiParser().ParseAsync(json);

        Assert.That(doc.OpenApiVersion, Is.EqualTo(new Version(3, 1, 0)));
    }

    [Test]
    public async Task JsonParser_Returns_Null_When_Version_Absent()
    {
        const string json = """
            {
              "info": { "title": "Test", "version": "1.0" },
              "paths": {}
            }
            """;

        var doc = await new JsonOpenApiParser().ParseAsync(json);

        Assert.That(doc.OpenApiVersion, Is.Null);
    }

    [Test]
    public async Task JsonParser_Returns_Parsed_Version_For_Unrecognised_Version()
    {
        const string json = """
            {
              "openapi": "4.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {}
            }
            """;

        var doc = await new JsonOpenApiParser().ParseAsync(json);

        // Version is parsed and stored even when not explicitly supported
        Assert.That(doc.OpenApiVersion, Is.EqualTo(new Version(4, 0, 0)));
    }

    [Test]
    public async Task JsonParser_Normalises_3_1_Type_Array_To_Nullable()
    {
        const string json = """
            {
              "openapi": "3.1.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "Item": {
                    "type": "object",
                    "properties": {
                      "name": { "type": "string" },
                      "nickname": { "type": ["string", "null"] }
                    }
                  }
                }
              }
            }
            """;

        var doc = await new JsonOpenApiParser().ParseAsync(json);

        var item = doc.Schemas["Item"];
        Assert.That(item.Properties["name"].Type, Is.EqualTo("string"));
        Assert.That(item.Properties["name"].Nullable, Is.False);
        Assert.That(item.Properties["nickname"].Type, Is.EqualTo("string"));
        Assert.That(item.Properties["nickname"].Nullable, Is.True);
    }

    [Test]
    public async Task JsonParser_Preserves_3_0_Nullable_Keyword()
    {
        const string json = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "Item": {
                    "type": "object",
                    "properties": {
                      "name": { "type": "string", "nullable": true }
                    }
                  }
                }
              }
            }
            """;

        var doc = await new JsonOpenApiParser().ParseAsync(json);

        var item = doc.Schemas["Item"];
        Assert.That(item.Properties["name"].Type, Is.EqualTo("string"));
        Assert.That(item.Properties["name"].Nullable, Is.True);
    }

    [Test]
    public async Task JsonParser_Reads_ReadOnly_Flag()
    {
        const string json = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "Item": {
                    "type": "object",
                    "properties": {
                      "id": { "type": "string", "readOnly": true },
                      "password": { "type": "string", "writeOnly": true }
                    }
                  }
                }
              }
            }
            """;

        var doc = await new JsonOpenApiParser().ParseAsync(json);

        var item = doc.Schemas["Item"];
        Assert.That(item.Properties["id"].ReadOnly, Is.True);
        Assert.That(item.Properties["id"].WriteOnly, Is.False);
    }

    [Test]
    public async Task JsonParser_Reads_WriteOnly_Flag()
    {
        const string json = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "Item": {
                    "type": "object",
                    "properties": {
                      "password": { "type": "string", "writeOnly": true }
                    }
                  }
                }
              }
            }
            """;

        var doc = await new JsonOpenApiParser().ParseAsync(json);

        var item = doc.Schemas["Item"];
        Assert.That(item.Properties["password"].WriteOnly, Is.True);
        Assert.That(item.Properties["password"].ReadOnly, Is.False);
    }

    [Test]
    public async Task JsonParser_Defaults_ReadOnly_And_WriteOnly_To_False()
    {
        const string json = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "Item": {
                    "type": "object",
                    "properties": {
                      "name": { "type": "string" }
                    }
                  }
                }
              }
            }
            """;

        var doc = await new JsonOpenApiParser().ParseAsync(json);

        var item = doc.Schemas["Item"];
        Assert.That(item.Properties["name"].ReadOnly, Is.False);
        Assert.That(item.Properties["name"].WriteOnly, Is.False);
    }

    // ── Generator-level diagnostics ───────────────────────────────────────

    [Test]
    public void Generator_Emits_MOA006_For_Missing_Version()
    {
        const string yaml = """
            info:
              title: Test
              version: "1.0"
            paths:
              /ping:
                get:
                  operationId: ping
                  responses:
                    "200":
                      description: OK
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", yaml)]);

        var diagnostics = result.Diagnostics;
        Assert.That(diagnostics.Any(d => d.Id == "MOA006"), Is.True,
            "Expected MOA006 warning for missing openapi version field");
    }

    [Test]
    public void Generator_Emits_MOA006_For_Unrecognised_Version()
    {
        const string yaml = """
            openapi: "4.0.0"
            info:
              title: Test
              version: "1.0"
            paths:
              /ping:
                get:
                  operationId: ping
                  responses:
                    "200":
                      description: OK
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", yaml)]);

        var diagnostics = result.Diagnostics;
        Assert.That(diagnostics.Any(d => d.Id == "MOA006"), Is.True,
            "Expected MOA006 warning for unrecognised openapi version");
    }

    [Test]
    public void Generator_Does_Not_Emit_MOA006_For_V3_0()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", OpenApiFixtures.GetClientYaml)]);

        var diagnostics = result.Diagnostics;
        Assert.That(diagnostics.Any(d => d.Id == "MOA006"), Is.False,
            "Should not emit MOA006 for a valid OpenAPI 3.0 document");
    }
}
