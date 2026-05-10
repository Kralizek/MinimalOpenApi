using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.Samples.SchemaShapes.Openapi.Contracts;
using MinimalOpenAPI.Samples.SchemaShapes.Openapi.Endpoints;

namespace MinimalOpenAPI.Samples.SchemaShapes;

/// <summary>
/// Demonstrates allOf flattening: <c>CatalogDetail</c> is generated as a single record
/// that merges all properties from <c>Catalog</c> and the inline extension object.
/// </summary>
public sealed class GetCatalogHandler : GetCatalogEndpointBase
{
    public override Task<Results<Ok<CatalogDetail>, NotFound>> HandleAsync(
        System.Guid catalogId,
        CancellationToken cancellationToken)
    {
        var catalog = new CatalogDetail
        {
            Id = catalogId,
            Name = "Sample Catalog",
            ProductCount = 42,
            LastUpdated = DateTimeOffset.UtcNow
        };
        return Task.FromResult<Results<Ok<CatalogDetail>, NotFound>>(TypedResults.Ok(catalog));
    }
}