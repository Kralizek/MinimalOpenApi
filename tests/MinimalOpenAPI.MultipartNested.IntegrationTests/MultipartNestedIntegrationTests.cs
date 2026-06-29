using System.Net;
using System.Net.Http.Json;

using MinimalOpenAPI.Samples.MultipartNested.Openapi.Contracts;

namespace MinimalOpenAPI.MultipartNested.IntegrationTests;

/// <summary>
/// Integration tests that prove ASP.NET Core correctly binds nested
/// <c>multipart/form-data</c> fields in the generated endpoint handlers.
/// </summary>
[TestFixture]
public class MultipartNestedIntegrationTests
{
    private WebApplicationFactory<MinimalOpenAPI.Samples.MultipartNested.Program> _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WebApplicationFactory<MinimalOpenAPI.Samples.MultipartNested.Program>();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task UploadDocument_BindsFileAndNestedInlineMetadata()
    {
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent([0x01, 0x02]), "file", "test.bin");
        content.Add(new StringContent("My Title"), "metadata.title");
        content.Add(new StringContent("My Source"), "metadata.source");

        var response = await _client.PostAsync("/uploads", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var result = await response.Content.ReadFromJsonAsync<UploadResponse>();
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.FileName, Is.EqualTo("test.bin"));
        Assert.That(result.MetadataTitle, Is.EqualTo("My Title"));
        Assert.That(result.MetadataSource, Is.EqualTo("My Source"));
    }

    [Test]
    public async Task UploadDocument_OmittingOptionalSource_BindsSuccessfully()
    {
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent([0x01]), "file", "only-title.bin");
        content.Add(new StringContent("Title Only"), "metadata.title");

        var response = await _client.PostAsync("/uploads", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var result = await response.Content.ReadFromJsonAsync<UploadResponse>();
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.FileName, Is.EqualTo("only-title.bin"));
        Assert.That(result.MetadataTitle, Is.EqualTo("Title Only"));
        Assert.That(result.MetadataSource, Is.Null);
    }

    [Test]
    public async Task UploadWithTag_BindsFileAndReferencedTagObject()
    {
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent([0xAB, 0xCD]), "file", "tagged.bin");
        content.Add(new StringContent("release"), "tag.name");
        content.Add(new StringContent("v1.0"), "tag.value");

        var response = await _client.PostAsync("/uploads/with-tag", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var result = await response.Content.ReadFromJsonAsync<UploadResponse>();
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.FileName, Is.EqualTo("tagged.bin"));
        Assert.That(result.TagName, Is.EqualTo("release"));
        Assert.That(result.TagValue, Is.EqualTo("v1.0"));
    }

    [Test]
    public async Task UploadWithTag_OmittingOptionalTag_BindsSuccessfully()
    {
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent([0x42]), "file", "no-tag.bin");

        var response = await _client.PostAsync("/uploads/with-tag", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var result = await response.Content.ReadFromJsonAsync<UploadResponse>();
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.FileName, Is.EqualTo("no-tag.bin"));
        Assert.That(result.TagName, Is.Null);
        Assert.That(result.TagValue, Is.Null);
    }
}