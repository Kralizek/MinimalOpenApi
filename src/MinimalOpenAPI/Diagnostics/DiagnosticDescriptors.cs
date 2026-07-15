using Microsoft.CodeAnalysis;

namespace MinimalOpenAPI.Generator.Diagnostics;

internal static class DiagnosticDescriptors
{
    private const string Category = "MinimalOpenAPI";

    /// <summary>No concrete implementation found for a generated handler base class.</summary>
    public static readonly DiagnosticDescriptor MissingHandlerImplementation = new(
        id: "MOA001",
        title: "Missing handler implementation",
        messageFormat: "No concrete implementation of '{0}' was found. The default implementation will throw NotImplementedException at runtime. Create a class that inherits from '{0}' to provide a real implementation.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>More than one concrete implementation found for a generated handler base class.</summary>
    public static readonly DiagnosticDescriptor DuplicateHandlerImplementation = new(
        id: "MOA002",
        title: "Duplicate handler implementations",
        messageFormat: "Multiple implementations of '{0}' were found: {1}. Exactly one implementation is required.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>More than one concrete implementation found for a generated endpoint configuration base class.</summary>
    public static readonly DiagnosticDescriptor DuplicateEndpointConfigurationImplementation = new(
        id: "MOA003",
        title: "Duplicate endpoint configuration implementations",
        messageFormat: "Multiple implementations of '{0}' were found: {1}. At most one implementation is allowed.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>The OpenAPI file could not be parsed.</summary>
    public static readonly DiagnosticDescriptor ParseError = new(
        id: "MOA004",
        title: "OpenAPI parse error",
        messageFormat: "Failed to parse OpenAPI file '{0}': {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>The OpenAPI file has an unsupported extension.</summary>
    public static readonly DiagnosticDescriptor UnsupportedFileExtension = new(
        id: "MOA005",
        title: "Unsupported OpenAPI file extension",
        messageFormat: "No parser is available for the file extension '{0}' (file: '{1}'). Supported extensions are: .yaml, .yml, .json.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>The OpenAPI document's <c>openapi</c> version field is absent or unrecognised.</summary>
    public static readonly DiagnosticDescriptor UnknownOpenApiVersion = new(
        id: "MOA006",
        title: "Unknown OpenAPI version",
        messageFormat: "The 'openapi' version field in '{0}' is absent or unrecognised. Supported versions are 3.0.x and 3.1.x. Code will still be generated but behaviour may be incorrect for unsupported versions.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>Flattening <c>allOf</c> produced incompatible property types.</summary>
    public static readonly DiagnosticDescriptor AllOfPropertyConflict = new(
        id: "MOA007",
        title: "Conflicting allOf property definitions",
        messageFormat: "Schema '{0}' has incompatible 'allOf' definitions for property '{1}'. Falling back to 'JsonElement'.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>A parameter <c>$ref</c> in an operation's parameters array could not be resolved.</summary>
    public static readonly DiagnosticDescriptor UnresolvedParameterReference = new(
        id: "MOA008",
        title: "Unresolved parameter reference",
        messageFormat: "Parameter reference '{0}' in operation '{1}' could not be resolved. Only local references of the form '#/components/parameters/{{name}}' are supported.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>Multiple OpenAPI files resolve to the same generated spec name.</summary>
    public static readonly DiagnosticDescriptor DuplicateSpecName = new(
        id: "MOA009",
        title: "Duplicate OpenAPI spec name",
        messageFormat: "Multiple OpenAPI files resolve to the spec name '{0}'. Set the Namespace metadata on one or more <OpenApi> items to provide unique generated namespaces. Conflicting files: {1}.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>The <c>ReadWriteSchemaHandling</c> item metadata contains an unsupported value.</summary>
    public static readonly DiagnosticDescriptor InvalidReadWriteSchemaHandling = new(
        id: "MOA010",
        title: "Invalid ReadWriteSchemaHandling value",
        messageFormat: "OpenApi item '{0}' has ReadWriteSchemaHandling='{1}'. Supported values are Ignore, Auto, Split.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>A <c>multipart/form-data</c> property has a shape that cannot be bound via form binding.</summary>
    public static readonly DiagnosticDescriptor UnsupportedMultipartFormShape = new(
        id: "MOA011",
        title: "Unsupported multipart/form-data field shape",
        messageFormat: "Multipart form-data property '{0}' in form record '{1}' has an unsupported shape for form binding. Only scalars, IFormFile (string/binary), IReadOnlyList<IFormFile> (array of binary), and nested object types are supported. This property has been omitted from the generated form DTO.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// Two or more schema names normalise to the same generated C# type name.
    /// </summary>
    public static readonly DiagnosticDescriptor SchemaNameCollision = new(
        id: "MOA012",
        title: "Schema name collision",
        messageFormat: "The schema names {0} all normalise to the generated C# type name '{1}'. Rename one or more of the schemas so each produces a unique type name.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>A schema name cannot be normalised to any valid C# identifier.</summary>
    public static readonly DiagnosticDescriptor UnnormalisableSchemaName = new(
        id: "MOA013",
        title: "Unnormalisable schema name",
        messageFormat: "The schema name '{0}' cannot be normalised to a valid C# identifier. Rename the schema to use letters and digits with optional separators (., -, _).",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// A derived C# type name (scoped variant or inline property type) collides with an
    /// already-generated type from a different component schema.
    /// </summary>
    public static readonly DiagnosticDescriptor GeneratedSymbolCollision = new(
        id: "MOA014",
        title: "Generated symbol collision",
        messageFormat: "The derived C# type name '{0}' conflicts with an already-generated type. This occurs when a schema name variant (e.g. a Request/Response scope suffix or an inline property type) produces the same identifier as an existing component schema. Rename one of the conflicting schemas to avoid duplicate type declarations.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}