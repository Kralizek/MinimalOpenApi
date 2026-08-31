using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using MinimalOpenAPI.Abstractions;
using MinimalOpenAPI.Abstractions.Models;
using MinimalOpenAPI.Client.Generator;
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
            .Combine(rootNamespace);

        context.RegisterSourceOutput(files, static (productionContext, input) =>
        {
            var file = input.Left;
            var rootNamespace = input.Right;

            try
            {
                var parser = SelectParser(file.Path);
                if (parser is null)
                {
                    productionContext.ReportDiagnostic(Diagnostic.Create(
                        UnsupportedFile,
                        Location.None,
                        file.Path));
                    return;
                }

                var document = parser.ParseAsync(file.Content, productionContext.CancellationToken)
                    .GetAwaiter()
                    .GetResult();

                var specName = ClientCodeGenerator.ToPascalIdentifier(Path.GetFileNameWithoutExtension(file.Path));
                var targetNamespace = file.Namespace ?? $"{rootNamespace}.Clients.{specName}";
                var source = ClientCodeGenerator.Generate(document, specName, targetNamespace);

                productionContext.AddSource($"{specName}.Client.g.cs", SourceText.From(source, System.Text.Encoding.UTF8));
            }
            catch (Exception ex)
            {
                productionContext.ReportDiagnostic(Diagnostic.Create(
                    ParseError,
                    Location.None,
                    file.Path,
                    ex.Message));
            }
        });
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
}