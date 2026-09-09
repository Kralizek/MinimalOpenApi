using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using MinimalOpenAPI.Abstractions;
using MinimalOpenAPI.Abstractions.Models;
using MinimalOpenAPI.Parser.Json;
using MinimalOpenAPI.Parser.Yaml;

namespace MinimalOpenAPIClient.Generator;

[Generator]
public sealed class MinimalOpenApiClientGenerator : IIncrementalGenerator
{
    private const string ClientFileMetadataKey = "build_metadata.AdditionalFiles.MinimalOpenApiClientFile";
    private const string NamespaceMetadataKey = "build_metadata.AdditionalFiles.MinimalOpenApiClientNamespace";
    private const string RootNamespaceKey = "build_property.RootNamespace";

    private static readonly DiagnosticDescriptor ParseError = new(
        id: "MOAC001",
        title: "Unable to parse OpenAPI document",
        messageFormat: "Unable to parse OpenAPI document '{0}': {1}",
        category: "MinimalOpenAPIClient",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnsupportedFile = new(
        id: "MOAC002",
        title: "Unsupported OpenAPI document format",
        messageFormat: "OpenAPI client generation supports .json, .yaml and .yml files; '{0}' is not supported",
        category: "MinimalOpenAPIClient",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor GenerationError = new(
        id: "MOAC003",
        title: "Unable to generate OpenAPI client",
        messageFormat: "Unable to generate OpenAPI client for '{0}': {1}",
        category: "MinimalOpenAPIClient",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DuplicateNamespace = new(
        id: "MOAC004",
        title: "Duplicate generated client namespace",
        messageFormat: "Multiple OpenApiClient items generate namespace '{0}': {1}. Set Namespace metadata to make each client namespace unique.",
        category: "MinimalOpenAPIClient",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var rootNamespace = context.AnalyzerConfigOptionsProvider
            .Select((provider, _) =>
            {
                provider.GlobalOptions.TryGetValue(RootNamespaceKey, out var value);
                return string.IsNullOrWhiteSpace(value) ? "MinimalOpenAPIClient.Generated" : value!;
            });

        var files = context.AdditionalTextsProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Where(static pair =>
            {
                pair.Right.GetOptions(pair.Left).TryGetValue(ClientFileMetadataKey, out var enabled);
                return string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase);
            })
            .Select((pair, cancellationToken) =>
            {
                var options = pair.Right.GetOptions(pair.Left);
                options.TryGetValue(NamespaceMetadataKey, out var explicitNamespace);

                return new ClientFileInput(
                    pair.Left.Path,
                    pair.Left.GetText(cancellationToken)?.ToString() ?? string.Empty,
                    string.IsNullOrWhiteSpace(explicitNamespace) ? null : explicitNamespace);
            })
            .Collect()
            .Combine(rootNamespace);

        context.RegisterSourceOutput(files, static (productionContext, input) =>
        {
            var prepared = input.Left
                .Select(file =>
                {
                    var specName = ClientCodeGenerator.ToPascalIdentifier(Path.GetFileNameWithoutExtension(file.Path));
                    var targetNamespace = file.Namespace ?? $"{input.Right}.Clients.{specName}";
                    return new PreparedClientFile(file, specName, targetNamespace);
                })
                .ToArray();

            var duplicateNamespaces = prepared
                .GroupBy(static file => file.TargetNamespace, StringComparer.Ordinal)
                .Where(static group => group.Count() > 1)
                .ToDictionary(static group => group.Key, static group => group.ToArray(), StringComparer.Ordinal);

            foreach (var duplicate in duplicateNamespaces)
            {
                productionContext.ReportDiagnostic(Diagnostic.Create(
                    DuplicateNamespace,
                    Location.None,
                    duplicate.Key,
                    string.Join(", ", duplicate.Value.Select(static file => file.File.Path))));
            }

            foreach (var preparedFile in prepared)
            {
                if (duplicateNamespaces.ContainsKey(preparedFile.TargetNamespace))
                    continue;

                var file = preparedFile.File;
                try
                {
                    var parser = SelectParser(file.Path);
                    if (parser is null)
                    {
                        productionContext.ReportDiagnostic(Diagnostic.Create(
                            UnsupportedFile,
                            Location.None,
                            file.Path));
                        continue;
                    }

                    var document = parser.ParseAsync(file.Content, productionContext.CancellationToken)
                        .GetAwaiter()
                        .GetResult();

                    if (document.OpenApiVersion?.Major != 3)
                        throw new ClientGenerationException("An OpenAPI 3.x version is required.");

                    var namespaceSyntax = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseName(preparedFile.TargetNamespace);
                    if (namespaceSyntax.ContainsDiagnostics || namespaceSyntax.DescendantNodesAndSelf().Any(static node =>
                        node is not Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax and
                        not Microsoft.CodeAnalysis.CSharp.Syntax.QualifiedNameSyntax))
                        throw new ClientGenerationException($"Invalid client namespace '{preparedFile.TargetNamespace}'.");

                    var source = ClientCodeGenerator.Generate(
                        document,
                        preparedFile.SpecName,
                        preparedFile.TargetNamespace);

                    productionContext.AddSource(
                        CreateHintName(preparedFile.SpecName, file.Path),
                        SourceText.From(source, System.Text.Encoding.UTF8));
                }
                catch (OperationCanceledException) when (productionContext.CancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (ClientGenerationException ex)
                {
                    productionContext.ReportDiagnostic(Diagnostic.Create(
                        GenerationError,
                        Location.None,
                        file.Path,
                        ex.Message));
                }
                catch (Exception ex)
                {
                    productionContext.ReportDiagnostic(Diagnostic.Create(
                        ParseError,
                        Location.None,
                        file.Path,
                        ex.Message));
                }
            }
        });
    }

    private static string CreateHintName(string specName, string path)
    {
        const ulong offsetBasis = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;

        var hash = offsetBasis;
        foreach (var character in path)
            hash = unchecked((hash ^ character) * prime);

        return $"{specName}.{hash:x16}.Client.g.cs";
    }

    private static IOpenApiParser? SelectParser(string path)
    {
        var extension = Path.GetExtension(path);
        if (string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase))
            return new JsonOpenApiParser();

        if (string.Equals(extension, ".yaml", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".yml", StringComparison.OrdinalIgnoreCase))
            return new YamlOpenApiParser();

        return null;
    }

    private sealed record ClientFileInput(string Path, string Content, string? Namespace);

    private sealed record PreparedClientFile(ClientFileInput File, string SpecName, string TargetNamespace);
}