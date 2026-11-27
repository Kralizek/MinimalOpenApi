using System.Text;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

using MinimalOpenAPI.Samples.ResponseResults.Openapi.Endpoints;

namespace MinimalOpenAPI.Samples.ResponseResults;

/// <summary>
/// Demonstrates an <c>OkFile</c> file result for an <c>image/*</c> response.
/// The declared media type is a wildcard, so the handler supplies the concrete content type;
/// the generated wrapper rejects a content type outside <c>image/*</c>.
/// </summary>
public sealed class GetOrderBadgeHandler : GetOrderBadgeEndpointBase
{
    private readonly InMemoryOrderStore _store;

    public GetOrderBadgeHandler(InMemoryOrderStore store) => _store = store;

    public override Task<Results<OkFile, NotFoundProblem>> HandleAsync(
        System.Guid id,
        CancellationToken cancellationToken)
    {
        var order = _store.Get(id);
        if (order is null)
        {
            return Task.FromResult<Results<OkFile, NotFoundProblem>>(
                new NotFoundProblem(new ProblemDetails { Title = "Order not found" }));
        }

        var svg = $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="240" height="40">
              <rect width="240" height="40" rx="6" fill="#2b6cb0"/>
              <text x="12" y="26" fill="#fff" font-family="sans-serif" font-size="16">{System.Security.SecurityElement.Escape(order.ExternalReference)}</text>
            </svg>
            """;

        return Task.FromResult<Results<OkFile, NotFoundProblem>>(
            new OkFile(Encoding.UTF8.GetBytes(svg), "image/svg+xml"));
    }
}