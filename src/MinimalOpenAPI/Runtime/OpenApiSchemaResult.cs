#if NET10_0_OR_GREATER
using System.Collections.Generic;

using Microsoft.AspNetCore.Builder;

namespace MinimalOpenAPI;

/// <summary>
/// Describes a single published OpenAPI schema endpoint mapped by <see cref="ServiceCollectionExtensions.MapOpenApiSchemas"/>.
/// </summary>
/// <param name="Name">The schema file name without extension (e.g. <c>openapi</c>).</param>
/// <param name="Version">
/// The <c>info.version</c> value extracted from the spec file, or <see langword="null"/> when
/// the version cannot be determined (e.g. the file does not exist at mapping time).
/// </param>
/// <param name="PublicPath">
/// The full HTTP path at which the schema is accessible
/// (e.g. <c>/.openapi/schemas/1.0.0/openapi.yaml</c> or the verbatim override path).
/// </param>
/// <param name="HasOverride">
/// <see langword="true"/> when the endpoint path was set via <c>PublishPathOverride</c>;
/// <see langword="false"/> when the default <c>{prefix}/schemas/{version}/{name}.{ext}</c>
/// routing was used.
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