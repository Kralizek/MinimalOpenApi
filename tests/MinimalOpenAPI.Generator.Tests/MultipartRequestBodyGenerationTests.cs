namespace MinimalOpenAPI.Generator.Tests;

/// <summary>
/// Tests that <c>multipart/form-data</c> request bodies are correctly parsed and
/// generate a form-bound <c>Request</c> record with <c>[FromForm]</c> attributes.
/// </summary>
[TestFixture]
public class MultipartRequestBodyGenerationTests
{
    private static readonly (string, string)[] YamlAdditionalFiles =
    [
        ("openapi.yaml", OpenApiFixtures.UploadFileYaml)
    ];

    private static readonly (string, string)[] JsonAdditionalFiles =
    [
        ("openapi.json", OpenApiFixtures.UploadFileJson)
    ];

    private static readonly (string, string)[] DocumentYamlFiles =
    [
        ("openapi.yaml", OpenApiFixtures.UploadDocumentYaml)
    ];

    private static readonly (string, string)[] MultipleFilesYamlFiles =
    [
        ("openapi.yaml", OpenApiFixtures.UploadMultipleFilesYaml)
    ];

    private const string UploadFileHandlerImpl = """
        public class UploadFileHandler : UploadFileEndpointBase
        {
            public override System.Threading.Tasks.Task<global::Microsoft.AspNetCore.Http.IResult> HandleAsync(
                Request request,
                System.Threading.CancellationToken cancellationToken) => throw new System.NotImplementedException();
        }
        """;

    private const string UploadDocumentHandlerImpl = """
        public class UploadDocumentHandler : UploadDocumentEndpointBase
        {
            public override System.Threading.Tasks.Task<global::Microsoft.AspNetCore.Http.IResult> HandleAsync(
                Request request,
                System.Threading.CancellationToken cancellationToken) => throw new System.NotImplementedException();
        }
        """;

    private const string BatchUploadHandlerImpl = """
        public class BatchUploadHandler : BatchUploadEndpointBase
        {
            public override System.Threading.Tasks.Task<global::Microsoft.AspNetCore.Http.IResult> HandleAsync(
                Request request,
                System.Threading.CancellationToken cancellationToken) => throw new System.NotImplementedException();
        }
        """;

    // ── Handler base class — YAML spec ────────────────────────────────────

