namespace MinimalOpenAPI.Abstractions.Models;

/// <summary>
/// Carries the pre-detected format and version of an OpenAPI document to
/// <see cref="IOpenApiParser.CanParse"/>, so that each parser can express its
/// supported scope with a simple, declarative condition such as:
/// <code>return request.Format == OpenApiFormat.Yaml;</code>
/// or, for a version-targeted parser:
/// <code>return request.Format == OpenApiFormat.Yaml &amp;&amp; request.Version?.Major == 4;</code>
/// The call site (the generator) is responsible for the format and version detection;
/// parsers need not repeat that logic.
/// </summary>
/// <param name="Format">The serialisation format detected from the file extension.</param>
/// <param name="Version">
/// The OpenAPI specification version parsed from the document's <c>openapi</c> field,
/// or <see langword="null"/> when the field is absent or its value cannot be parsed.
/// </param>
public sealed record OpenApiParserRequest(OpenApiFormat Format, Version? Version);