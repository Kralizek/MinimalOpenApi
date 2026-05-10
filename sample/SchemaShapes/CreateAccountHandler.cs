using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.Samples.SchemaShapes.Openapi.Contracts;
using MinimalOpenAPI.Samples.SchemaShapes.Openapi.Endpoints;

namespace MinimalOpenAPI.Samples.SchemaShapes;

/// <summary>
/// Demonstrates readOnly/writeOnly filtering: the generator emits <c>AccountRequest</c>
/// (omits readOnly fields like id and createdAt) and <c>AccountResponse</c>
/// (omits writeOnly fields like password).
/// </summary>
public sealed class CreateAccountHandler : CreateAccountEndpointBase
{
    public override Task<Created<AccountResponse>> HandleAsync(AccountRequest request, CancellationToken cancellationToken)
    {
        var response = new AccountResponse
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            Role = request.Role,
            CreatedAt = DateTimeOffset.UtcNow,
            ProfileUrl = request.ProfileUrl
        };
        return Task.FromResult(TypedResults.Created($"/accounts/{response.Id}", response));
    }
}