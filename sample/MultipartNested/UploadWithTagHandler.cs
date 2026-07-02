using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.Samples.MultipartNested.Openapi.Contracts;
using MinimalOpenAPI.Samples.MultipartNested.Openapi.Endpoints;

namespace MinimalOpenAPI.Samples.MultipartNested;

/// <summary>
/// Handles <c>POST /uploads/with-tag</c>: echoes back the bound file name and
/// nested tag fields (populated from a <c>$ref</c> component schema) so integration
/// tests can verify <c>$ref</c>-based form binding.
/// </summary>
public sealed class UploadWithTagHandler : UploadWithTagEndpointBase
{
    public override Task<Ok<UploadResponse>> HandleAsync(
        Request request,
        CancellationToken cancellationToken)
    {
        var response = new UploadResponse
        {
            FileName = request.File.FileName,
            TagName = request.Tag?.Name,
            TagValue = request.Tag?.Value,
        };
        return Task.FromResult(TypedResults.Ok(response));
    }
}