using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using MinimalOpenAPI.Parser.Json;
using MinimalOpenAPI.Parser.Yaml;

using MinimalOpenAPIClient.Generator;

namespace MinimalOpenAPI.Generator.Tests;

[TestFixture]
public sealed class ClientGeneratorTests
{
    [Test]
    public async Task Json_omitted_response_content_generates_bodyless_client()
    {
        const string content = """
            {"openapi":"3.0.3","paths":{"/items":{"get":{"responses":{"200":{
              "description":"OK"
            }}}}}}
            """;
        var document = await new JsonOpenApiParser().ParseAsync(content);
        var response = document.Operations.Single().Responses.Single();
        Assert.That(response.HasContent, Is.False);
        Assert.That(response.HasContentRepresentations, Is.False);
        Assert.That(response.Schema, Is.Null);

        var result = Run(content, "api.json");
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.GeneratedTrees, Has.Length.EqualTo(1));
    }

    [Test]
    public async Task Json_empty_response_content_preserves_declaration_and_generates_bodyless_client()
    {
        const string content = """
            {"openapi":"3.0.3","paths":{"/items":{"get":{"responses":{"200":{
              "description":"OK","content":{}
            }}}}}}
            """;
        var document = await new JsonOpenApiParser().ParseAsync(content);
        var response = document.Operations.Single().Responses.Single();
        Assert.That(response.HasContent, Is.True);
        Assert.That(response.HasContentRepresentations, Is.False);
        Assert.That(response.Schema, Is.Null);

        var result = Run(content, "api.json");
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.GeneratedTrees, Has.Length.EqualTo(1));
    }

    [Test]
    public async Task Yaml_omitted_response_content_generates_bodyless_client()
    {
        const string content = """
            openapi: 3.0.3
            paths:
              /items:
                get:
                  responses:
                    '200':
                      description: OK
            """;
        var document = await new YamlOpenApiParser().ParseAsync(content);
        var response = document.Operations.Single().Responses.Single();
        Assert.That(response.HasContent, Is.False);
        Assert.That(response.HasContentRepresentations, Is.False);
        Assert.That(response.Schema, Is.Null);

        var result = Run(content, "api.yaml");
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.GeneratedTrees, Has.Length.EqualTo(1));
    }

    [Test]
    public async Task Yaml_empty_response_content_preserves_declaration_and_generates_bodyless_client()
    {
        const string content = """
            openapi: 3.0.3
            paths:
              /items:
                get:
                  responses:
                    '200':
                      description: OK
                      content: {}
            """;
        var document = await new YamlOpenApiParser().ParseAsync(content);
        var response = document.Operations.Single().Responses.Single();
        Assert.That(response.HasContent, Is.True);
        Assert.That(response.HasContentRepresentations, Is.False);
        Assert.That(response.Schema, Is.Null);

        var result = Run(content, "api.yaml");
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(result.GeneratedTrees, Has.Length.EqualTo(1));
    }

    [TestCase("application/octet-stream")]
    [TestCase("multipart/form-data")]
    public void Unsupported_request_body_reports_diagnostic(string contentType)
    {
        var result = Run($$$$$"""
            {"openapi":"3.0.3","paths":{"/items":{"post":{
            "requestBody":{"required":true,"content":{"{{{{{contentType}}}}}":{"schema":{"type":"string"}}}},
              "responses":{"204":{"description":"OK"}}
            }}}}
            """);
        AssertGenerationError(result, "request");
    }

    [TestCase("query", "\"style\":\"pipeDelimited\",")]
    [TestCase("query", "\"explode\":false,")]
    [TestCase("header", "")]
    [TestCase("cookie", "")]
    [TestCase("path", "")]
    public void Unsupported_array_serialization_reports_diagnostic(string location, string settings)
    {
        var result = Run($$$$$"""
            {"openapi":"3.0.3","paths":{"/items":{"get":{
            "parameters":[{"name":"tags","in":"{{{{{location}}}}}",{{{{{settings}}}}}"schema":{"type":"array","items":{"type":"string"}}}],
              "responses":{"204":{"description":"OK"}}
            }}}}
            """);
        AssertGenerationError(result, "serialization");
    }

    [Test]
    public void Yaml_parameter_serialization_metadata_is_preserved()
    {
        var result = Run("""
            openapi: 3.0.3
            paths:
              /items:
                get:
                  parameters:
                    - name: tags
                      in: query
                      explode: false
                      schema:
                        type: array
                        items: {type: string}
                  responses:
                    '204': {description: OK}
            """, "api.yaml");
        AssertGenerationError(result, "serialization");
    }

    [Test]
    public void Incompatible_success_bodies_report_diagnostic()
    {
        var result = Run("""
            {"openapi":"3.0.3","paths":{"/items":{"get":{
              "responses":{
                "200":{"description":"OK","content":{"application/json":{"schema":{"type":"integer"}}}},
                "204":{"description":"No content"}
              }
            }}}}
            """);
        AssertGenerationError(result, "incompatible success");
    }

    [Test]
    public void Cyclic_aliases_report_diagnostic()
    {
        var result = Run("""
            {"openapi":"3.0.3","components":{"schemas":{"Loop":{"$ref":"#/components/schemas/Loop"}}},
             "paths":{"/items":{"get":{"responses":{"200":{"description":"OK","content":{
               "application/json":{"schema":{"$ref":"#/components/schemas/Loop"}}
             }}}}}}}
            """);
        AssertGenerationError(result, "Cyclic");
    }

    private static void AssertGenerationError(GeneratorDriverRunResult result, string message)
    {
        Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Id), Is.EqualTo(new[] { "MOAC003" }));
        Assert.That(result.Diagnostics[0].GetMessage(), Does.Contain(message));
        Assert.That(result.GeneratedTrees, Is.Empty);
    }

    [TestCase("text/plain")]
    [TestCase("application/octet-stream")]
    public void Unsupported_success_media_reports_diagnostic(string mediaType)
    {
        var content = """
        {"openapi":"3.0.3","paths":{"/items":{"get":{"responses":{"200":{
          "description":"OK","content":{"MEDIA":{"schema":{"type":"string"}}}
        }}}}}}
        """.Replace("MEDIA", mediaType);
        AssertGenerationError(Run(content), "unsupported success response");
    }

    [TestCase("MyApp.bad-name")]
    [TestCase("MyApp.class")]
    [TestCase("MyApp.Generic<T>")]
    public void Invalid_namespace_reports_diagnostic(string targetNamespace)
      => AssertGenerationError(Run("""{"openapi":"3.0.3","paths":{}}""", targetNamespace: targetNamespace), "Invalid client namespace");

    [TestCase("#/components/schemas/Missing")]
    [TestCase("other.yaml#/components/schemas/Existing")]
    public void Unresolved_all_of_reference_reports_diagnostic(string reference)
    {
        var content = """
        {"openapi":"3.0.3","paths":{},"components":{"schemas":{
          "Existing":{"type":"object","properties":{"value":{"type":"string"}}},
          "Combined":{"allOf":[{"$ref":"REFERENCE"}]}
        }}}
        """.Replace("REFERENCE", reference);
        AssertGenerationError(Run(content), "Unable to resolve schema reference");
    }

    [Test]
    public void Recursive_array_alias_reports_diagnostic()
    {
        var result = Run("""
        {"openapi":"3.0.3","components":{"schemas":{"Loop":{"type":"array","items":{"$ref":"#/components/schemas/Loop"}}}},
         "paths":{"/items":{"get":{"responses":{"200":{"description":"OK","content":{
           "application/json":{"schema":{"$ref":"#/components/schemas/Loop"}}
         }}}}}}}
        """);
        AssertGenerationError(result, "Recursive array");
    }

    private static GeneratorDriverRunResult Run(string content, string path = "api.json", string? targetNamespace = null)
    {
        var files = ImmutableArray.Create<AdditionalText>(new TestAdditionalText(path, content));
        var options = new TestAnalyzerConfigOptionsProvider(files.ToArray(), "TestProject",
            "build_metadata.AdditionalFiles.MinimalOpenApiClientFile",
            "build_metadata.AdditionalFiles.MinimalOpenApiClientNamespace",
            "schemaId", "publishAs", "displayName", "displayVersion", "readWrite",
            "build_property.RootNamespace", specNameOverride: targetNamespace);
        return CSharpGeneratorDriver.Create(new MinimalOpenApiClientGenerator())
            .AddAdditionalTexts(files)
            .WithUpdatedAnalyzerConfigOptions(options)
            .RunGenerators(CSharpCompilation.Create("TestProject"))
            .GetRunResult();
    }
}