using MinimalOpenAPI.Samples.MultipartNested.Openapi.Endpoints;

namespace MinimalOpenAPI.Samples.MultipartNested;

/// <summary>
/// Applies application-specific policies to the generated document upload endpoint.
/// </summary>
public sealed class UploadDocumentEndpointConfiguration : UploadDocumentEndpointConfigurationBase
{
    public override void Configure(RouteHandlerBuilder endpoint)
        => endpoint.DisableAntiforgery();
}

/// <summary>
/// Applies application-specific policies to the generated multipart endpoint that accepts a tag.
/// </summary>
public sealed class UploadWithTagEndpointConfiguration : UploadWithTagEndpointConfigurationBase
{
    public override void Configure(RouteHandlerBuilder endpoint)
        => endpoint.DisableAntiforgery();
}