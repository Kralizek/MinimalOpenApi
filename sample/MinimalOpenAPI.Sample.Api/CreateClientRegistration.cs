using MinimalOpenAPI.Sample.Api.Generated;

namespace MinimalOpenAPI.Sample.Api;

/// <summary>
/// Optional registration customizer for the createClient endpoint.
/// Demonstrates how to add authorization, rate limiting, etc.
/// </summary>
public sealed class CreateClientRegistration : CreateClientEndpointRegistration
{
    public override void Configure(Microsoft.AspNetCore.Builder.RouteHandlerBuilder builder)
    {
        // Example: require authorization on the create endpoint
        // builder.RequireAuthorization();
    }
}
