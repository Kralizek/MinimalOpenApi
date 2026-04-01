using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using MinimalOpenAPI.Abstractions;
using MinimalOpenAPI.Abstractions.Models;
using MinimalOpenAPI.Generator.CodeGen;
using MinimalOpenAPI.Generator.Diagnostics;
using MinimalOpenAPI.Parser.Json;
using MinimalOpenAPI.Parser.Yaml;

namespace MinimalOpenAPI.Generator;

/// <summary>
/// Roslyn incremental source generator that reads OpenAPI specification files and emits
/// strongly-typed DTOs, abstract handler base classes, registration customizer bases,
/// DI registration extensions and endpoint mapping extensions for use with ASP.NET Core
/// Minimal APIs.
/// </summary>
[Generator]
public sealed class MinimalOpenApiGenerator : IIncrementalGenerator
{
    private const string MetadataKey = "build_metadata.AdditionalFiles.MinimalOpenApiFile";
    private const string RootNamespaceKey = "build_property.RootNamespace";

    /// <summary>
    /// Registers the incremental generation pipeline with Roslyn.
    /// This is called once by the compiler host; do not call it directly.
    /// </summary>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. Collect OpenAPI additional files
        var openApiFiles = context.AdditionalTextsProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Where(pair =>
            {
                pair.Right.GetOptions(pair.Left).TryGetValue(MetadataKey, out var flag);
                return string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase);
            })
            .Select((pair, ct) =>
            {
                var content = pair.Left.GetText(ct)?.ToString() ?? string.Empty;
                var path = pair.Left.Path;
                return (Content: content, Path: path);
            });

        // 2. Get root namespace
        var rootNamespace = context.AnalyzerConfigOptionsProvider
            .Select((provider, _) =>
            {
                provider.GlobalOptions.TryGetValue(RootNamespaceKey, out var ns);
                return ns ?? "MinimalOpenAPI.Generated";
            });

        // 3. Parse OpenAPI documents — parser is selected from the file extension
        var parsedDocuments = openApiFiles
            .Combine(rootNamespace)
            .Select((pair, _) =>
            {
                var ((content, path), ns) = pair;

                var parser = SelectParser(path);
                if (parser is null)
                {
                    var ext = System.IO.Path.GetExtension(path);
                    return (Doc: (OpenApiDocument?)null, Path: path, Namespace: ns,
                            Error: (string?)null, UnsupportedExtension: ext);
                }

                try
                {
                    var doc = parser.ParseAsync(content).GetAwaiter().GetResult();
                    return (Doc: (OpenApiDocument?)doc, Path: path, Namespace: ns,
                            Error: (string?)null, UnsupportedExtension: (string?)null);
                }
                catch (Exception ex)
                {
                    return (Doc: (OpenApiDocument?)null, Path: path, Namespace: ns,
                            Error: ex.Message, UnsupportedExtension: (string?)null);
                }
            });

        // 4. Discover concrete class declarations in user code
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) =>
                    node is ClassDeclarationSyntax cls &&
                    cls.BaseList?.Types.Count > 0 &&
                    !cls.Modifiers.Any(m => m.ValueText == "abstract"),
                transform: static (ctx, ct) =>
                {
                    var cls = (ClassDeclarationSyntax)ctx.Node;
                    var symbol = ctx.SemanticModel.GetDeclaredSymbol(cls, ct) as INamedTypeSymbol;
                    if (symbol is null || symbol.IsAbstract) return null;

                    var baseTypes = new List<string>();
                    var baseType = symbol.BaseType;
                    while (baseType is not null)
                    {
                        baseTypes.Add(baseType.Name);
                        baseType = baseType.BaseType;
                    }

                    return new ClassInfo(
                        FullName: symbol.ToDisplayString(),
                        Name: symbol.Name,
                        BaseTypeNames: baseTypes);
                })
            .Where(c => c is not null)
            .Select((c, _) => c!);

        // 5. Combine parsed docs with discovered classes
        var combined = parsedDocuments.Combine(classDeclarations.Collect());

        // 6. Generate source files
        context.RegisterSourceOutput(combined, (spc, pair) =>
        {
            var ((doc, path, ns, error, unsupportedExtension), classes) = pair;

            if (unsupportedExtension is not null)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.UnsupportedFileExtension,
                    CreateOpenApiLocation(path),
                    unsupportedExtension, path));
                return;
            }

            if (error is not null)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ParseError,
                    CreateOpenApiLocation(path),
                    path, error));
                return;
            }

            if (doc is null) return;

            // Warn when the OpenAPI version is absent or not yet explicitly supported.
            if (doc.OpenApiVersion == OpenApiVersion.Unknown)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.UnknownOpenApiVersion,
                    CreateOpenApiLocation(path),
                    path));
            }

            GenerateForDocument(spc, doc, ns, path, classes.ToArray());
        });
    }

    /// <summary>
    /// Returns the appropriate parser for the given file path based on its extension,
    /// or <see langword="null"/> if no parser supports that extension.
    /// </summary>
    private static IOpenApiParser? SelectParser(string path)
    {
        var ext = System.IO.Path.GetExtension(path);
        return ext.ToLowerInvariant() switch
        {
            ".yaml" or ".yml" => new YamlOpenApiParser(),
            ".json" => new JsonOpenApiParser(),
            _ => null
        };
    }

    /// <summary>
    /// Creates a <see cref="Location"/> that points to the start of the given OpenAPI
    /// spec file.  Using a real file location (rather than <see cref="Location.None"/>)
    /// ensures that diagnostics are visible in IDE Error Lists, shown with the correct
    /// filename, and survive the Roslyn incremental-generator analysis cache.
    /// </summary>
    private static Location CreateOpenApiLocation(string filePath)
        => Location.Create(
            filePath,
            textSpan: TextSpan.FromBounds(0, 0),
            lineSpan: new LinePositionSpan(new LinePosition(0, 0), new LinePosition(0, 0)));

    private static void GenerateForDocument(
        SourceProductionContext spc,
        OpenApiDocument doc,
        string rootNamespace,
        string openApiFilePath,
        ClassInfo[] allClasses)
    {
        // Generate DTOs
        if (doc.Schemas.Count > 0)
        {
            var dtoSource = DtoGenerator.Generate(doc.Schemas, rootNamespace);
            if (!string.IsNullOrWhiteSpace(dtoSource))
                spc.AddSource("MinimalOpenApi.Dtos.g.cs", dtoSource);
        }

        // Discover handlers and customizers, emit diagnostics
        var handlers = new List<DiscoveredImplementation>();
        var customizers = new List<DiscoveredImplementation>();

        foreach (var op in doc.Operations)
        {
            var handlerBase = TypeMapper.HandlerClassName(op.OperationId);
            var customizerBase = TypeMapper.RegistrationClassName(op.OperationId);

            // Generate handler base
            var handlerSource = HandlerBaseGenerator.Generate(op, rootNamespace);
            spc.AddSource($"MinimalOpenApi.{handlerBase}.g.cs", handlerSource);

            // Generate registration customizer base
            var customizerSource = RegistrationCustomizerGenerator.Generate(op, rootNamespace);
            spc.AddSource($"MinimalOpenApi.{customizerBase}.g.cs", customizerSource);

            // Discover handler implementations
            var handlerImpls = allClasses
                .Where(c => c.BaseTypeNames.Contains(handlerBase))
                .ToList();

            switch (handlerImpls.Count)
            {
                case 0:
                    spc.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.MissingHandlerImplementation,
                        CreateOpenApiLocation(openApiFilePath),
                        $"{rootNamespace}.Endpoints.{handlerBase}"));
                    break;
                case 1:
                    handlers.Add(new DiscoveredImplementation
                    {
                        BaseName = handlerBase,
                        FullName = handlerImpls[0].FullName
                    });
                    break;
                default:
                    spc.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.DuplicateHandlerImplementation,
                        CreateOpenApiLocation(openApiFilePath),
                        handlerBase,
                        string.Join(", ", handlerImpls.Select(h => h.FullName))));
                    break;
            }

            // Discover customizer implementations
            var customizerImpls = allClasses
                .Where(c => c.BaseTypeNames.Contains(customizerBase))
                .ToList();

            switch (customizerImpls.Count)
            {
                case 0:
                    break; // Optional, none is fine
                case 1:
                    customizers.Add(new DiscoveredImplementation
                    {
                        BaseName = customizerBase,
                        FullName = customizerImpls[0].FullName
                    });
                    break;
                default:
                    spc.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.DuplicateRegistrationCustomizerImplementation,
                        CreateOpenApiLocation(openApiFilePath),
                        customizerBase,
                        string.Join(", ", customizerImpls.Select(c => c.FullName))));
                    break;
            }
        }

        // Generate DI registration
        var diSource = DependencyInjectionRegistrationGenerator.Generate(doc.Operations, handlers, customizers, rootNamespace);
        spc.AddSource("MinimalOpenApi.DependencyInjection.g.cs", diSource);

        // Generate endpoint mapping
        var mappingSource = EndpointMappingGenerator.Generate(doc.Operations, customizers, rootNamespace);
        spc.AddSource("MinimalOpenApi.EndpointMapping.g.cs", mappingSource);
    }
}

/// <summary>Information about a class discovered via syntax analysis.</summary>
internal sealed record ClassInfo(
    string FullName,
    string Name,
    List<string> BaseTypeNames);