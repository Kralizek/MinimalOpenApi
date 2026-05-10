using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.Samples.SchemaShapes.Openapi.Contracts;
using MinimalOpenAPI.Samples.SchemaShapes.Openapi.Endpoints;

namespace MinimalOpenAPI.Samples.SchemaShapes;

/// <summary>
/// Demonstrates enums, DateOnly, nested inline objects, and additionalProperties.
/// The <c>Product.Dimensions</c> property is a nested inline object record.
/// The <c>Product.Tags</c> property is <c>Dictionary&lt;string, string&gt;</c> (primitive value).
/// The <c>Product.Attributes</c> property is <c>Dictionary&lt;string, ProductAttributesValue&gt;</c>
/// (inline object value type).
/// </summary>
public sealed class CreateProductHandler : CreateProductEndpointBase
{
    public override Task<Created<Product>> HandleAsync(Product request, CancellationToken cancellationToken)
    {
        // In a real implementation the product would be persisted.
        // Here we return the same object to demonstrate the generated types.
        return Task.FromResult(TypedResults.Created("/products/new", request));
    }
}