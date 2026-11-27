using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.Samples.MultipartNested.Openapi.Contracts;
using MinimalOpenAPI.Samples.MultipartNested.Openapi.Endpoints;

namespace MinimalOpenAPI.Samples.MultipartNested;

/// <summary>
/// Handles <c>POST /uploads/notes</c>, whose form fields are all optional. The generated
/// endpoint never passes a <see langword="null"/> <c>Request</c>, even for a form that contains
/// none of its fields, so the handler does not need a null check.
/// </summary>
public sealed class UploadNoteHandler : UploadNoteEndpointBase
{
    public override Task<Ok<UploadResponse>> HandleAsync(
        Request request,
        CancellationToken cancellationToken)
    {
        var response = new UploadResponse
        {
            FileName = request.Attachment?.FileName ?? string.Empty,
            MetadataTitle = request.Note,
        };
        return Task.FromResult(TypedResults.Ok(response));
    }
}