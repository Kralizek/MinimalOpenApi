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
    private static readonly string RootNamespaceKey = "build_property.RootNamespace";

    public static (GeneratorDriverRunResult Result, Compilation OutputCompilation) RunGenerator(
        string userSource,
        IEnumerable<(string FileName, string Content)> additionalFiles,
        string rootNamespace = "TestProject")
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
            RootNamespaceKey);

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