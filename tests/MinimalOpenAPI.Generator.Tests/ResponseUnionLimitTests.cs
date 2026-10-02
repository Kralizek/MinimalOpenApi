using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MinimalOpenAPI.Generator.Tests;

[TestFixture]
public class ResponseUnionLimitTests
{
    private static readonly (int Status, string Type)[] KnownResponses =
    [
        (500, "InternalServerError"),
        (200, "Ok"),
        (404, "NotFound"),
        (201, "Created"),
        (202, "Accepted"),
        (400, "BadRequest"),
        (401, "UnauthorizedHttpResult"),
        (403, "ForbidHttpResult"),
        (409, "Conflict"),
        (422, "UnprocessableEntity"),
        (204, "NoContent")
    ];

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(11)]
    [TestCase(12)]
    [TestCase(23)]
    public void EffectiveResults_UseOrderedFlatOrNestedFrameworkUnions(int count)
    {
        var statuses = KnownResponses.Select(r => r.Status).Take(count)
            .Concat(Enumerable.Range(210, Math.Max(0, count - KnownResponses.Length)));
        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", Contract(statuses))]);

        Assert.That(result.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error), Is.False);
        var types = KnownResponses.Take(count).Select(r => HttpResult(r.Type))
            .Concat(Enumerable.Range(210, Math.Max(0, count - KnownResponses.Length)).Select(s => $"Status{s}Problem"))
            .ToArray();
        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetResponsesEndpointBase.g.cs");
        Assert.That(source, Does.Contain($"Task<{ExpectedUnion(types, 0)}> HandleAsync("));
    }

    [Test]
    public void SevenDeclaredResponses_WithDuplicateFallbackTypes_ProduceSixAlternatives()
    {
        var statuses = new[] { 500, 200, 201, 202, 400, 407, 406 };
        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", Contract(statuses, bodyStatuses: [407, 406, 500]))]);

        var types = new[] { HttpResult("InternalServerError<int>"), HttpResult("Ok"), HttpResult("Created"),
            HttpResult("Accepted"), HttpResult("BadRequest"), "global::Microsoft.AspNetCore.Http.IResult" };
        Assert.That(GeneratorTestHelper.GetGeneratedSource(result, "GetResponsesEndpointBase.g.cs"),
            Does.Contain($"Task<{ExpectedUnion(types, 0)}> HandleAsync("));
    }

    [Test]
    public void DuplicateFallbackType_KeepsFirstPositionAcrossNestedBoundary()
    {
        var statuses = new[] { 500, 407, 200, 201, 202, 400, 406, 404 };
        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", Contract(statuses, bodyStatuses: [407, 406]))]);

        var types = new[] { HttpResult("InternalServerError"), "global::Microsoft.AspNetCore.Http.IResult",
            HttpResult("Ok"), HttpResult("Created"), HttpResult("Accepted"), HttpResult("BadRequest"), HttpResult("NotFound") };
        Assert.That(GeneratorTestHelper.GetGeneratedSource(result, "GetResponsesEndpointBase.g.cs"),
            Does.Contain($"Task<{ExpectedUnion(types, 0)}> HandleAsync("));
    }

    [Test]
    public void FilteringRestoresFlatUnionInDocumentOrder_WithoutRemovingEndpointMetadata()
    {
        var statuses = KnownResponses.Take(7).Select(r => r.Status).ToArray();
        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", Contract(statuses))],
            globalExclusionsByFile: new Dictionary<string, string> { ["openapi.yaml"] = "500;401" },
            overrides: [(null, "getResponses", "500", null)]);

        var expectedTypes = KnownResponses.Take(6).Select(r => HttpResult(r.Type)).ToArray();
        Assert.That(GeneratorTestHelper.GetGeneratedSource(result, "GetResponsesEndpointBase.g.cs"),
            Does.Contain($"Task<{ExpectedUnion(expectedTypes, 0)}> HandleAsync("));
        var mapping = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");
        Assert.That(mapping, Does.Contain("Status401Unauthorized"));
        Assert.That(mapping, Does.Contain("Status500InternalServerError"));
    }

    [Test]
    public void OperationExclusion_ReducesNestedUnionToFlatWithoutReordering()
    {
        var statuses = KnownResponses.Take(7).Select(r => r.Status).ToArray();
        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", Contract(statuses))],
            overrides: [(null, "getResponses", null, "404")]);

        var expectedTypes = KnownResponses.Take(7).Where(r => r.Status != 404).Select(r => HttpResult(r.Type)).ToArray();
        Assert.That(GeneratorTestHelper.GetGeneratedSource(result, "GetResponsesEndpointBase.g.cs"),
            Does.Contain($"Task<{ExpectedUnion(expectedTypes, 0)}> HandleAsync("));
        Assert.That(GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs"),
            Does.Contain("Status404NotFound"));
    }

    [Test]
    public void NestedUnion_OuterAndExplicitInnerReturnsCompile()
    {
        var statuses = KnownResponses.Take(7).Select(r => r.Status).ToArray();
        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", Contract(statuses))]);
        var types = KnownResponses.Take(7).Select(r => HttpResult(r.Type)).ToArray();
        var outer = ExpectedUnion(types, 0);
        var inner = ExpectedUnion(types, 5);
        var handlerSource = GeneratorTestHelper.GetGeneratedSource(result, "GetResponsesEndpointBase.g.cs");
        Assert.That(handlerSource, Does.Contain($"Task<{outer}> HandleAsync("));

        var handler = $$"""
            public sealed class DirectEndpoint : TestProject.Openapi.Endpoints.GetResponsesEndpointBase
            {
                public override System.Threading.Tasks.Task<{{outer}}> HandleAsync(System.Threading.CancellationToken cancellationToken)
                    => System.Threading.Tasks.Task.FromResult<{{outer}}>(Microsoft.AspNetCore.Http.TypedResults.Ok());
            }

            public sealed class NestedEndpoint : TestProject.Openapi.Endpoints.GetResponsesEndpointBase
            {
                public override System.Threading.Tasks.Task<{{outer}}> HandleAsync(System.Threading.CancellationToken cancellationToken)
                {
                    {{inner}} nested = Microsoft.AspNetCore.Http.TypedResults.BadRequest();
                    return System.Threading.Tasks.Task.FromResult<{{outer}}>(nested);
                }
            }
            """;

        var frameworkPath = Path.GetDirectoryName(typeof(Microsoft.AspNetCore.Http.IResult).Assembly.Location)!;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Concat(Directory.GetFiles(frameworkPath, "*.dll"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("NestedUnionHandlers",
            [CSharpSyntaxTree.ParseText(handlerSource), CSharpSyntaxTree.ParseText(handler)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        Assert.That(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error),
            Is.Empty);
    }

    private static string HttpResult(string name) => $"global::Microsoft.AspNetCore.Http.HttpResults.{name}";

    private static string ExpectedUnion(IReadOnlyList<string> types, int start)
    {
        var remaining = types.Count - start;
        if (remaining == 1)
            return types[start];
        var prefix = "global::Microsoft.AspNetCore.Http.HttpResults.Results<";
        return remaining <= 6
            ? prefix + string.Join(", ", types.Skip(start)) + ">"
            : prefix + string.Join(", ", types.Skip(start).Take(5)) + ", " + ExpectedUnion(types, start + 5) + ">";
    }

    private static string Contract(IEnumerable<int> statuses, int[]? bodyStatuses = null)
    {
        var yaml = new StringBuilder("""
            openapi: "3.0.0"
            info:
              title: Responses
              version: "1"
            paths:
              /responses:
                get:
                  operationId: getResponses
                  responses:

            """);
        foreach (var status in statuses)
        {
            yaml.Append("\n        \"").Append(status).Append("\":\n          description: Response");
            if (status >= 210 && status < 300)
                yaml.Append("\n          content:\n            application/problem+json: {}");
            else if (bodyStatuses?.Contains(status) == true)
                yaml.Append("\n          content:\n            application/json:\n              schema:\n                type: integer");
        }
        return yaml.ToString();
    }
}