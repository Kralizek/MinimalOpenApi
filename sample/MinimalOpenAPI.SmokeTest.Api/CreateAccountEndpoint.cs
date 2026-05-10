using Microsoft.AspNetCore.Http.HttpResults;

using MinimalOpenAPI.SmokeTest.Api.Openapi.Contracts;
using MinimalOpenAPI.SmokeTest.Api.Openapi.Endpoints;

namespace MinimalOpenAPI.SmokeTest.Api;

/// <summary>
/// Ensures smoke-test consumers compile with request/response-specific DTOs
/// when <c>readOnly</c>/<c>writeOnly</c> force directional body shapes.
/// </summary>
public sealed class CreateAccountEndpoint : CreateAccountEndpointBase
{
    public override Task<Created<AccountResponse>> HandleAsync(AccountRequest request, CancellationToken cancellationToken)
    {
        var accountId = Guid.NewGuid();
        return Task.FromResult(
            TypedResults.Created(
                $"/accounts/{accountId}",
                new AccountResponse
                {
                    Id = accountId,
                    Email = request.Email,
                    Profile = new ProfileResponse
                    {
                        CreatedAt = DateTimeOffset.UtcNow,
                        DisplayName = request.Profile.DisplayName
                    }
                }));
    }
}
