using System.Text;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using MinimalOpenAPI.IntegrationTests.FileResponses.Endpoints;

namespace MinimalOpenAPI.IntegrationTests;

public sealed class GetTestPhotoEndpoint : GetTestPhotoEndpointBase
{
    internal static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public override Task<Results<OkFile, NotFound>> HandleAsync(string photoId, CancellationToken cancellationToken)
        => Task.FromResult<Results<OkFile, NotFound>>(photoId == "known"
            ? new OkFile(Png, "image/png")
            : TypedResults.NotFound());
}

public sealed class CreateTestReportEndpoint : CreateTestReportEndpointBase
{
    internal const string Content = "%PDF-1.7 test report";

    public override Task<CreatedFile> HandleAsync(CancellationToken cancellationToken)
        => Task.FromResult(new CreatedFile(new MemoryStream(Encoding.ASCII.GetBytes(Content)), fileDownloadName: "report.pdf"));
}

[TestFixture]
public class FileResponseIntegrationTests
{
    [Test]
    public async Task WildcardFileResponse_WritesStatusContentTypeAndBytes()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/file-response-test/photos/known");

        Assert.That((int)response.StatusCode, Is.EqualTo(200));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("image/png"));
        Assert.That(await response.Content.ReadAsByteArrayAsync(), Is.EqualTo(GetTestPhotoEndpoint.Png));
    }

    [Test]
    public async Task FileResponseUnion_StillReturnsOtherDeclaredResults()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/file-response-test/photos/unknown");

        Assert.That((int)response.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task ConcreteFileResponse_WritesDeclaredStatusContentTypeAndDownloadName()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/file-response-test/reports", content: null);

        Assert.That((int)response.StatusCode, Is.EqualTo(201));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/pdf"));
        Assert.That(response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName,
            Is.EqualTo("report.pdf"));
        Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo(CreateTestReportEndpoint.Content));
    }

    [Test]
    public void FileResponseEndpointMetadata_KeepsConcreteMediaTypes()
    {
        using var factory = new WebApplicationFactory<Program>();
        _ = factory.Services;

        var endpoints = factory.Services.GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .ToList();

        var photo = endpoints.Single(e => e.RoutePattern.RawText == "/file-response-test/photos/{photoId}")
            .Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>()
            .Single(m => m.StatusCode == 200);
        var report = endpoints.Single(e => e.RoutePattern.RawText == "/file-response-test/reports")
            .Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>()
            .Single(m => m.StatusCode == 201);

        // Wildcard media types cannot be expressed in ASP.NET Core response metadata.
        Assert.That(photo.ContentTypes, Is.Empty);
        Assert.That(report.ContentTypes, Is.EquivalentTo(new[] { "application/pdf" }));
    }

    [TestCase("image/png")]
    [TestCase("IMAGE/JPEG")]
    [TestCase("image/svg+xml; charset=utf-8")]
    public void WildcardFileResult_AcceptsMatchingConcreteContentType(string contentType)
    {
        var result = new GetTestPhotoEndpointBase.OkFile([1], contentType);

        Assert.That(result.ContentType, Is.EqualTo(contentType));
        Assert.That(result.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
    }

    [TestCase("text/plain")]
    [TestCase("image/*")]
    [TestCase("image/")]
    [TestCase("image")]
    [TestCase("imagery/png")]
    [TestCase(" ")]
    public void WildcardFileResult_RejectsContentTypeOutsideDeclaredPattern(string contentType)
    {
        Assert.Throws<ArgumentException>(() => _ = new GetTestPhotoEndpointBase.OkFile([1], contentType));
    }

    [Test]
    public void ConcreteFileResult_UsesDeclaredContentType()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        var result = new CreateTestReportEndpointBase.CreatedFile(stream);

        Assert.That(result.ContentType, Is.EqualTo("application/pdf"));
        Assert.That(result.StatusCode, Is.EqualTo(StatusCodes.Status201Created));
        Assert.That(result.FileStream, Is.SameAs(stream));
        Assert.That(result.FileContents, Is.Null);
    }
}