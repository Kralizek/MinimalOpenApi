using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.Samples.SmokeTest.Openapi.Endpoints;

namespace MinimalOpenAPI.Samples.SmokeTest;

/// <summary>
/// Demonstrates inline complex <c>additionalProperties</c>: the generated
/// <c>Request.Labels</c> property is typed as
/// <c>Dictionary&lt;string, SetLabelsEndpointBase.RequestLabelsValue&gt;</c> where
/// <c>RequestLabelsValue</c> is a nested record emitted inside the handler base class.
/// </summary>
public sealed class SetLabelsEndpoint : SetLabelsEndpointBase
{
    public override Task<NoContent> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        // In a real implementation labels would be persisted here.
        // Labels are available as a Dictionary<string, RequestLabelsValue> with
        // strongly-typed Name and Color properties on each value record.
        _ = request.Labels;
        return Task.FromResult(TypedResults.NoContent());
    }
}
