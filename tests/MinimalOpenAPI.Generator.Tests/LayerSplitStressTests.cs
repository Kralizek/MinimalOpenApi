using Microsoft.CodeAnalysis;

namespace MinimalOpenAPI.Generator.Tests;

/// <summary>
/// Splitting a document into a <c>Generate="Contracts"</c> project and a <c>Generate="Endpoints"</c>
/// project must produce exactly the code a single project produces, except for the contracts
/// namespace, and both halves must compile against each other. Exercised over every OpenAPI document
/// in the repository plus a document covering the schema shapes that explicit CLR type reuse (#111)
/// must resolve consistently in every generator.
/// </summary>
[TestFixture]
public class LayerSplitStressTests
{
    private const string SpecName = "Spec";
    private const string CombinedRoot = "Api";
    private const string ContractsRoot = "Dto";
    private const string EndpointsRoot = "Api";

    private static readonly MetadataReference Runtime =
        MetadataReference.CreateFromFile(typeof(MinimalOpenAPI.MinimalOpenApiDocumentAttribute).Assembly.Location);

    private static readonly Dictionary<string, string> Library = new() { ["OutputType"] = "Library" };

    /// <summary>Every schema shape that a CLR type mapping (#111) must be honoured for.</summary>
    private const string ShapesDocument = """
        openapi: "3.0.3"
        info: { title: Shapes, version: "1" }
        paths:
          /contexts/{gameContext}/customers:
            post:
              operationId: createCustomer
              parameters:
                - { name: gameContext, in: path, required: true, schema: { $ref: "#/components/schemas/GameContextId" } }
                - { name: region, in: query, required: false, schema: { $ref: "#/components/schemas/Region" } }
                - { name: X-Tenant, in: header, required: true, schema: { $ref: "#/components/schemas/TenantId" } }
                - $ref: "#/components/parameters/PageSize"
              requestBody:
                required: true
                content:
                  application/json:
                    schema: { $ref: "#/components/schemas/Customer" }
              responses:
                "201":
                  description: Created.
                  content:
                    application/json:
                      schema: { $ref: "#/components/schemas/Customer" }
                "400":
                  $ref: "#/components/responses/Problem"
                "409":
                  description: Conflict.
                  content:
                    application/problem+json:
                      schema: { $ref: "#/components/schemas/ConflictProblem" }
          /contexts/{gameContext}/customers/search:
            get:
              operationId: searchCustomers
              parameters:
                - { name: gameContext, in: path, required: true, schema: { $ref: "#/components/schemas/GameContextId" } }
                - { name: tags, in: query, schema: { type: array, items: { $ref: "#/components/schemas/Tag" } } }
              responses:
                "200":
                  description: Customers.
                  content:
                    application/json:
                      schema: { $ref: "#/components/schemas/CustomerList" }
          /lookups:
            get:
              operationId: getLookups
              responses:
                "200":
                  description: Lookups.
                  content:
                    application/json:
                      schema: { $ref: "#/components/schemas/AddressBook" }
          /addresses:
            put:
              operationId: replaceAddresses
              requestBody:
                required: true
                content:
                  application/json:
                    schema: { type: array, items: { $ref: "#/components/schemas/Address" } }
              responses:
                "200":
                  description: Inline object wrapping mapped types.
                  content:
                    application/json:
                      schema:
                        type: object
                        required: [items]
                        properties:
                          items: { type: array, items: { $ref: "#/components/schemas/Address" } }
                          primary: { $ref: "#/components/schemas/Address" }
          /customers/{id}/avatar:
            post:
              operationId: uploadAvatar
              parameters:
                - { name: id, in: path, required: true, schema: { type: integer, format: int64 } }
              requestBody:
                required: true
                content:
                  multipart/form-data:
                    schema:
                      type: object
                      required: [file, owner]
                      properties:
                        file: { type: string, format: binary }
                        owner: { $ref: "#/components/schemas/Address" }
                        context: { $ref: "#/components/schemas/GameContextId" }
              responses:
                "204": { description: Uploaded. }
            get:
              operationId: getAvatar
              parameters:
                - { name: id, in: path, required: true, schema: { type: integer, format: int64 } }
              responses:
                "200":
                  description: Image.
                  content:
                    image/png:
                      schema: { type: string, format: binary }
        components:
          parameters:
            PageSize:
              name: pageSize
              in: query
              schema: { $ref: "#/components/schemas/PageSize" }
          responses:
            Problem:
              description: Validation problem.
              content:
                application/problem+json:
                  schema: { $ref: "#/components/schemas/ValidationProblem" }
          schemas:
            GameContextId: { type: string, example: retail }
            TenantId: { type: string, format: uuid }
            PageSize: { type: integer, format: int32, minimum: 1, maximum: 100 }
            Region: { type: string, enum: [eu, us, apac] }
            Tag: { type: string, minLength: 1, maxLength: 20 }
            Address:
              type: object
              required: [street, city]
              properties:
                street: { type: string }
                city: { type: string }
            NullableAddress:
              allOf: [{ $ref: "#/components/schemas/Address" }]
              nullable: true
            Auditable:
              type: object
              properties:
                createdAt: { type: string, format: date-time, readOnly: true }
                password: { type: string, writeOnly: true }
            Customer:
              allOf:
                - $ref: "#/components/schemas/Auditable"
                - type: object
                  required: [name, address, region]
                  properties:
                    id: { type: integer, format: int64, readOnly: true }
                    name: { type: string }
                    address: { $ref: "#/components/schemas/Address" }
                    previous: { $ref: "#/components/schemas/NullableAddress" }
                    region: { $ref: "#/components/schemas/Region" }
                    tags: { type: array, items: { $ref: "#/components/schemas/Tag" } }
                    context: { $ref: "#/components/schemas/GameContextId" }
                    labels: { type: object, additionalProperties: { $ref: "#/components/schemas/Address" } }
                    shipping:
                      type: object
                      properties:
                        to: { $ref: "#/components/schemas/Address" }
                        notes: { type: string }
            CustomerList:
              type: array
              items: { $ref: "#/components/schemas/Customer" }
            AddressBook:
              type: object
              additionalProperties: { $ref: "#/components/schemas/Address" }
            ValidationProblem:
              type: object
              properties:
                title: { type: string }
                errors: { type: object, additionalProperties: { type: array, items: { type: string } } }
            ConflictProblem:
              type: object
              required: [existing]
              properties:
                existing: { $ref: "#/components/schemas/Customer" }
        """;

