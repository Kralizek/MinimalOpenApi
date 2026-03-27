using Microsoft.CodeAnalysis;

namespace MinimalOpenAPI.Generator.Diagnostics;

internal static class DiagnosticDescriptors
{
    private const string Category = "MinimalOpenAPI";

    /// <summary>No concrete implementation found for a generated handler base class.</summary>
    public static readonly DiagnosticDescriptor MissingHandlerImplementation = new(
        id: "MOA001",
        title: "Missing handler implementation",
        messageFormat: "No concrete implementation of '{0}' was found. Create a class that inherits from '{0}'.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>More than one concrete implementation found for a generated handler base class.</summary>
    public static readonly DiagnosticDescriptor DuplicateHandlerImplementation = new(
        id: "MOA002",
        title: "Duplicate handler implementations",
        messageFormat: "Multiple implementations of '{0}' were found: {1}. Exactly one implementation is required.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>More than one concrete implementation found for a generated registration customizer base class.</summary>
    public static readonly DiagnosticDescriptor DuplicateRegistrationCustomizerImplementation = new(
        id: "MOA003",
        title: "Duplicate registration customizer implementations",
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
}
