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
            handlers: [(null, "getOrder", "403", "404")]);

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
            handlers: [("admin.yaml", "getOrder", null, "403")]);
        Assert.That(result.Diagnostics.Any(d => d.Id == "MOA015"), Is.False);
        Assert.That(GeneratorTestHelper.GetGeneratedSource(result, "Admin.GetOrderEndpointBase.g.cs"), Does.Not.Contain("ForbidHttpResult"));
        Assert.That(GeneratorTestHelper.GetGeneratedSource(result, "Public.GetOrderEndpointBase.g.cs"), Does.Contain("ForbidHttpResult"));

        var (single, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", Contract)],
            handlers: [("openapi.yaml", "getOrder", null, "403")]);
        Assert.That(GeneratorTestHelper.GetGeneratedSource(single, "GetOrderEndpointBase.g.cs"), Does.Not.Contain("ForbidHttpResult"));
    }

    [Test]
    public void MultipleFilesWithSameBasename_RequireAndMatchTheFullItemIdentity()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator("",
            [("contracts/orders/openapi.yaml", Contract), ("contracts/admin/openapi.yaml", Contract)],
            specNameOverridesByFilePath: new Dictionary<string, string>
            {
                ["contracts/orders/openapi.yaml"] = "Orders",
                ["contracts/admin/openapi.yaml"] = "Admin",
            },
            handlers: [("contracts/orders/openapi.yaml", "getOrder", null, "403")]);

        Assert.That(result.Diagnostics.Any(d => d.Id == "MOA015"), Is.False);
        Assert.That(GeneratorTestHelper.GetGeneratedSource(result, "Orders.GetOrderEndpointBase.g.cs"), Does.Not.Contain("ForbidHttpResult"));
        Assert.That(GeneratorTestHelper.GetGeneratedSource(result, "Admin.GetOrderEndpointBase.g.cs"), Does.Contain("ForbidHttpResult"));

        var (basename, _) = GeneratorTestHelper.RunGenerator("",
            [("contracts/orders/openapi.yaml", Contract), ("contracts/admin/openapi.yaml", Contract)],
            specNameOverridesByFilePath: new Dictionary<string, string>
            {
                ["contracts/orders/openapi.yaml"] = "Orders",
                ["contracts/admin/openapi.yaml"] = "Admin",
            },
            handlers: [("openapi.yaml", "getOrder", null, "403")]);
        Assert.That(basename.Diagnostics.Any(d => d.Id == "MOA015" && d.GetMessage().Contains("does not match")), Is.True);
    }

    [TestCase(null, "getOrder", null, null, "requires OpenApiFile")]
    [TestCase("missing.yaml", "getOrder", null, null, "does not match")]
    [TestCase("public.yaml", null, null, null, "missing Include")]
    [TestCase("public.yaml", "missingOperation", null, null, "was not found")]
    [TestCase("public.yaml", "getOrder", "409", null, "not declared")]
    [TestCase("public.yaml", "getOrder", null, "409", "not declared")]
    [TestCase("public.yaml", "getOrder", "oops", null, "invalid HTTP status code")]
    [TestCase("public.yaml", "getOrder", null, "oops", "invalid HTTP status code")]
    public void InvalidHandlerSettings_ProduceTargetedDiagnostic(
        string? openApiFile, string? include, string? includeCodes, string? excludeCodes, string message)
    {
        var (result, _) = GeneratorTestHelper.RunGenerator("",
            [("public.yaml", Contract), ("admin.yaml", Contract)],
            handlers: [(openApiFile, include, includeCodes, excludeCodes)]);
        Assert.That(result.Diagnostics.Any(d => d.Id == "MOA015" && d.GetMessage().Contains(message)), Is.True);
    }

    [Test]
    public void ExplicitOpenApiFile_IsValidatedForSingleDocument()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", Contract)],
            handlers: [("wrong.yaml", "getOrder", null, null)]);

        Assert.That(result.Diagnostics.Any(d => d.Id == "MOA015" && d.GetMessage().Contains("does not match")), Is.True);
    }

    [Test]
    public void IncludeAndExcludeOverlap_ExcludesTheResponse()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", Contract)],
            handlers: [("openapi.yaml", "getOrder", "403", "403")]);

        Assert.That(GeneratorTestHelper.GetGeneratedSource(result, "GetOrderEndpointBase.g.cs"), Does.Not.Contain("ForbidHttpResult"));
        Assert.That(GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs"), Does.Contain("Status403"));
    }

    [Test]
    public void InvalidGlobalCode_ProducesDiagnostic()
    {
        var (invalid, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", Contract)],
            globalExclusionsByFile: new Dictionary<string, string> { ["openapi.yaml"] = "700" });
        Assert.That(invalid.Diagnostics.Any(d => d.Id == "MOA015" && d.GetMessage().Contains("invalid HTTP status code")), Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void DuplicateOperationId_ReportsErrorAndSkipsDocument(bool withHandler)
    {
        var secondPath = "\n  /again:\n    get:\n      operationId: getOrder\n      responses:\n        \"200\":\n          description: OK";
        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", Contract + secondPath)],
            handlers: withHandler ? [(null, "getOrder", "200", null)] : null);

        Assert.That(result.Diagnostics, Has.Some.Matches<Microsoft.CodeAnalysis.Diagnostic>(
            d => d.Id == "MOA016"
                && d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error
                && d.GetMessage().Contains("getOrder")
                && d.GetMessage().Contains("openapi.yaml")
                && d.Location.GetLineSpan().Path == "openapi.yaml"));
        Assert.That(result.Diagnostics.Any(d => d.Id == "MOA015" || d.Id == "CS8785"), Is.False);
        Assert.That(result.GeneratedTrees, Is.Empty);
    }
}