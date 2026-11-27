using MinimalOpenAPI.Parser.Json;
using MinimalOpenAPI.Parser.Yaml;

namespace MinimalOpenAPI.Generator.Tests;

[TestFixture]
public class FileResponseGenerationTests
{
    private const string ImageWildcardYaml = """
        openapi: "3.0.0"
        info:
          title: Test
          version: "1.0"
        paths:
          /entries/{entryId}/photos/{photoId}:
            parameters:
              - { name: entryId, in: path, required: true, schema: { type: string, format: uuid } }
              - { name: photoId, in: path, required: true, schema: { type: string, format: uuid } }
            get:
              operationId: getEntryPhoto
              responses:
                "200":
                  description: The photo content.
                  content:
                    image/*:
                      schema:
                        type: string
                        format: binary
                "404":
                  description: Not found.
        """;

    [Test]
    public void WildcardImageResponse_GeneratesFileWrapperInResultUnion()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", ImageWildcardYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetEntryPhotoEndpointBase.g.cs");

        Assert.That(source, Does.Contain("Results<OkFile, global::Microsoft.AspNetCore.Http.HttpResults.NotFound>"));
        Assert.That(source, Does.Not.Contain("HttpResults.Ok>"));
        Assert.That(source, Does.Not.Contain("HttpResults.Ok<string>"));
        Assert.That(source, Does.Contain(
            "public sealed class OkFile : global::Microsoft.AspNetCore.Http.IResult, global::Microsoft.AspNetCore.Http.IStatusCodeHttpResult, global::Microsoft.AspNetCore.Http.IContentTypeHttpResult, global::Microsoft.AspNetCore.Http.IFileHttpResult"));
        Assert.That(source, Does.Contain("public int? StatusCode => global::Microsoft.AspNetCore.Http.StatusCodes.Status200OK;"));
        Assert.That(source, Does.Contain("httpContext.Response.StatusCode = global::Microsoft.AspNetCore.Http.StatusCodes.Status200OK;"));
        Assert.That(source, Does.Contain("global::Microsoft.AspNetCore.Http.TypedResults.Stream(FileStream, contentType: ContentType"));
        Assert.That(source, Does.Contain("global::Microsoft.AspNetCore.Http.TypedResults.File(FileContents!, contentType: ContentType"));
    }

    [Test]
    public void WildcardImageResponse_RequiresAndValidatesConcreteContentType()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", ImageWildcardYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetEntryPhotoEndpointBase.g.cs");

        Assert.That(source, Does.Contain("public OkFile(global::System.IO.Stream fileStream, string contentType, string? fileDownloadName = null"));
        Assert.That(source, Does.Contain("public OkFile(byte[] fileContents, string contentType, string? fileDownloadName = null"));
        Assert.That(source, Does.Contain("ValidateContentType(contentType)"));
        Assert.That(source, Does.Contain("private static string ValidateContentType(string contentType)"));
        Assert.That(source, Does.Contain("!essence.StartsWith(\"image/\", global::System.StringComparison.OrdinalIgnoreCase)"));
        Assert.That(source, Does.Contain("does not match the declared response media type 'image/*'."));
    }

    [Test]
    public void WildcardImageResponse_RecordsStatusWithoutContentTypeInEndpointMetadata()
    {
        // ASP.NET Core response metadata rejects wildcard content types.
        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", ImageWildcardYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");

        Assert.That(source, Does.Contain(".Produces(global::Microsoft.AspNetCore.Http.StatusCodes.Status200OK)"));
        Assert.That(source, Does.Not.Contain("image/*"));
        Assert.That(source, Does.Contain(".Produces(global::Microsoft.AspNetCore.Http.StatusCodes.Status404NotFound);"));
        Assert.That(source, Does.Not.Contain(".Produces<string>"));
    }

    [Test]
    public void ConcreteMediaType_FixesContentTypeWithoutContentTypeParameter()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", SingleResponseYaml("200", "application/pdf", BinarySchema))]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetFileEndpointBase.g.cs");

        Assert.That(source, Does.Contain("Task<OkFile> HandleAsync("));
        Assert.That(source, Does.Contain("public OkFile(global::System.IO.Stream fileStream, string? fileDownloadName = null"));
        Assert.That(source, Does.Contain("public OkFile(byte[] fileContents, string? fileDownloadName = null"));
        Assert.That(source, Does.Contain(", null, \"application/pdf\", fileDownloadName,"));
        Assert.That(source, Does.Not.Contain("fileStream, string contentType"));
        Assert.That(source, Does.Not.Contain("ValidateContentType"));
    }

    [Test]
    public void AnyMediaTypeWildcard_ValidatesOnlyThatContentTypeIsConcrete()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", SingleResponseYaml("200", "\"*/*\"", BinarySchema))]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetFileEndpointBase.g.cs");

        Assert.That(source, Does.Contain("private static string ValidateContentType(string contentType)"));
        Assert.That(source, Does.Contain("essence.IndexOf('*') >= 0)"));
        Assert.That(source, Does.Not.Contain("essence.StartsWith("));
    }

    [TestCase("application/octet-stream", "")]
    [TestCase("application/pdf", "")]
    [TestCase("image/png", "")]
    [TestCase("audio/mpeg", "")]
    [TestCase("video/mp4", "")]
    [TestCase("text/csv", BinarySchema)]
    [TestCase("application/zip", BinarySchema)]
    public void BinaryOrFileMediaTypes_GenerateFileWrapper(string mediaType, string schema)
    {
        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", SingleResponseYaml("200", mediaType, schema))]);

        var handler = GeneratorTestHelper.GetGeneratedSource(result, "GetFileEndpointBase.g.cs");
        var mapping = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");

        Assert.That(handler, Does.Contain("Task<OkFile> HandleAsync("));
        Assert.That(mapping, Does.Contain($".Produces(global::Microsoft.AspNetCore.Http.StatusCodes.Status200OK, contentType: \"{mediaType}\");"));
        Assert.That(result.Diagnostics.Where(d => d.Id == "MOA018"), Is.Empty);
    }

    [Test]
    public void NonOkStatuses_UseStatusSpecificFileWrapperNames()
    {
        const string yaml = """
            openapi: "3.0.0"
            info:
              title: Test
              version: "1.0"
            paths:
              /files:
                post:
                  operationId: createFile
                  responses:
                    "201":
                      description: Created file.
                      content:
                        application/octet-stream: {}
                    "206":
                      description: Partial file.
                      content:
                        application/octet-stream: {}
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", yaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "CreateFileEndpointBase.g.cs");

        Assert.That(source, Does.Contain("Results<CreatedFile, Status206File>"));
        Assert.That(source, Does.Contain("public int? StatusCode => global::Microsoft.AspNetCore.Http.StatusCodes.Status201Created;"));
        Assert.That(source, Does.Contain("public int? StatusCode => 206;"));
        Assert.That(source, Does.Contain("httpContext.Response.StatusCode = 206;"));
    }

    [Test]
    public void JsonMediaType_IsPreferredOverFileMediaType()
    {
        const string yaml = """
            openapi: "3.0.0"
            info:
              title: Test
              version: "1.0"
            paths:
              /files:
                get:
                  operationId: getFile
                  responses:
                    "200":
                      description: OK
                      content:
                        image/png:
                          schema:
                            type: string
                            format: binary
                        application/json:
                          schema:
                            type: object
                            properties:
                              url:
                                type: string
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", yaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetFileEndpointBase.g.cs");

        Assert.That(source, Does.Contain("Ok<OkResponse>"));
        Assert.That(source, Does.Not.Contain("OkFile"));
    }

    [Test]
    public void FileResponseWithObjectSchema_DoesNotGenerateInlineResponseDto()
    {
        const string schema = """

                          schema:
                            type: object
                            properties:
                              ignored:
                                type: string
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", SingleResponseYaml("200", "image/png", schema))]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetFileEndpointBase.g.cs");

        Assert.That(source, Does.Contain("Task<OkFile> HandleAsync("));
        Assert.That(source, Does.Not.Contain("record OkResponse"));
    }

    [Test]
    public void FileResponseExcludedFromHandler_GeneratesNoWrapperButKeepsMetadata()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", ImageWildcardYaml)],
            handlers: [(null, "getEntryPhoto", null, "200")]);

        var handler = GeneratorTestHelper.GetGeneratedSource(result, "GetEntryPhotoEndpointBase.g.cs");
        var mapping = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");

        Assert.That(handler, Does.Not.Contain("OkFile"));
        Assert.That(mapping, Does.Contain(".Produces(global::Microsoft.AspNetCore.Http.StatusCodes.Status200OK)"));
    }

    [Test]
    public void JsonParser_GeneratesFileWrapper()
    {
        const string json = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {
                "/files": {
                  "get": {
                    "operationId": "getFile",
                    "responses": {
                      "200": {
                        "description": "OK",
                        "content": {
                          "image/*": { "schema": { "type": "string", "format": "binary" } }
                        }
                      },
                      "404": { "description": "Not found" }
                    }
                  }
                }
              }
            }
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.json", json)]);

        var handler = GeneratorTestHelper.GetGeneratedSource(result, "GetFileEndpointBase.g.cs");
        var mapping = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");

        Assert.That(handler, Does.Contain("Results<OkFile, global::Microsoft.AspNetCore.Http.HttpResults.NotFound>"));
        Assert.That(handler, Does.Contain("ValidateContentType(contentType)"));
        Assert.That(mapping, Does.Contain(".Produces(global::Microsoft.AspNetCore.Http.StatusCodes.Status200OK)"));
    }

    [TestCase("text/plain", "")]
    [TestCase("text/csv", "")]
    [TestCase("application/xml", "")]
    [TestCase("text/plain", StringSchema)]
    public void UnsupportedResponseMediaType_ReportsMOA018(string mediaType, string schema)
    {
        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", SingleResponseYaml("200", mediaType, schema))]);

        var diagnostic = result.Diagnostics.Single(d => d.Id == "MOA018");

        Assert.That(diagnostic.Severity, Is.EqualTo(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning));
        Assert.That(diagnostic.GetMessage(), Does.Contain("Response '200' of operation 'getFile'"));
        Assert.That(diagnostic.GetMessage(), Does.Contain($"({mediaType})"));
    }

    [Test]
    public void UnsupportedResponseMediaTypes_AreListedInDocumentOrder()
    {
        const string yaml = """
            openapi: "3.0.0"
            info:
              title: Test
              version: "1.0"
            paths:
              /files:
                get:
                  operationId: getFile
                  responses:
                    "200":
                      description: OK
                      content:
                        text/plain: {}
                        application/xml: {}
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", yaml)]);

        var diagnostic = result.Diagnostics.Single(d => d.Id == "MOA018");
        Assert.That(diagnostic.GetMessage(), Does.Contain("(text/plain, application/xml)"));
    }

    [Test]
    public void SupportedAlongsideUnsupportedMediaType_DoesNotReportMOA018()
    {
        const string yaml = """
            openapi: "3.0.0"
            info:
              title: Test
              version: "1.0"
            paths:
              /files:
                get:
                  operationId: getFile
                  responses:
                    "200":
                      description: OK
                      content:
                        application/xml: {}
                        application/json:
                          schema:
                            type: string
                    "204":
                      description: No content
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", yaml)]);

        Assert.That(result.Diagnostics.Where(d => d.Id == "MOA018"), Is.Empty);
    }

    [Test]
    public async Task Parsers_RecordDeclaredResponseMediaTypesInDocumentOrder()
    {
        const string yaml = """
            openapi: "3.0.0"
            info:
              title: Test
              version: "1.0"
            paths:
              /files:
                get:
                  operationId: getFile
                  responses:
                    "200":
                      description: OK
                      content:
                        text/plain: {}
                        image/png: {}
                    "204":
                      description: No content
            """;
        const string json = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {
                "/files": {
                  "get": {
                    "operationId": "getFile",
                    "responses": {
                      "200": { "description": "OK", "content": { "text/plain": {}, "image/png": {} } },
                      "204": { "description": "No content" }
                    }
                  }
                }
              }
            }
            """;

        var yamlDocument = await new YamlOpenApiParser().ParseAsync(yaml);
        var jsonDocument = await new JsonOpenApiParser().ParseAsync(json);

        foreach (var document in new[] { yamlDocument, jsonDocument })
        {
            var responses = document.Operations.Single().Responses;
            var ok = responses.Single(r => r.StatusCode == 200);
            var noContent = responses.Single(r => r.StatusCode == 204);

            Assert.That(ok.DeclaredContentTypes, Is.EqualTo(new[] { "text/plain", "image/png" }));
            Assert.That(ok.ContentType, Is.EqualTo("image/png"));
            Assert.That(noContent.DeclaredContentTypes, Is.Empty);
            Assert.That(noContent.ContentType, Is.Null);
        }
    }

    private const string BinarySchema = """

                          schema:
                            type: string
                            format: binary
        """;

    private const string StringSchema = """

                          schema:
                            type: string
        """;

    private static string SingleResponseYaml(string status, string mediaType, string schema)
    {
        var mediaTypeBody = string.IsNullOrEmpty(schema) ? " {}" : schema.TrimEnd();
        return $"""
            openapi: "3.0.0"
            info:
              title: Test
              version: "1.0"
            paths:
              /files:
                get:
                  operationId: getFile
                  responses:
                    "{status}":
                      description: Response
                      content:
                        {mediaType}:{mediaTypeBody}
            """;
    }
}