    [Test]
    public void Yaml_GeneratesHandlerBaseClass()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: UploadFileHandlerImpl,
            additionalFiles: YamlAdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "UploadFileEndpointBase.g.cs");

        Assert.That(source, Does.Contain("public class UploadFileEndpointBase"),
            "Handler base class should be generated for multipart operations");
    }

    [Test]
    public void Yaml_HandlerBaseClass_EmitsRequestRecord()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: UploadFileHandlerImpl,
            additionalFiles: YamlAdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "UploadFileEndpointBase.g.cs");

        Assert.That(source, Does.Contain("public sealed record Request"),
            "A nested Request record should be emitted for multipart bodies");
    }

    [Test]
    public void Yaml_HandlerBaseClass_IncludesRequestBodyParameter()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: UploadFileHandlerImpl,
            additionalFiles: YamlAdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "UploadFileEndpointBase.g.cs");

        Assert.That(source, Does.Contain("Request request"),
            "HandleAsync must have a 'Request request' parameter for multipart bodies");
    }

    [Test]
    public void Yaml_RequestRecord_UsesFromFormAttribute()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: UploadFileHandlerImpl,
            additionalFiles: YamlAdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "UploadFileEndpointBase.g.cs");

        Assert.That(source, Does.Contain("FromForm(Name ="),
            "Each property in the Request record must use [FromForm(Name = \"...\")]");
    }

    [Test]
    public void Yaml_RequestRecord_DoesNotUseJsonPropertyName()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: UploadFileHandlerImpl,
            additionalFiles: YamlAdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "UploadFileEndpointBase.g.cs");

        Assert.That(source, Does.Not.Contain("JsonPropertyName"),
            "Multipart Request record must not use [JsonPropertyName]");
    }

    [Test]
    public void Yaml_BinaryField_MapsToIFormFile()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: UploadFileHandlerImpl,
            additionalFiles: YamlAdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "UploadFileEndpointBase.g.cs");

        Assert.That(source, Does.Contain("global::Microsoft.AspNetCore.Http.IFormFile"),
            "string/binary fields must map to IFormFile");
    }

    [Test]
    public void Yaml_NonBinaryField_MapsToString()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: UploadFileHandlerImpl,
            additionalFiles: YamlAdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "UploadFileEndpointBase.g.cs");

        Assert.That(source, Does.Contain("string?"),
            "Optional non-file text fields must map to string?");
    }

    // ── Required vs optional fields ────────────────────────────────────

    [Test]
    public void RequiredFileField_EmitsRequiredKeyword()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: UploadDocumentHandlerImpl,
            additionalFiles: DocumentYamlFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "UploadDocumentEndpointBase.g.cs");

        Assert.That(source, Does.Contain("public required global::Microsoft.AspNetCore.Http.IFormFile File"),
            "Required binary fields must emit 'required' keyword and IFormFile type");
    }

    [Test]
    public void OptionalFileField_DoesNotEmitRequiredKeyword()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: UploadFileHandlerImpl,
            additionalFiles: YamlAdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "UploadFileEndpointBase.g.cs");

        // fileContent is not in the required array so it must be nullable IFormFile?
        Assert.That(source, Does.Contain("global::Microsoft.AspNetCore.Http.IFormFile? FileContent"),
            "Optional binary fields must be nullable IFormFile?");
        Assert.That(source, Does.Not.Contain("required global::Microsoft.AspNetCore.Http.IFormFile? FileContent"),
            "Optional binary fields must not carry the 'required' keyword");
    }

    [Test]
    public void OptionalTextField_EmitsNullableString()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: UploadDocumentHandlerImpl,
            additionalFiles: DocumentYamlFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "UploadDocumentEndpointBase.g.cs");

        Assert.That(source, Does.Contain("string? Description"),
            "Optional text fields must be emitted as nullable string?");
    }

    [Test]
    public void FromFormAttribute_PreservesOriginalFieldName()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: UploadDocumentHandlerImpl,
            additionalFiles: DocumentYamlFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "UploadDocumentEndpointBase.g.cs");

        Assert.That(source, Does.Contain("FromForm(Name = \"file\")"),
            "[FromForm(Name)] must use the original OpenAPI field name, not the C# property name");
        Assert.That(source, Does.Contain("FromForm(Name = \"description\")"),
            "[FromForm(Name)] must use the original OpenAPI field name for all fields");
    }

    // ── Array of binary files ─────────────────────────────────────────────

    [Test]
    public void ArrayOfBinaryField_MapsToIReadOnlyListOfIFormFile()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: BatchUploadHandlerImpl,
            additionalFiles: MultipleFilesYamlFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "BatchUploadEndpointBase.g.cs");

        Assert.That(source, Does.Contain(
            "global::System.Collections.Generic.IReadOnlyList<global::Microsoft.AspNetCore.Http.IFormFile>"),
            "array of string/binary must map to IReadOnlyList<IFormFile>");
    }

    [Test]
    public void ArrayOfBinaryField_RequiredEmitsRequiredKeyword()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: BatchUploadHandlerImpl,
            additionalFiles: MultipleFilesYamlFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "BatchUploadEndpointBase.g.cs");

        Assert.That(source, Does.Contain(
            "public required global::System.Collections.Generic.IReadOnlyList<global::Microsoft.AspNetCore.Http.IFormFile> Files"),
            "Required array-of-file fields must emit 'required' and IReadOnlyList<IFormFile>");
    }

    // ── Endpoint mapping ─────────────────────────────────────────────────

    [Test]
    public void Yaml_GeneratesEndpointMapping()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: UploadFileHandlerImpl,
            additionalFiles: YamlAdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");

        Assert.That(source, Does.Contain("MapPost("),
            "Endpoint mapping should be generated for multipart operations");
        Assert.That(source, Does.Contain("/uploads"));
    }

    [Test]
    public void Yaml_EndpointMapping_IncludesFromFormRequestParameter()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: UploadFileHandlerImpl,
            additionalFiles: YamlAdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");

        Assert.That(source, Does.Contain("[global::Microsoft.AspNetCore.Mvc.FromForm]"),
            "Lambda parameter must be decorated with [FromForm] for multipart bodies");
    }

    [Test]
    public void Yaml_EndpointMapping_RequestParameterUsesFullyQualifiedRequestType()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: UploadFileHandlerImpl,
            additionalFiles: YamlAdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");

        Assert.That(source, Does.Contain("UploadFileEndpointBase.Request request"),
            "Lambda parameter must use the fully-qualified nested Request type");
    }

    [Test]
    public void Yaml_EndpointMapping_PassesRequestToHandlerInvocation()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: UploadFileHandlerImpl,
            additionalFiles: YamlAdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");

        Assert.That(source, Does.Contain("handler.HandleAsync(request, ct)"),
            "Handler invocation must pass the request argument");
    }

    // ── JSON spec ─────────────────────────────────────────────────────────

    [Test]
    public void Json_HandlerBaseClass_EmitsRequestRecord()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: UploadFileHandlerImpl,
            additionalFiles: JsonAdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "UploadFileEndpointBase.g.cs");

        Assert.That(source, Does.Contain("public sealed record Request"),
            "A nested Request record should be emitted for multipart bodies (JSON spec)");
    }

    [Test]
    public void Json_EndpointMapping_IncludesFromFormRequestParameter()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: UploadFileHandlerImpl,
            additionalFiles: JsonAdditionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");

        Assert.That(source, Does.Contain("[global::Microsoft.AspNetCore.Mvc.FromForm]"),
            "Lambda parameter must be decorated with [FromForm] for multipart bodies (JSON spec)");
    }

    // ── Required + nullable: true field ──────────────────────────────────

    [Test]
    public void RequiredNullableField_EmitsNullableType()
    {
        var additionalFiles = new (string, string)[]
        {
            ("openapi.yaml", OpenApiFixtures.NullableRequiredFieldYaml)
        };

        const string handlerImpl = """
            public class NullableUploadHandler : NullableUploadEndpointBase
            {
                public override System.Threading.Tasks.Task<global::Microsoft.AspNetCore.Http.IResult> HandleAsync(
                    Request request,
                    System.Threading.CancellationToken cancellationToken) => throw new System.NotImplementedException();
            }
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: handlerImpl,
            additionalFiles: additionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "NullableUploadEndpointBase.g.cs");

        Assert.That(source, Does.Contain("global::Microsoft.AspNetCore.Http.IFormFile? File"),
            "A required field with nullable: true must be emitted as a nullable type");
    }
    // ── Nested inline object ──────────────────────────────────────────────

    private static readonly (string, string)[] NestedInlineFiles =
    [
        ("openapi.yaml", OpenApiFixtures.NestedInlineObjectMultipartYaml)
    ];

    private const string NestedUploadHandlerImpl = """
        public class NestedUploadHandler : NestedUploadEndpointBase
        {
            public override System.Threading.Tasks.Task<global::Microsoft.AspNetCore.Http.IResult> HandleAsync(
                Request request,
                System.Threading.CancellationToken cancellationToken) => throw new System.NotImplementedException();
        }
        """;

    [Test]
    public void InlineObject_EmitsNestedFormRecord()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NestedUploadHandlerImpl,
            additionalFiles: NestedInlineFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "NestedUploadEndpointBase.g.cs");

        Assert.That(source, Does.Contain("public sealed record RequestMetadata"),
            "Inline object property should produce a nested RequestMetadata record");
    }

    [Test]
    public void InlineObject_NestedRecord_HasFromFormAttributes()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NestedUploadHandlerImpl,
            additionalFiles: NestedInlineFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "NestedUploadEndpointBase.g.cs");

        Assert.That(source, Does.Contain("[global::Microsoft.AspNetCore.Mvc.FromForm(Name = \"title\")]"),
            "Nested record properties must carry [FromForm(Name)] attributes");
        Assert.That(source, Does.Contain("[global::Microsoft.AspNetCore.Mvc.FromForm(Name = \"source\")]"),
            "Optional nested scalar property must carry [FromForm(Name)] attribute");
    }

    [Test]
    public void InlineObject_NestedRecord_HasNoJsonPropertyName()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NestedUploadHandlerImpl,
            additionalFiles: NestedInlineFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "NestedUploadEndpointBase.g.cs");

        Assert.That(source, Does.Not.Contain("[JsonPropertyName"),
            "Form DTOs must not carry [JsonPropertyName] attributes");
    }

    [Test]
    public void InlineObject_RootRecord_ReferencesNestedFormType()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NestedUploadHandlerImpl,
            additionalFiles: NestedInlineFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "NestedUploadEndpointBase.g.cs");

        Assert.That(source, Does.Contain("required RequestMetadata Metadata"),
            "Required inline object property must reference the generated form record type");
    }

    [Test]
    public void InlineObject_OptionalNestedField_IsNullable()
    {
        const string specWithOptionalMetadata = """
            openapi: "3.0.0"
            info:
              title: Test API
              version: "1.0.0"
            paths:
              /nested-upload:
                post:
                  operationId: nestedUpload
                  requestBody:
                    required: true
                    content:
                      multipart/form-data:
                        schema:
                          type: object
                          required:
                            - file
                          properties:
                            file:
                              type: string
                              format: binary
                            metadata:
                              type: object
                              properties:
                                title:
                                  type: string
                  responses:
                    "200":
                      description: OK
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NestedUploadHandlerImpl,
            additionalFiles: [("openapi.yaml", specWithOptionalMetadata)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "NestedUploadEndpointBase.g.cs");

        Assert.That(source, Does.Contain("RequestMetadata? Metadata"),
            "Optional inline object property must be emitted as a nullable form record type");
    }

    // ── $ref object ───────────────────────────────────────────────────────

    private static readonly (string, string)[] RefObjectFiles =
    [
        ("openapi.yaml", OpenApiFixtures.RefObjectMultipartYaml)
    ];

    private const string TaggedUploadHandlerImpl = """
        public class TaggedUploadHandler : TaggedUploadEndpointBase
        {
            public override System.Threading.Tasks.Task<global::Microsoft.AspNetCore.Http.IResult> HandleAsync(
                Request request,
                System.Threading.CancellationToken cancellationToken) => throw new System.NotImplementedException();
        }
        """;

    [Test]
    public void RefObject_EmitsNestedFormRecord()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: TaggedUploadHandlerImpl,
            additionalFiles: RefObjectFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "TaggedUploadEndpointBase.g.cs");

        Assert.That(source, Does.Contain("public sealed record RequestTag"),
            "$ref object property should produce a form-specific nested RequestTag record");
    }

    [Test]
    public void RefObject_NestedRecord_HasFromFormAttributes()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: TaggedUploadHandlerImpl,
            additionalFiles: RefObjectFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "TaggedUploadEndpointBase.g.cs");

        Assert.That(source, Does.Contain("[global::Microsoft.AspNetCore.Mvc.FromForm(Name = \"name\")]"),
            "$ref nested record must carry [FromForm(Name)] on all properties");
        Assert.That(source, Does.Contain("[global::Microsoft.AspNetCore.Mvc.FromForm(Name = \"value\")]"),
            "$ref nested record must carry [FromForm(Name)] on all properties");
    }

    [Test]
    public void RefObject_NestedRecord_HasNoJsonPropertyName()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: TaggedUploadHandlerImpl,
            additionalFiles: RefObjectFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "TaggedUploadEndpointBase.g.cs");

        Assert.That(source, Does.Not.Contain("[JsonPropertyName"),
            "$ref form DTOs must not carry [JsonPropertyName] attributes");
    }

    [Test]
    public void RefObject_RootRecord_ReferencesNestedFormType()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: TaggedUploadHandlerImpl,
            additionalFiles: RefObjectFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "TaggedUploadEndpointBase.g.cs");

        Assert.That(source, Does.Contain("RequestTag? Tag"),
            "Optional $ref object property must reference the generated form record type as nullable");
    }

    // ── Unsupported shapes ────────────────────────────────────────────────

    [Test]
    public void ArrayOfObjects_EmitsMoa011Diagnostic()
    {
        var additionalFiles = new (string, string)[]
        {
            ("openapi.yaml", OpenApiFixtures.UnsupportedArrayOfObjectsMultipartYaml)
        };

        const string handlerImpl = """
            public class MultiTaggedUploadHandler : MultiTaggedUploadEndpointBase
            {
                public override System.Threading.Tasks.Task<global::Microsoft.AspNetCore.Http.IResult> HandleAsync(
                    Request request,
                    System.Threading.CancellationToken cancellationToken) => throw new System.NotImplementedException();
            }
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: handlerImpl,
            additionalFiles: additionalFiles);

        var moa011 = result.Diagnostics.FirstOrDefault(d => d.Id == "MOA011");
        Assert.That(moa011, Is.Not.Null,
            "An array-of-objects multipart field must emit a MOA011 diagnostic");
        Assert.That(moa011!.Severity, Is.EqualTo(Microsoft.CodeAnalysis.DiagnosticSeverity.Error),
            "MOA011 must be an error so that generation of an incomplete form DTO is blocking");
    }

}