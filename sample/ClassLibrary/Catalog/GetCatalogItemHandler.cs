using System.Threading;
using System.Threading.Tasks;

using Contracts.Catalog.Contracts;
using Contracts.Catalog.Endpoints;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Contracts.Handlers;

/// <summary>Handles <c>GET /catalog/items/{id}</c>; item 0 does not exist.</summary>
public sealed class GetCatalogItemHandler : GetCatalogItemEndpointBase
{
    public override Task<Results<Ok<CatalogItem>, NotFound>> HandleAsync(int id, CancellationToken cancellationToken)
    {
        if (id == 0)
            return Task.FromResult<Results<Ok<CatalogItem>, NotFound>>(TypedResults.NotFound());

        return Task.FromResult<Results<Ok<CatalogItem>, NotFound>>(
            TypedResults.Ok(new CatalogItem { Id = id, Name = $"Item {id}" }));
    }
}