namespace MinimalOpenAPI.Generator.Tests;

/// <summary>
/// The endpoint mapping enforces the OpenAPI <c>required</c> list of multipart form bodies with a
/// generated endpoint filter, so handlers never receive a <see langword="null"/> or incomplete
/// <c>Request</c> (#121).
/// </summary>
[TestFixture]
public class MultipartRequiredFieldsFilterTests
{
    private const string Contract = """
        openapi: "3.0.0"
        info: { title: Uploads, version: "1" }
        paths:
          /albums/{albumId}/photos:
            post:
              operationId: uploadPhotos
              parameters:
                - { name: albumId, in: path, required: true, schema: { type: integer } }
                - { name: overwrite, in: query, schema: { type: boolean } }
              requestBody:
                required: true
                content:
                  multipart/form-data:
                    schema:
                      type: object
                      required: [cover, pages, count, details]
                      properties:
                        cover: { type: string, format: binary }
                        pages:
                          type: array
                          items: { type: string, format: binary }
                        count: { type: integer }
                        caption: { type: string }
                        details:
                          type: object
                          required: [title]
                          properties:
                            title: { type: string }
                            notes: { type: string }
                        location: { $ref: '#/components/schemas/Location' }
              responses:
                "204": { description: Uploaded. }
          /notes:
            post:
              operationId: uploadNote
              requestBody:
                required: true
                content:
                  multipart/form-data:
                    schema:
                      type: object
                      properties:
                        note: { type: string }
              responses:
                "204": { description: Uploaded. }
          /json:
            post:
              operationId: postJson
              requestBody:
                required: true
                content:
                  application/json:
                    schema:
                      type: object
                      required: [name]
                      properties:
                        name: { type: string }
              responses:
                "204": { description: Created. }
        components:
          schemas:
            Location:
              type: object
              required: [latitude, longitude]
              properties:
                latitude: { type: number }
                longitude: { type: number }
        """;

    private static string Mapping()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", Contract)]);
        return GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");
    }

    private static string Section(string mapping, string operationId, string nextOperationId)
    {
        var start = mapping.IndexOf($"// {operationId}", StringComparison.Ordinal);
        var end = mapping.IndexOf($"// {nextOperationId}", start, StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0));
        Assert.That(end, Is.GreaterThan(start));
        return mapping[start..end];
    }

    [Test]
    public void MultipartOperation_ChecksEachRequiredFieldKind()
    {
        var section = Section(Mapping(), "uploadPhotos", "uploadNote");

        Assert.Multiple(() =>
        {
            Assert.That(section, Does.Contain("global::Microsoft.AspNetCore.Http.EndpointFilterExtensions.AddEndpointFilter(uploadPhotosEndpointBase, static async (context, next) =>"));
            Assert.That(section, Does.Contain("if (form.Files.GetFile(\"cover\") is null)"));
            Assert.That(section, Does.Contain("if (form.Files.GetFiles(\"pages\").Count == 0)"));
            Assert.That(section, Does.Contain("if (!form.ContainsKey(\"count\"))"), "value-type fields are checked by key presence");
            Assert.That(section, Does.Not.Contain("\"caption\""), "optional fields are not checked");
            Assert.That(section, Does.Contain("return global::Microsoft.AspNetCore.Http.TypedResults.ValidationProblem(errors);"));
        });
    }

    [Test]
    public void MultipartOperation_ChecksNestedObjects()
    {
        var section = Section(Mapping(), "uploadPhotos", "uploadNote");

        Assert.Multiple(() =>
        {
            // Required nested object: must be present, and then its own required fields too.
            Assert.That(section, Does.Contain("if (!HasFormFieldsWithPrefix(form, \"details.\"))"));
            Assert.That(section, Does.Contain("if (!form.ContainsKey(\"details.title\"))"));
            Assert.That(section, Does.Not.Contain("\"details.notes\""));

            // Optional $ref nested object: its required fields are checked only when it is present.
            Assert.That(section, Does.Contain("if (HasFormFieldsWithPrefix(form, \"location.\"))"));
            Assert.That(section, Does.Contain("if (!form.ContainsKey(\"location.latitude\"))"));
            Assert.That(section, Does.Contain("if (!form.ContainsKey(\"location.longitude\"))"));
        });
    }

    [Test]
    public void FormRecordWithRequiredMembers_IsNotReplacedWhenNull()
    {
        var section = Section(Mapping(), "uploadPhotos", "uploadNote");

        Assert.That(section, Does.Not.Contain("??= new"));
    }

    [Test]
    public void FormRecordWithoutRequiredMembers_NullRequestIsReplacedWithEmptyRecord()
    {
        var section = Section(Mapping(), "uploadNote", "postJson");

        Assert.That(section, Does.Contain("context.Arguments[0] ??= new global::TestProject.Openapi.Endpoints.UploadNoteEndpointBase.Request();"));
    }

    [Test]
    public void RequestArgumentIndex_AccountsForPathAndParameterRecord()
    {
        // uploadPhotos has one path parameter and an [AsParameters] record before the form record,
        // so a null-replacement would target index 2; check through a contract without required members.
        var contract = Contract.Replace("required: [cover, pages, count, details]", "required: []");
        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", contract)]);
        var section = Section(GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs"), "uploadPhotos", "uploadNote");

        Assert.That(section, Does.Contain("context.Arguments[2] ??= new global::TestProject.Openapi.Endpoints.UploadPhotosEndpointBase.Request();"));
    }

    [Test]
    public void JsonOperation_HasNoFormFilter()
    {
        var mapping = Mapping();
        var section = mapping[mapping.IndexOf("// postJson", StringComparison.Ordinal)..];

        Assert.That(section, Does.Not.Contain("AddEndpointFilter"));
    }

    [Test]
    public void FormPrefixHelper_IsEmittedOnlyWhenMultipartOperationsExist()
    {
        Assert.That(Mapping(), Does.Contain("private static bool HasFormFieldsWithPrefix("));

        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", OpenApiFixtures.GetClientYaml)]);
        Assert.That(GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs"), Does.Not.Contain("HasFormFieldsWithPrefix"));
    }
}