using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MinimalOpenAPI.Generator.CodeGen;
using MinimalOpenAPI.Generator.Diagnostics;
using MinimalOpenAPI.Generator.Models;
using MinimalOpenAPI.Generator.Parser;

namespace MinimalOpenAPI.Generator;

[Generator]
public sealed class MinimalOpenApiGenerator : IIncrementalGenerator
{
    private const string MetadataKey = "build_metadata.AdditionalFiles.MinimalOpenApiFile";
    private const string RootNamespaceKey = "build_property.RootNamespace";

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

        // 3. Parse OpenAPI documents
        var parsedDocuments = openApiFiles
            .Combine(rootNamespace)
            .Select((pair, _) =>
            {
                var ((content, path), ns) = pair;
                try
                {
                    IOpenApiParser parser = new YamlOpenApiParser();
                    var doc = parser.Parse(content);
                    return (Doc: (OpenApiDocument?)doc, Path: path, Namespace: ns, Error: (string?)null);
                }
                catch (Exception ex)
                {
                    return (Doc: null, Path: path, Namespace: ns, Error: ex.Message);
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
            var ((doc, path, ns, error), classes) = pair;

            if (error is not null)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ParseError,
                    Location.None,
                    path, error));
                return;
            }

            if (doc is null) return;

            GenerateForDocument(spc, doc, ns, classes.ToArray());
        });
    }

    private static void GenerateForDocument(
        SourceProductionContext spc,
        OpenApiDocument doc,
        string rootNamespace,
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
                        Location.None,
                        handlerBase));
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
                        Location.None,
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
                        Location.None,
                        customizerBase,
                        string.Join(", ", customizerImpls.Select(c => c.FullName))));
                    break;
            }
        }

        // Generate DI registration
        var diSource = DiRegistrationGenerator.Generate(doc.Operations, handlers, customizers, rootNamespace);
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
