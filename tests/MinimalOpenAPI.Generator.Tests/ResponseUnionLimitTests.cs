using Microsoft.CodeAnalysis;

namespace MinimalOpenAPI.Generator.Tests;

[TestFixture]
public class ResponseUnionLimitTests
{
    private const string Contract = """
        openapi: "3.0.0"
        info:
          title: Responses
          version: "1"
        paths:
          /responses:
            get:
              operationId: getResponses
              responses:
                "200":
                  description: OK
                "201":
                  description: Created
                "202":
                  description: Accepted
                "400":
                  description: Bad request
                "401":
                  description: Unauthorized
                "403":
                  description: Forbidden
        """;

    [Test]
    public void SixDistinctAlternatives_GenerateTypedUnion()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", Contract)]);

        Assert.That(result.Diagnostics.Any(d => d.Id == "MOA017"), Is.False);
        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetResponsesEndpointBase.g.cs");
        Assert.That(source, Does.Contain("Results<global::Microsoft.AspNetCore.Http.HttpResults.Ok, global::Microsoft.AspNetCore.Http.HttpResults.Created, global::Microsoft.AspNetCore.Http.HttpResults.Accepted, global::Microsoft.AspNetCore.Http.HttpResults.BadRequest, global::Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult, global::Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>"));
    }

    [Test]
    public void SevenDistinctAlternatives_ReportErrorWithoutOversizedUnion()
    {
        var contract = Contract + "\n        \"404\":\n          description: Missing\n";
        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", contract)]);

        Assert.That(result.Diagnostics, Has.Some.Matches<Diagnostic>(d =>
            d.Id == "MOA017"
            && d.Severity == DiagnosticSeverity.Error
            && d.GetMessage().Contains("getResponses")
            && d.GetMessage().Contains("7")
            && d.GetMessage().Contains("six")
            && d.Location.GetLineSpan().Path == "openapi.yaml"));
        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetResponsesEndpointBase.g.cs");
        Assert.That(source, Does.Not.Contain("Results<"));
    }

    [Test]
    public void SevenDeclaredResponses_WithDuplicateMappedTypes_StayWithinLimit()
    {
        var contract = Contract.Replace("\"403\":", "\"407\":") + "\n        \"406\":\n          description: Not acceptable\n";
        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", contract)]);

        Assert.That(result.Diagnostics.Any(d => d.Id == "MOA017"), Is.False);
        Assert.That(GeneratorTestHelper.GetGeneratedSource(result, "GetResponsesEndpointBase.g.cs"), Does.Contain("Results<"));
    }

    [Test]
    public void FilteringSeventhResponse_RestoresSixAlternativeUnion()
    {
        var contract = Contract + "\n        \"404\":\n          description: Missing\n";
        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", contract)],
            globalExclusionsByFile: new Dictionary<string, string> { ["openapi.yaml"] = "404" });

        Assert.That(result.Diagnostics.Any(d => d.Id == "MOA017"), Is.False);
        Assert.That(GeneratorTestHelper.GetGeneratedSource(result, "GetResponsesEndpointBase.g.cs"), Does.Contain("Results<"));
    }
}
