using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using MinimalOpenAPI.Generator;

namespace MinimalOpenAPI.Generator.Tests;

/// <summary>
/// Helpers for running the source generator in tests.
/// </summary>
internal static class GeneratorTestHelper
{
    private static readonly string AdditionalFilesMetadataKey = "build_metadata.AdditionalFiles.MinimalOpenApiFile";
    private static readonly string NamespaceMetadataKey = "build_metadata.AdditionalFiles.MinimalOpenApiNamespace";
    private static readonly string SchemaIdMetadataKey = "build_metadata.AdditionalFiles.MinimalOpenApiSchemaId";
    private static readonly string PublishAsMetadataKey = "build_metadata.AdditionalFiles.MinimalOpenApiPublishAs";
    private static readonly string DisplayNameMetadataKey = "build_metadata.AdditionalFiles.MinimalOpenApiDisplayName";
    private static readonly string DisplayVersionMetadataKey = "build_metadata.AdditionalFiles.MinimalOpenApiDisplayVersion";
    private static readonly string RootNamespaceKey = "build_property.RootNamespace";

    /// <summary>
    /// Runs the generator with the given user source and additional files.
    /// </summary>
    /// <param name="userSource">C# source code representing user-written handler implementations.</param>
    /// <param name="additionalFiles">OpenAPI spec files to pass as AdditionalTexts.</param>
    /// <param name="rootNamespace">The root namespace to use (defaults to "TestProject").</param>
    /// <param name="specNameOverride">
    /// When non-null, simulates the <c>Namespace</c> MSBuild metadata attribute on all provided
    /// OpenAPI files, so the generator uses this value as the spec name instead of deriving it
    /// from the file name.
    /// </param>
    /// <param name="schemaId">
    /// When non-null, simulates the <c>MinimalOpenApiSchemaId</c> MSBuild metadata on all provided
    /// OpenAPI files (the stable hash of the source file's full path computed by the targets).
    /// </param>
    /// <param name="publishAs">Optional explicit HTTP path metadata.</param>
    /// <param name="displayName">Optional display name metadata.</param>
    /// <param name="displayVersion">Optional display version metadata.</param>
    public static (GeneratorDriverRunResult Result, Compilation OutputCompilation) RunGenerator(
        string userSource,
        IEnumerable<(string FileName, string Content)> additionalFiles,
        string rootNamespace = "TestProject",
        string? specNameOverride = null,
        string? schemaId = null,
        string? publishAs = null,
        string? displayName = null,
        string? displayVersion = null)
    {
        // Create a minimal compilation for the generator
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
        };

        // Add System.Runtime
        var systemRuntime = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "System.Runtime");
        if (systemRuntime is not null)
            references.Add(MetadataReference.CreateFromFile(systemRuntime.Location));

        var compilation = CSharpCompilation.Create(
            assemblyName: rootNamespace,
            syntaxTrees: [CSharpSyntaxTree.ParseText(userSource)],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new MinimalOpenApiGenerator();

        var additionalTexts = additionalFiles
            .Select(f => (AdditionalText)new TestAdditionalText(f.FileName, f.Content))
            .ToImmutableArray();

        var optionsProvider = new TestAnalyzerConfigOptionsProvider(
            additionalTexts.ToArray(),
            rootNamespace,
            AdditionalFilesMetadataKey,
            NamespaceMetadataKey,
            SchemaIdMetadataKey,
            PublishAsMetadataKey,
            DisplayNameMetadataKey,
            DisplayVersionMetadataKey,
            RootNamespaceKey,
            specNameOverride,
            schemaId,
            publishAs,
            displayName,
            displayVersion);

        var driver = CSharpGeneratorDriver
            .Create(generator)
            .AddAdditionalTexts(additionalTexts)
            .WithUpdatedAnalyzerConfigOptions(optionsProvider)
            .RunGeneratorsAndUpdateCompilation(
                compilation,
                out var outputCompilation,
                out _);

        return (driver.GetRunResult(), outputCompilation);
    }

    public static string GetGeneratedSource(GeneratorDriverRunResult result, string hintNameSuffix)
    {
        var source = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.EndsWith(hintNameSuffix, StringComparison.OrdinalIgnoreCase));

        Assert.That(source, Is.Not.Null, $"Generated file ending with '{hintNameSuffix}' should exist");
        return source!.GetText().ToString();
    }
}