using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.Samples.MultipartNested.Openapi.Contracts;
using MinimalOpenAPI.Samples.MultipartNested.Openapi.Endpoints;

namespace MinimalOpenAPI.Samples.MultipartNested;

/// <summary>
/// Handles <c>POST /uploads</c>: echoes back the bound file name and
/// nested metadata fields so integration tests can verify form binding.
/// </summary>
public sealed class UploadDocumentHandler : UploadDocumentEndpointBase
{
    public override Task<Ok<UploadResponse>> HandleAsync(
        Request request,
        CancellationToken cancellationToken)
    {
        var response = new UploadResponse
        {
            FileName = request.File.FileName,
            MetadataTitle = request.Metadata.Title,
            MetadataSource = request.Metadata.Source,
        };
        return Task.FromResult(TypedResults.Ok(response));
    }
}