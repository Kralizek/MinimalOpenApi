namespace MinimalOpenAPI.Generator.Tests;

[TestFixture]
public class HandlerResponseConfigurationTests
{
    private const string Contract = """
        openapi: "3.0.0"
        info:
          title: Orders
          version: "1"
        paths:
          /orders:
            get:
              operationId: getOrder
              responses:
                "200":
                  description: OK
                "401":
                  description: Unauthorized
                "403":
                  description: Forbidden
                "404":
                  description: Missing
        """;

    [Test]
    public void GlobalExclusion_WithImplicitOverride_RestoresIncludedStatusAndPreservesMetadata()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", Contract)],
            globalExclusionsByFile: new Dictionary<string, string> { ["openapi.yaml"] = "401;403" },
            overrides: [(null, "getOrder", "403", "404")]);

        Assert.That(result.Diagnostics.Any(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error), Is.False);
        var handler = GeneratorTestHelper.GetGeneratedSource(result, "GetOrderEndpointBase.g.cs");
        Assert.That(handler, Does.Contain("Results<"));
        Assert.That(handler, Does.Contain("ForbidHttpResult"));
        Assert.That(handler, Does.Not.Contain("UnauthorizedHttpResult"));
        Assert.That(handler, Does.Not.Contain("NotFound"));
        var mapping = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");
        foreach (var code in new[] { "200", "401", "403", "404" })
            Assert.That(mapping, Does.Contain("Status" + code));
    }

    [Test]
    public void NoConfiguration_PreservesExistingUnion()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", Contract)]);
        var handler = GeneratorTestHelper.GetGeneratedSource(result, "GetOrderEndpointBase.g.cs");
        Assert.That(handler, Does.Contain("UnauthorizedHttpResult"));
        Assert.That(handler, Does.Contain("ForbidHttpResult"));
        Assert.That(handler, Does.Contain("NotFound"));
    }

    [Test]
    public void ExplicitAndMultiContractOverrides_BindOnlySelectedDocument()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator("",
            [("public.yaml", Contract), ("admin.yaml", Contract)],
            overrides: [("admin.yaml", "getOrder", null, "403")]);
        Assert.That(result.Diagnostics.Any(d => d.Id == "MOA015"), Is.False);
        Assert.That(GeneratorTestHelper.GetGeneratedSource(result, "Admin.GetOrderEndpointBase.g.cs"), Does.Not.Contain("ForbidHttpResult"));
        Assert.That(GeneratorTestHelper.GetGeneratedSource(result, "Public.GetOrderEndpointBase.g.cs"), Does.Contain("ForbidHttpResult"));

        var (single, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", Contract)],
            overrides: [("openapi.yaml", "getOrder", null, "403")]);
        Assert.That(GeneratorTestHelper.GetGeneratedSource(single, "GetOrderEndpointBase.g.cs"), Does.Not.Contain("ForbidHttpResult"));
    }

    [TestCase(null, "getOrder", null, null, "requires OpenApi")]
    [TestCase("missing.yaml", "getOrder", null, null, "not configured")]
    [TestCase("public.yaml", null, null, null, "missing OperationId")]
    [TestCase("public.yaml", "missingOperation", null, null, "was not found")]
    [TestCase("public.yaml", "getOrder", "409", null, "not declared")]
    [TestCase("public.yaml", "getOrder", null, "409", "not declared")]
    [TestCase("public.yaml", "getOrder", "oops", null, "invalid HTTP status code")]
    public void InvalidOverrides_ProduceTargetedDiagnostic(
        string? openApi, string? operation, string? include, string? exclude, string message)
    {
        var (result, _) = GeneratorTestHelper.RunGenerator("",
            [("public.yaml", Contract), ("admin.yaml", Contract)],
            overrides: [(openApi, operation, include, exclude)]);
        Assert.That(result.Diagnostics.Any(d => d.Id == "MOA015" && d.GetMessage().Contains(message)), Is.True);
    }

    [Test]
    public void InvalidGlobalCodeAndAmbiguousOperation_ProduceDiagnostics()
    {
        var (invalid, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", Contract)],
            globalExclusionsByFile: new Dictionary<string, string> { ["openapi.yaml"] = "700" });
        Assert.That(invalid.Diagnostics.Any(d => d.Id == "MOA015" && d.GetMessage().Contains("invalid HTTP status code")), Is.True);

        var secondPath = "\n  /again:\n    get:\n      operationId: getOrder\n      responses:\n        \"200\":\n          description: OK";
        var (ambiguous, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", Contract + secondPath)],
            overrides: [(null, "getOrder", "200", null)]);
        Assert.That(ambiguous.Diagnostics.Any(d => d.Id == "MOA015" && d.GetMessage().Contains("ambiguous")), Is.True,
            string.Join("; ", ambiguous.Diagnostics.Select(d => d.ToString())));
    }
}