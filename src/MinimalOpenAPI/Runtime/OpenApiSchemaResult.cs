#if NET10_0_OR_GREATER
using System.Collections.Generic;

using Microsoft.AspNetCore.Builder;

namespace MinimalOpenAPI;

/// <summary>
/// Describes a single published OpenAPI schema endpoint mapped by <see cref="ServiceCollectionExtensions.MapOpenApiSchemas"/>.
/// </summary>
/// <param name="Name">
/// The display name of the schema, taken from <c>DisplayName</c> metadata or the file name without extension.
/// </param>
/// <param name="Version">
/// The <c>DisplayVersion</c> metadata value, or <see langword="null"/> when not provided.
/// </param>
/// <param name="PublicPath">
/// The full HTTP path at which the schema is accessible, exactly matching the configured <c>PublishAs</c> value.
/// </param>
/// <param name="HasOverride">
/// <see langword="true"/> when the endpoint path is explicitly configured by project metadata.
/// </param>
/// <param name="Endpoint">
/// The <see cref="RouteHandlerBuilder"/> returned when the schema endpoint was registered,
/// allowing further configuration such as <c>.RequireAuthorization()</c>.
/// </param>
public sealed record OpenApiSchemaEndpoint(
    string Name,
    string? Version,
    string PublicPath,
    bool HasOverride,
    RouteHandlerBuilder Endpoint);

/// <summary>
/// The result returned by <see cref="ServiceCollectionExtensions.MapOpenApiSchemas"/>,
/// containing descriptors for all mapped schema endpoints.
/// </summary>
/// <param name="Schemas">The list of descriptors for every mapped schema endpoint.</param>
public sealed record OpenApiSchemaMapResult(
    IReadOnlyList<OpenApiSchemaEndpoint> Schemas);
#endif