    private static IEnumerable<TestCaseData> Documents()
    {
        var root = RepositoryRoot();
        var files = Directory.EnumerateFiles(Path.Combine(root, "sample"), "*.*", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(root, "tests", "MinimalOpenAPI.IntegrationTests"), "*.*", SearchOption.TopDirectoryOnly))
            .Where(f => f.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .OrderBy(f => f, StringComparer.Ordinal)
            .Select(f => (Name: Path.GetRelativePath(root, f).Replace('\\', '/'), Content: File.ReadAllText(f)))
            .Append(("shapes (#111)", ShapesDocument));

        foreach (var (name, content) in files)
        {
            foreach (var handling in new[] { "Auto", "Split", "Ignore" })
                yield return new TestCaseData(name, content, handling).SetName($"{name} [{handling}]");
        }
    }

    [TestCaseSource(nameof(Documents))]
    public void SplitLayers_MatchSingleProjectGeneration_AndCompileAlike(string name, string content, string readWriteSchemaHandling)
    {
        var fileName = name.Contains('/', StringComparison.Ordinal) ? Path.GetFileName(name) : "openapi.yaml";
        var file = (fileName, content);

        var combined = Run(CombinedRoot, file, readWriteSchemaHandling, generate: null);
        Assert.That(Errors(combined.Result), Is.Empty, "the document must generate as a single project");

        var contracts = Run(ContractsRoot, file, readWriteSchemaHandling, generate: "Contracts");
        var contractsCompilation = GeneratorTestHelper.WithFrameworkReferences(contracts.Output);
        var contractsReference = contractsCompilation.ToMetadataReference();

        var endpoints = Run(EndpointsRoot, file, readWriteSchemaHandling, generate: "Endpoints", contractsReference);
        Assert.That(Errors(endpoints.Result), Is.Empty);
        var endpointsCompilation = GeneratorTestHelper.WithFrameworkReferences(endpoints.Output).AddReferences(contractsReference);

        // The split compiles exactly when the single project compiles, with the same errors otherwise:
        // the layering introduces no error of its own.
        var combinedErrors = CompilerErrors(GeneratorTestHelper.WithFrameworkReferences(combined.Output))
            .Select(RenameContractsNamespace)
            .ToList();
        var splitErrors = CompilerErrors(contractsCompilation).Concat(CompilerErrors(endpointsCompilation)).ToList();
        Assert.That(splitErrors.Order(StringComparer.Ordinal), Is.EqualTo(combinedErrors.Order(StringComparer.Ordinal)));

        // Same files, same text: the single project's output with its contracts namespace renamed.
        var expected = Sources(combined.Result).ToDictionary(s => s.Key, s => RenameContractsNamespace(s.Value));
        var actual = Sources(contracts.Result).Concat(Sources(endpoints.Result)).ToDictionary(s => s.Key, s => s.Value);

        Assert.That(actual.Keys.Order(StringComparer.Ordinal), Is.EqualTo(expected.Keys.Order(StringComparer.Ordinal)),
            "the two layers together generate the same files as one project");
        Assert.Multiple(() =>
        {
            foreach (var (hint, text) in actual)
                Assert.That(text, Is.EqualTo(expected[hint]), hint);
        });
    }

    private sealed record Run_(GeneratorDriverRunResult Result, Compilation Output);

    private static Run_ Run(
        string rootNamespace,
        (string FileName, string Content) file,
        string readWriteSchemaHandling,
        string? generate,
        params MetadataReference[] references)
    {
        var metadata = new Dictionary<string, string>();
        if (generate is not null)
            metadata["MinimalOpenApiGenerate"] = generate;

        var (result, output) = GeneratorTestHelper.RunGenerator(
            "",
            [file],
            rootNamespace: rootNamespace,
            specNameOverride: SpecName,
            schemaId: "42",
            readWriteSchemaHandling: readWriteSchemaHandling,
            additionalReferences: [Runtime, .. references],
            globalProperties: Library,
            itemMetadata: metadata);
        return new Run_(result, output);
    }

    private static string RenameContractsNamespace(string text)
        => text.Replace($"{CombinedRoot}.{SpecName}.Contracts", $"{ContractsRoot}.{SpecName}.Contracts", StringComparison.Ordinal);

    private static IEnumerable<string> CompilerErrors(Compilation compilation)
        => compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => $"{Path.GetFileName(d.Location.SourceTree?.FilePath)}:{d.Location.GetLineSpan().StartLinePosition}: {d.Id} {d.GetMessage(System.Globalization.CultureInfo.InvariantCulture)}");

    private static List<Diagnostic> Errors(GeneratorDriverRunResult result)
        => result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

    private static IEnumerable<KeyValuePair<string, string>> Sources(GeneratorDriverRunResult result)
        => result.GeneratedTrees.Select(t => new KeyValuePair<string, string>(Path.GetFileName(t.FilePath), t.ToString()));

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MinimalOpenApi.slnx")))
            directory = directory.Parent;
        return directory!.FullName;
    }
}