using Microsoft.CodeAnalysis;

namespace MinimalOpenAPI.Generator.Tests;

/// <summary>
/// Documents, their handlers and their contracts may be split across assemblies: a library may leave
/// handlers and endpoint configurations to projects referencing it, and an <c>&lt;OpenApi&gt;</c> item
/// may generate only <c>Contracts</c> or only <c>Endpoints</c>.
/// </summary>
[TestFixture]
public class CrossAssemblyImplementationTests
{
    private const string Contract = """
        openapi: "3.0.3"
        info: { title: Shop, version: "1" }
        paths:
          /items/{id}:
            get:
              operationId: getItem
              parameters:
                - { name: id, in: path, required: true, schema: { type: integer } }
              responses:
                "200":
                  description: The item.
                  content:
                    application/json:
                      schema: { $ref: "#/components/schemas/Item" }
        components:
          schemas:
            Item:
              type: object
              required: [id]
              properties:
                id: { type: integer }
        """;

    private static readonly MetadataReference Runtime =
        MetadataReference.CreateFromFile(typeof(MinimalOpenAPI.MinimalOpenApiDocumentAttribute).Assembly.Location);

    private static Dictionary<string, string> Library => new() { ["OutputType"] = "Library" };

    private static Dictionary<string, string> Application => new() { ["OutputType"] = "Exe" };

    private static string Handler(string ns, string baseType, string contractsNamespace) => $$"""
        namespace {{ns}}
        {
            public sealed class GetItemHandler : {{baseType}}
            {
                public override global::System.Threading.Tasks.Task<global::Microsoft.AspNetCore.Http.HttpResults.Ok<global::{{contractsNamespace}}.Item>> HandleAsync(
                    int id, global::System.Threading.CancellationToken cancellationToken)
                    => global::System.Threading.Tasks.Task.FromResult(
                        global::Microsoft.AspNetCore.Http.TypedResults.Ok(new global::{{contractsNamespace}}.Item { Id = id }));
            }
        }

        """;

    private static string Configuration(string ns, string baseType, string className = "GetItemConfiguration") => $$"""
        namespace {{ns}}
        {
            public sealed class {{className}} : {{baseType}}
            {
                public override void Configure(global::Microsoft.AspNetCore.Builder.RouteHandlerBuilder endpoint) { }
            }
        }

        """;

    private const string LibraryBase = "global::Lib.Shop.Endpoints.GetItemEndpointBase";
    private const string LibraryConfigurationBase = "global::Lib.Shop.Endpoints.GetItemEndpointConfigurationBase";
    private const string LibraryContracts = "Lib.Shop.Contracts";

    private sealed record Built(GeneratorDriverRunResult Result, MetadataReference Reference, Compilation Output);

