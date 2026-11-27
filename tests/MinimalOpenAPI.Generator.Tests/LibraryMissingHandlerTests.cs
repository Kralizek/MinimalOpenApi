using Microsoft.CodeAnalysis;

namespace MinimalOpenAPI.Generator.Tests;

/// <summary>
/// A class library may leave handlers to the application that references it: the library does not
/// report MOA001, records the unimplemented handler bases on its assembly attribute, and the
/// referencing application reports MOA001 for them.
/// </summary>
[TestFixture]
public class LibraryMissingHandlerTests
{
    private const string Contract = """
        openapi: "3.0.3"
        info: { title: Library, version: "1" }
        paths:
          /ping:
            get:
              operationId: ping
              responses:
                "204": { description: No content. }
        """;

    private const string PingHandler = """
        namespace LibA.Handlers;

        public sealed class PingHandler : global::LibA.Catalog.Endpoints.PingEndpointBase
        {
            public override global::System.Threading.Tasks.Task<global::Microsoft.AspNetCore.Http.HttpResults.NoContent> HandleAsync(
                global::System.Threading.CancellationToken cancellationToken)
                => global::System.Threading.Tasks.Task.FromResult(global::Microsoft.AspNetCore.Http.TypedResults.NoContent());
        }
        """;

    private static readonly MetadataReference Runtime =
        MetadataReference.CreateFromFile(typeof(MinimalOpenAPI.MinimalOpenApiDocumentAttribute).Assembly.Location);

    private static Dictionary<string, string> Library => new() { ["OutputType"] = "Library" };

    private static Dictionary<string, string> Application => new() { ["OutputType"] = "Exe" };

    private static (GeneratorDriverRunResult Result, Compilation Output) RunLibrary(string userSource)
        => GeneratorTestHelper.RunGenerator(
            userSource, [("openapi.yaml", Contract)], rootNamespace: "LibA", specNameOverride: "Catalog",
            globalProperties: Library);

    private static MetadataReference Emit(Compilation outputCompilation)
    {
        using var stream = new MemoryStream();
        var emit = GeneratorTestHelper.WithFrameworkReferences(outputCompilation).Emit(stream);
        Assert.That(emit.Success, Is.True, string.Join(Environment.NewLine, emit.Diagnostics));
        return MetadataReference.CreateFromImage(stream.ToArray());
    }

    private static IEnumerable<Diagnostic> Moa001(GeneratorDriverRunResult result)
        => result.Diagnostics.Where(d => d.Id == "MOA001");

    [Test]
    public void Library_WithoutHandler_DoesNotReportMoa001_AndRecordsTheUnimplementedHandler()
    {
        var (result, _) = RunLibrary("");

        Assert.That(Moa001(result), Is.Empty);
        Assert.That(
            GeneratorTestHelper.GetGeneratedSource(result, "Catalog.EntryPoint.g.cs"),
            Does.Contain("[assembly: global::MinimalOpenAPI.MinimalOpenApiDocument(typeof(global::LibA.Catalog.CatalogMinimalOpenApi), " +
                         "UnimplementedHandlers = new global::System.Type[] { typeof(global::LibA.Catalog.Endpoints.PingEndpointBase) })]"));
    }

    [Test]
    public void Library_WithHandler_RecordsNoUnimplementedHandlers()
    {
        var (result, _) = RunLibrary(PingHandler);

        Assert.That(Moa001(result), Is.Empty);
        Assert.That(
            GeneratorTestHelper.GetGeneratedSource(result, "Catalog.EntryPoint.g.cs"),
            Does.Contain("[assembly: global::MinimalOpenAPI.MinimalOpenApiDocument(typeof(global::LibA.Catalog.CatalogMinimalOpenApi))]"));
    }

    [Test]
    public void Application_WithoutHandler_StillReportsMoa001ForItsOwnDocument()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            "", [("openapi.yaml", Contract)], rootNamespace: "App", specNameOverride: "Catalog", globalProperties: Application);

        var diagnostic = Moa001(result).Single();
        Assert.That(diagnostic.GetMessage(), Does.StartWith("No concrete implementation of 'App.Catalog.Endpoints.PingEndpointBase' was found. "));
        Assert.That(GeneratorTestHelper.GetGeneratedSource(result, "Catalog.EntryPoint.g.cs"), Does.Not.Contain("UnimplementedHandlers"));
    }

    [Test]
    public void Application_ReferencingLibraryWithoutHandler_ReportsMoa001()
    {
        var library = Emit(RunLibrary("").Output);

        var (result, _) = GeneratorTestHelper.RunGenerator(
            "", [], rootNamespace: "HostApp", additionalReferences: [Runtime, library], globalProperties: Application);

        var diagnostic = Moa001(result).Single();
        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Warning));
            Assert.That(diagnostic.GetMessage(), Does.Contain(
                "No concrete implementation of 'LibA.Catalog.Endpoints.PingEndpointBase' was found in referenced assembly 'LibA'."));
        });
    }

    [Test]
    public void Application_ReferencingLibraryWithHandler_DoesNotReportMoa001()
    {
        var library = Emit(RunLibrary(PingHandler).Output);

        var (result, _) = GeneratorTestHelper.RunGenerator(
            "", [], rootNamespace: "HostApp", additionalReferences: [Runtime, library], globalProperties: Application);

        Assert.That(Moa001(result), Is.Empty);
    }

    [TestCase("Library", null, TestName = "Library referencing an incomplete library")]
    [TestCase("Exe", "true", TestName = "Test project referencing an incomplete library")]
    [TestCase(null, null, TestName = "Unknown project kind referencing an incomplete library")]
    public void NonApplication_ReferencingLibraryWithoutHandler_DoesNotReportMoa001(string? outputType, string? isTestProject)
    {
        var library = Emit(RunLibrary("").Output);
        var properties = new Dictionary<string, string>();
        if (outputType is not null)
            properties["OutputType"] = outputType;
        if (isTestProject is not null)
            properties["IsTestProject"] = isTestProject;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            "", [], rootNamespace: "Consumer", additionalReferences: [Runtime, library], globalProperties: properties);

        Assert.That(Moa001(result), Is.Empty);
    }
}