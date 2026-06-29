using MinimalOpenAPI.Abstractions.Models;
using MinimalOpenAPI.Parser.Json;
using MinimalOpenAPI.Parser.Yaml;

namespace MinimalOpenAPI.Generator.Tests;

/// <summary>
/// Tests that both parsers correctly populate <see cref="OpenApiRequestBody.ContentType"/>
/// for the supported request body media types.
/// </summary>
[TestFixture]
public class RequestBodyContentTypeParserTests
{
    // ── YAML parser ───────────────────────────────────────────────────────

    [Test]
    public async Task Yaml_JsonRequestBody_HasContentTypeApplicationJson()
    {
        var doc = await ParseYaml(YamlWithRequestBody("application/json"));

        var body = doc.Operations[0].RequestBody;
        Assert.That(body, Is.Not.Null);
        Assert.That(body!.ContentType, Is.EqualTo("application/json"));
    }

    [Test]
    public async Task Yaml_JsonRequestBody_PreservesSchema()
    {
        var doc = await ParseYaml(YamlWithRequestBody("application/json"));

        var body = doc.Operations[0].RequestBody;
        Assert.That(body!.Schema, Is.Not.Null);
        Assert.That(body.Schema!.Properties, Contains.Key("name"));
    }

    [Test]
    public async Task Yaml_MultipartRequestBody_HasContentTypeMultipartFormData()
    {
        var doc = await ParseYaml(YamlWithRequestBody("multipart/form-data"));

        var body = doc.Operations[0].RequestBody;
        Assert.That(body, Is.Not.Null);
        Assert.That(body!.ContentType, Is.EqualTo("multipart/form-data"));
    }

    [Test]
    public async Task Yaml_MultipartRequestBody_PreservesSchema()
    {
        var doc = await ParseYaml(YamlWithRequestBody("multipart/form-data"));

        var body = doc.Operations[0].RequestBody;
        Assert.That(body!.Schema, Is.Not.Null);
        Assert.That(body.Schema!.Properties, Contains.Key("name"));
    }

    [Test]
    public async Task Yaml_WhenBothJsonAndMultipartPresent_PrefersJson()
    {
        var doc = await ParseYaml(YamlWithBothContentTypes());

        var body = doc.Operations[0].RequestBody;
        Assert.That(body, Is.Not.Null);
        Assert.That(body!.ContentType, Is.EqualTo("application/json"));
    }

    [Test]
    public async Task Yaml_UnsupportedContentType_HasNullContentTypeAndNullSchema()
    {
        var doc = await ParseYaml(YamlWithRequestBody("application/octet-stream"));

        var body = doc.Operations[0].RequestBody;
        Assert.That(body, Is.Not.Null);
        Assert.That(body!.ContentType, Is.Null);
        Assert.That(body.Schema, Is.Null);
    }

    [Test]
    public async Task Yaml_UnsupportedContentType_PreservesRequired()
    {
        var doc = await ParseYaml(YamlWithRequestBody("application/octet-stream"));

        var body = doc.Operations[0].RequestBody;
        Assert.That(body!.Required, Is.True);
    }

    // ── JSON parser ───────────────────────────────────────────────────────

    [Test]
    public async Task Json_JsonRequestBody_HasContentTypeApplicationJson()
    {
        var doc = await ParseJson(JsonWithRequestBody("application/json"));

        var body = doc.Operations[0].RequestBody;
        Assert.That(body, Is.Not.Null);
        Assert.That(body!.ContentType, Is.EqualTo("application/json"));
    }

    [Test]
    public async Task Json_JsonRequestBody_PreservesSchema()
    {
        var doc = await ParseJson(JsonWithRequestBody("application/json"));

        var body = doc.Operations[0].RequestBody;
        Assert.That(body!.Schema, Is.Not.Null);
        Assert.That(body.Schema!.Properties, Contains.Key("name"));
    }

    [Test]
    public async Task Json_MultipartRequestBody_HasContentTypeMultipartFormData()
    {
        var doc = await ParseJson(JsonWithRequestBody("multipart/form-data"));

        var body = doc.Operations[0].RequestBody;
        Assert.That(body, Is.Not.Null);
        Assert.That(body!.ContentType, Is.EqualTo("multipart/form-data"));
    }

    [Test]
    public async Task Json_MultipartRequestBody_PreservesSchema()
    {
        var doc = await ParseJson(JsonWithRequestBody("multipart/form-data"));

        var body = doc.Operations[0].RequestBody;
        Assert.That(body!.Schema, Is.Not.Null);
        Assert.That(body.Schema!.Properties, Contains.Key("name"));
    }

    [Test]
    public async Task Json_WhenBothJsonAndMultipartPresent_PrefersJson()
    {
        var doc = await ParseJson(JsonWithBothContentTypes());

        var body = doc.Operations[0].RequestBody;
        Assert.That(body, Is.Not.Null);
        Assert.That(body!.ContentType, Is.EqualTo("application/json"));
    }

    [Test]
    public async Task Json_UnsupportedContentType_HasNullContentTypeAndNullSchema()
    {
        var doc = await ParseJson(JsonWithRequestBody("application/octet-stream"));

        var body = doc.Operations[0].RequestBody;
        Assert.That(body, Is.Not.Null);
        Assert.That(body!.ContentType, Is.Null);
        Assert.That(body.Schema, Is.Null);
    }

    [Test]
    public async Task Json_UnsupportedContentType_PreservesRequired()
    {
        var doc = await ParseJson(JsonWithRequestBody("application/octet-stream"));

        var body = doc.Operations[0].RequestBody;
        Assert.That(body!.Required, Is.True);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static Task<OpenApiDocument> ParseYaml(string yaml)
        => new YamlOpenApiParser().ParseAsync(yaml);

    private static Task<OpenApiDocument> ParseJson(string json)
        => new JsonOpenApiParser().ParseAsync(json);

    private static string YamlWithRequestBody(string contentType) => $"""
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
                  {contentType}:
                    schema:
                      type: object
                      properties:
                        name:
                          type: string
              responses:
                "201":
                  description: Created
        """;

    private static string YamlWithBothContentTypes() => """
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
                  multipart/form-data:
                    schema:
                      type: object
                      properties:
                        file:
                          type: string
              responses:
                "201":
                  description: Created
        """;

    private static string JsonWithRequestBody(string contentType) => $$"""
        {
          "openapi": "3.0.0",
          "info": { "title": "Test API", "version": "1.0.0" },
          "paths": {
            "/items": {
              "post": {
                "operationId": "createItem",
                "requestBody": {
                  "required": true,
                  "content": {
                    "{{contentType}}": {
                      "schema": {
                        "type": "object",
                        "properties": {
                          "name": { "type": "string" }
                        }
                      }
                    }
                  }
                },
                "responses": {
                  "201": { "description": "Created" }
                }
              }
            }
          }
        }
        """;

    private static string JsonWithBothContentTypes() => """
        {
          "openapi": "3.0.0",
          "info": { "title": "Test API", "version": "1.0.0" },
          "paths": {
            "/items": {
              "post": {
                "operationId": "createItem",
                "requestBody": {
                  "required": true,
                  "content": {
                    "application/json": {
                      "schema": {
                        "type": "object",
                        "properties": {
                          "name": { "type": "string" }
                        }
                      }
                    },
                    "multipart/form-data": {
                      "schema": {
                        "type": "object",
                        "properties": {
                          "file": { "type": "string" }
                        }
                      }
                    }
                  }
                },
                "responses": {
                  "201": { "description": "Created" }
                }
              }
            }
          }
        }
        """;
}