    /// <summary>Runs the generator for one project, compiles the result and returns it as a reference.</summary>
    private static Built Build(
        string assemblyName,
        string userSource = "",
        bool withDocument = true,
        Dictionary<string, string>? properties = null,
        Dictionary<string, string>? metadata = null,
        MetadataReference[]? references = null)
    {
        var (result, output) = GeneratorTestHelper.RunGenerator(
            userSource,
            withDocument ? [("openapi.yaml", Contract)] : [],
            rootNamespace: assemblyName,
            specNameOverride: withDocument ? "Shop" : null,
            additionalReferences: [Runtime, .. references ?? []],
            globalProperties: properties ?? Library,
            itemMetadata: metadata);

        var compilation = GeneratorTestHelper.WithFrameworkReferences(output).AddReferences(references ?? []);
        using var stream = new MemoryStream();
        var emit = compilation.Emit(stream);
        Assert.That(emit.Success, Is.True, string.Join(Environment.NewLine, emit.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)));
        return new Built(result, MetadataReference.CreateFromImage(stream.ToArray()), output);
    }

    private static List<Diagnostic> Diagnostics(GeneratorDriverRunResult result, string id)
        => result.Diagnostics.Where(d => d.Id == id).ToList();

    private static string? Source(GeneratorDriverRunResult result, string fileName)
        => result.GeneratedTrees.FirstOrDefault(t => t.FilePath.EndsWith(fileName, StringComparison.Ordinal))?.ToString();

    // ── Missing handlers ─────────────────────────────────────────────────

    [Test]
    public void Library_WithoutHandler_DoesNotReportMoa001_AndRecordsTheUnimplementedHandler()
    {
        var library = Build("Lib");

        Assert.That(Diagnostics(library.Result, "MOA001"), Is.Empty);
        Assert.That(Source(library.Result, "Shop.EntryPoint.g.cs"), Does.Contain(
            "UnimplementedHandlers = new global::System.Type[] { typeof(global::Lib.Shop.Endpoints.GetItemEndpointBase) }"));
    }

    [Test]
    public void Application_WithoutHandler_StillReportsMoa001ForItsOwnDocument()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            "", [("openapi.yaml", Contract)], rootNamespace: "App", specNameOverride: "Shop", globalProperties: Application);

        Assert.That(Diagnostics(result, "MOA001").Single().GetMessage(),
            Does.StartWith("No concrete implementation of 'App.Shop.Endpoints.GetItemEndpointBase' was found. "));
    }

    [Test]
    public void Application_ReferencingLibraryWithoutHandler_ReportsMoa001()
    {
        var library = Build("Lib");

        var host = Build("Host", withDocument: false, properties: Application, references: [library.Reference]);

        var diagnostic = Diagnostics(host.Result, "MOA001").Single();
        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Warning));
            Assert.That(diagnostic.GetMessage(), Does.Contain(
                "No concrete implementation of 'Lib.Shop.Endpoints.GetItemEndpointBase' was found in referenced assembly 'Lib' or in any project referencing it."));
        });
    }

    [Test]
    public void Application_ReferencingLibraryWithHandler_DoesNotReportMoa001()
    {
        var library = Build("Lib", Handler("Lib.Handlers", LibraryBase, LibraryContracts));

        var host = Build("Host", withDocument: false, properties: Application, references: [library.Reference]);

        Assert.That(Diagnostics(host.Result, "MOA001"), Is.Empty);
    }

    [TestCase("Library", null, TestName = "Library referencing an incomplete library")]
    [TestCase("Exe", "true", TestName = "Test project referencing an incomplete library")]
    [TestCase(null, null, TestName = "Unknown project kind referencing an incomplete library")]
    public void NonApplication_ReferencingLibraryWithoutHandler_DoesNotReportMoa001(string? outputType, string? isTestProject)
    {
        var library = Build("Lib");
        var properties = new Dictionary<string, string>();
        if (outputType is not null)
            properties["OutputType"] = outputType;
        if (isTestProject is not null)
            properties["IsTestProject"] = isTestProject;

        var consumer = Build("Consumer", withDocument: false, properties: properties, references: [library.Reference]);

        Assert.That(Diagnostics(consumer.Result, "MOA001"), Is.Empty);
    }

    // ── Implementations in referencing projects ──────────────────────────

    [Test]
    public void GeneratedBases_CarryMarkerAttributes_AndMappingLooksUpConfigurationAtStartup()
    {
        var library = Build("Lib");

        Assert.Multiple(() =>
        {
            Assert.That(Source(library.Result, "Shop.GetItemEndpointBase.g.cs"), Does.Contain("[global::MinimalOpenAPI.MinimalOpenApiHandlerBase]"));
            Assert.That(Source(library.Result, "Shop.GetItemEndpointConfigurationBase.g.cs"),
                Does.Contain("[global::MinimalOpenAPI.MinimalOpenApiEndpointConfigurationBase]"));
            Assert.That(Source(library.Result, "Shop.EndpointMapping.g.cs"),
                Does.Contain("services.GetService<global::Lib.Shop.Endpoints.GetItemEndpointConfigurationBase>()"));
            Assert.That(Source(library.Result, "Shop.DependencyInjection.g.cs"),
                Does.Contain("TryAddScoped<global::Lib.Shop.Endpoints.GetItemEndpointBase>(services)"));
        });
    }

    [Test]
    public void Application_ImplementingReferencedBases_RegistersThemAndReportsNothing()
    {
        var library = Build("Lib");

        var host = Build(
            "Host",
            Handler("Host", LibraryBase, LibraryContracts) + Configuration("Host.Config", LibraryConfigurationBase),
            withDocument: false,
            properties: Application,
            references: [library.Reference]);

        var source = Source(host.Result, "MinimalOpenApi.Implementations.g.cs");
        Assert.Multiple(() =>
        {
            Assert.That(host.Result.Diagnostics.Where(d => d.Id.StartsWith("MOA", StringComparison.Ordinal)), Is.Empty);
            Assert.That(source, Does.Contain(
                "[assembly: global::MinimalOpenAPI.MinimalOpenApiImplementations(typeof(global::Host.MinimalOpenApiImplementations), " +
                "Handlers = new global::System.Type[] { typeof(global::Lib.Shop.Endpoints.GetItemEndpointBase) }, " +
                "Configurations = new global::System.Type[] { typeof(global::Lib.Shop.Endpoints.GetItemEndpointConfigurationBase) })]"));
            Assert.That(source, Does.Contain("public static class MinimalOpenApiImplementations"));
            Assert.That(source, Does.Contain("global::Lib.Shop.ShopMinimalOpenApi.Register();"),
                "the documents are registered first, so their default registrations are replaced");
            Assert.That(source, Does.Contain("RemoveAll<global::Lib.Shop.Endpoints.GetItemEndpointBase>(services);"));
            Assert.That(source, Does.Contain("AddScoped<global::Lib.Shop.Endpoints.GetItemEndpointBase, global::Host.GetItemHandler>(services);"));
            Assert.That(source, Does.Contain(
                "AddSingleton<global::Lib.Shop.Endpoints.GetItemEndpointConfigurationBase, global::Host.Config.GetItemConfiguration>(services);"));
            Assert.That(source, Does.Contain("[global::System.Runtime.CompilerServices.ModuleInitializer]"));
        });
    }

    [Test]
    public void Application_ReferencingAnIntermediateImplementationLibrary_CallsItsEntryPoint()
    {
        var library = Build("Lib");
        var handlers = Build("Handlers", Handler("Handlers", LibraryBase, LibraryContracts), withDocument: false, references: [library.Reference]);

        var host = Build("Host", withDocument: false, properties: Application, references: [library.Reference, handlers.Reference]);

        var aggregator = Source(host.Result, "MinimalOpenApi.ReferencedDocuments.g.cs");
        Assert.Multiple(() =>
        {
            Assert.That(Diagnostics(host.Result, "MOA001"), Is.Empty);
            Assert.That(aggregator, Does.Contain("global::Handlers.MinimalOpenApiImplementations.Register();"));
            Assert.That(
                aggregator!.IndexOf("global::Lib.Shop.ShopMinimalOpenApi.Register();", StringComparison.Ordinal),
                Is.LessThan(aggregator.IndexOf("global::Handlers.MinimalOpenApiImplementations.Register();", StringComparison.Ordinal)));
        });
    }

    [Test]
    public void Project_ImplementingAHandlerTheLibraryAlreadyImplements_ReportsMoa002()
    {
        var library = Build("Lib", Handler("Lib.Handlers", LibraryBase, LibraryContracts));

        var (result, _) = GeneratorTestHelper.RunGenerator(
            Handler("Host", LibraryBase, LibraryContracts), [], rootNamespace: "Host",
            additionalReferences: [Runtime, library.Reference], globalProperties: Application);

        Assert.That(Diagnostics(result, "MOA002").Single().GetMessage(),
            Does.Contain("Host.GetItemHandler").And.Contain("assembly 'Lib'"));
    }

    [Test]
    public void Project_WithTwoImplementationsOfAReferencedHandler_ReportsMoa002()
    {
        var library = Build("Lib");
        var source = Handler("Host.A", LibraryBase, LibraryContracts) + Handler("Host.B", LibraryBase, LibraryContracts);

        var (result, _) = GeneratorTestHelper.RunGenerator(
            source, [], rootNamespace: "Host", additionalReferences: [Runtime, library.Reference], globalProperties: Application);

        Assert.That(Diagnostics(result, "MOA002").Single().GetMessage(),
            Does.Contain("Host.A.GetItemHandler").And.Contain("Host.B.GetItemHandler"));
    }

    [Test]
    public void Application_WhereTwoReferencedProjectsImplementTheSameHandler_ReportsMoa002()
    {
        var library = Build("Lib");
        var first = Build("First", Handler("First", LibraryBase, LibraryContracts), withDocument: false, references: [library.Reference]);
        var second = Build("Second", Handler("Second", LibraryBase, LibraryContracts), withDocument: false, references: [library.Reference]);

        var host = Build("Host", withDocument: false, properties: Application, references: [library.Reference, first.Reference, second.Reference]);

        Assert.That(Diagnostics(host.Result, "MOA002").Single().GetMessage(),
            Does.Contain("assembly 'First'").And.Contain("assembly 'Second'"));
    }

    [Test]
    public void Project_ConfiguringAnEndpointTheLibraryAlreadyConfigures_ReportsMoa003()
    {
        var library = Build("Lib", Configuration("Lib.Config", LibraryConfigurationBase));

        var (result, _) = GeneratorTestHelper.RunGenerator(
            Configuration("Host", LibraryConfigurationBase), [], rootNamespace: "Host",
            additionalReferences: [Runtime, library.Reference], globalProperties: Application);

        Assert.That(Diagnostics(result, "MOA003").Single().GetMessage(),
            Does.Contain("Host.GetItemConfiguration").And.Contain("assembly 'Lib'"));
    }

    // ── Generate="Contracts" / "Endpoints" ───────────────────────────────

    private static Dictionary<string, string> Generate(string value) => new() { ["MinimalOpenApiGenerate"] = value };

    [Test]
    public void ContractsOnly_GeneratesDtosAndTheContractsMarker_ButNoEndpoints()
    {
        var contracts = Build("Contracts", metadata: Generate("Contracts"));

        Assert.Multiple(() =>
        {
            Assert.That(Source(contracts.Result, "Shop.Contracts.g.cs"),
                Does.Contain("[assembly: global::MinimalOpenAPI.MinimalOpenApiContracts(\"Shop\", \"Contracts.Shop.Contracts\", \""));
            Assert.That(contracts.Result.GeneratedTrees.Any(t => t.ToString().Contains("public partial record Item", StringComparison.Ordinal)
                || t.ToString().Contains("record Item", StringComparison.Ordinal)), Is.True);
            Assert.That(Source(contracts.Result, "Shop.GetItemEndpointBase.g.cs"), Is.Null);
            Assert.That(Source(contracts.Result, "Shop.EntryPoint.g.cs"), Is.Null);
            Assert.That(Source(contracts.Result, "Shop.EndpointMapping.g.cs"), Is.Null);
        });
    }

    [Test]
    public void EndpointsOnly_UsesTheReferencedContracts_AndHandlersMayLiveInTheHost()
    {
        var contracts = Build("Contracts", metadata: Generate("Contracts"));
        var endpoints = Build("Endpoints", metadata: Generate("Endpoints"), references: [contracts.Reference]);

        Assert.Multiple(() =>
        {
            Assert.That(endpoints.Result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error), Is.Empty);
            Assert.That(Source(endpoints.Result, "Shop.Contracts.g.cs"), Is.Null, "no contracts marker without the Contracts layer");
            Assert.That(endpoints.Result.GeneratedTrees.Any(t => t.ToString().Contains("namespace Endpoints.Shop.Contracts", StringComparison.Ordinal)), Is.False);
            Assert.That(Source(endpoints.Result, "Shop.GetItemEndpointBase.g.cs"),
                Does.Contain("global::Contracts.Shop.Contracts.Item"));
        });

        var host = Build(
            "Host",
            Handler("Host", "global::Endpoints.Shop.Endpoints.GetItemEndpointBase", "Contracts.Shop.Contracts"),
            withDocument: false,
            properties: Application,
            references: [contracts.Reference, endpoints.Reference]);

        Assert.Multiple(() =>
        {
            Assert.That(host.Result.Diagnostics.Where(d => d.Id.StartsWith("MOA", StringComparison.Ordinal)), Is.Empty);
            Assert.That(Source(host.Result, "MinimalOpenApi.Implementations.g.cs"),
                Does.Contain("AddScoped<global::Endpoints.Shop.Endpoints.GetItemEndpointBase, global::Host.GetItemHandler>(services);"));
        });
    }

    [Test]
    public void EndpointsOnly_WithoutReferencedContracts_ReportsMoa020()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            "", [("openapi.yaml", Contract)], rootNamespace: "Endpoints", specNameOverride: "Shop",
            additionalReferences: [Runtime], globalProperties: Library, itemMetadata: Generate("Endpoints"));

        Assert.Multiple(() =>
        {
            Assert.That(Diagnostics(result, "MOA020").Single().GetMessage(), Does.Contain("no referenced assembly generates its Contracts"));
            Assert.That(Source(result, "Shop.GetItemEndpointBase.g.cs"), Is.Null);
        });
    }

    [Test]
    public void EndpointsOnly_WithContractsFromADifferentDocument_ReportsMoa021()
    {
        var contracts = Build("Contracts", metadata: Generate("Contracts"));
        var changed = Contract.Replace("title: Shop", "title: Shop v2", StringComparison.Ordinal);

        var (result, _) = GeneratorTestHelper.RunGenerator(
            "", [("openapi.yaml", changed)], rootNamespace: "Endpoints", specNameOverride: "Shop",
            additionalReferences: [Runtime, contracts.Reference], globalProperties: Library, itemMetadata: Generate("Endpoints"));

        Assert.That(Diagnostics(result, "MOA021").Single().GetMessage(), Does.Contain("referenced assembly 'Contracts'"));
    }

    [TestCase("Contracts|Endpoints")]
    [TestCase("endpoints, contracts")]
    public void BothLayers_AreTheDefault(string value)
    {
        var both = Build("Lib", metadata: Generate(value));

        Assert.Multiple(() =>
        {
            Assert.That(Source(both.Result, "Shop.Contracts.g.cs"), Is.Not.Null);
            Assert.That(Source(both.Result, "Shop.EntryPoint.g.cs"), Is.Not.Null);
        });
    }

    [Test]
    public void InvalidGenerateValue_ReportsMoa022()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            "", [("openapi.yaml", Contract)], rootNamespace: "Lib", specNameOverride: "Shop", itemMetadata: Generate("Contracts|Clients"));

        Assert.That(Diagnostics(result, "MOA022").Single().GetMessage(), Does.Contain("Generate='Contracts;Clients'"));
    }
}