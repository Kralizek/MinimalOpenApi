using Microsoft.CodeAnalysis;

namespace MinimalOpenAPI.Generator.Tests;

/// <summary>
/// Documents declared in referenced assemblies are registered by the consuming project through
/// generated entry points discovered at build time (#127).
/// </summary>
[TestFixture]
public class ClassLibraryRegistrationTests
{
    private static string Contract(string route, string operationId) => $$"""
        openapi: "3.0.3"
        info: { title: Library, version: "1" }
        paths:
          {{route}}:
            get:
              operationId: {{operationId}}
              responses:
                "204": { description: No content. }
        """;

    /// <summary>Generates and compiles a library assembly that declares one OpenAPI document.</summary>
    private static MetadataReference BuildLibrary(string assemblyName, string specName, string route, string operationId)
    {
        var (result, outputCompilation) = GeneratorTestHelper.RunGenerator(
            "",
            [("openapi.yaml", Contract(route, operationId))],
            rootNamespace: assemblyName,
            specNameOverride: specName,
            schemaId: "123");
        Assert.That(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error), Is.Empty);

        using var stream = new MemoryStream();
        var emit = GeneratorTestHelper.WithFrameworkReferences(outputCompilation).Emit(stream);
        Assert.That(emit.Success, Is.True, string.Join(Environment.NewLine, emit.Diagnostics));
        return MetadataReference.CreateFromImage(stream.ToArray());
    }

    // A consuming project always references the MinimalOpenAPI runtime, which declares the marker attribute.
    private static readonly MetadataReference Runtime =
        MetadataReference.CreateFromFile(typeof(MinimalOpenAPI.MinimalOpenApiDocumentAttribute).Assembly.Location);

    [Test]
    public void Document_GeneratesPublicIdempotentEntryPointAndAssemblyMarker()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            "", [("openapi.yaml", Contract("/ping", "ping"))], rootNamespace: "LibA", specNameOverride: "Catalog", schemaId: "123");

        var entryPoint = GeneratorTestHelper.GetGeneratedSource(result, "Catalog.EntryPoint.g.cs");

        Assert.Multiple(() =>
        {
            Assert.That(entryPoint, Does.Contain("[assembly: global::MinimalOpenAPI.MinimalOpenApiDocument(typeof(global::LibA.Catalog.CatalogMinimalOpenApi))]"));
            Assert.That(entryPoint, Does.Contain("namespace LibA.Catalog;"));
            Assert.That(entryPoint, Does.Contain("public static class CatalogMinimalOpenApi"));
            Assert.That(entryPoint, Does.Contain("public static void Register()"));
            Assert.That(entryPoint, Does.Contain("if (global::System.Threading.Interlocked.Exchange(ref _registered, 1) != 0)"));
            Assert.That(entryPoint, Does.Contain("RegisterSchemaFile(\"openapi/schemas/123/openapi.yaml\""));
        });
    }

    [Test]
    public void ReferencedLibraries_AreRegisteredFromTheConsumingProject()
    {
        var catalog = BuildLibrary("LibA", "Catalog", "/catalog", "getCatalog");
        var billing = BuildLibrary("LibB", "Billing", "/billing", "getBilling");

        // A host without any <OpenApi> item of its own.
        var (result, outputCompilation) = GeneratorTestHelper.RunGenerator(
            "", [], rootNamespace: "HostApp", additionalReferences: [Runtime, catalog, billing]);

        var aggregator = GeneratorTestHelper.GetGeneratedSource(result, "MinimalOpenApi.ReferencedDocuments.g.cs");
        Assert.Multiple(() =>
        {
            Assert.That(aggregator, Does.Contain("namespace HostApp;"));
            Assert.That(aggregator, Does.Contain("[global::System.Runtime.CompilerServices.ModuleInitializer]"));
            var catalogCall = aggregator.IndexOf("global::LibA.Catalog.CatalogMinimalOpenApi.Register();", StringComparison.Ordinal);
            var billingCall = aggregator.IndexOf("global::LibB.Billing.BillingMinimalOpenApi.Register();", StringComparison.Ordinal);
            Assert.That(catalogCall, Is.GreaterThan(0), "entry points are called directly");
            Assert.That(billingCall, Is.GreaterThan(catalogCall), "entry points are called in a deterministic order");
        });

        var errors = GeneratorTestHelper
            .WithFrameworkReferences(outputCompilation)
            .AddReferences(catalog, billing)
            .GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();
        Assert.That(errors, Is.Empty, string.Join(Environment.NewLine, errors));
    }

    [Test]
    public void ReferencedLibrary_IsRegisteredAlongsideTheHostsOwnDocument()
    {
        var catalog = BuildLibrary("LibA", "Catalog", "/catalog", "getCatalog");

        var (result, _) = GeneratorTestHelper.RunGenerator(
            "", [("openapi.yaml", Contract("/health", "getHealth"))], rootNamespace: "HostApp", specNameOverride: "Health",
            additionalReferences: [Runtime, catalog]);

        var aggregator = GeneratorTestHelper.GetGeneratedSource(result, "MinimalOpenApi.ReferencedDocuments.g.cs");
        Assert.Multiple(() =>
        {
            Assert.That(aggregator, Does.Contain("global::LibA.Catalog.CatalogMinimalOpenApi.Register();"));
            Assert.That(aggregator, Does.Not.Contain("HealthMinimalOpenApi"),
                "the host's own documents are registered by its per-document module initializer");
            Assert.That(GeneratorTestHelper.GetGeneratedSource(result, "Health.EntryPoint.g.cs"), Does.Contain("HealthMinimalOpenApi"));
        });
    }

    [Test]
    public void NoReferencedDocuments_GeneratesNoAggregator()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", Contract("/ping", "ping"))]);

        Assert.That(result.GeneratedTrees.Any(t => t.FilePath.EndsWith("ReferencedDocuments.g.cs", StringComparison.Ordinal)), Is.False);
    }
}