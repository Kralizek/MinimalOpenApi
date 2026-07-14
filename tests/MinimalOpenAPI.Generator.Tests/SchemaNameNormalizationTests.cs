using Microsoft.CodeAnalysis;

namespace MinimalOpenAPI.Generator.Tests;

/// <summary>
/// Tests for the centralised schema-name normalisation feature (issue #82 / #75).
/// Every legal OpenAPI component name must map to a deterministic, valid C# type name.
/// </summary>
[TestFixture]
public class SchemaNameNormalizationTests
{
    // ── NormalizeSchemaTypeName — verified via generated source ───────────

    /// <summary>
    /// A minimal spec containing a single schema whose key is the input name.
    /// The generated DTO source should contain the expected normalised type name.
    /// </summary>
    private static (string DtoSource, string? ErrorDiagId) GetNormalisedTypeName(
        string openApiSchemaName,
        string? typeDefinition = null)
    {
        var def = typeDefinition ?? "type: object\n      properties:\n        id:\n          type: integer";
        var yaml = $$"""
            openapi: "3.0.0"
            info:
              title: Test
              version: "1.0.0"
            paths:
              /noop:
                get:
                  operationId: noop
                  responses:
                    "200":
                      description: OK
            components:
              schemas:
                {{openApiSchemaName}}:
                  {{def}}
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", yaml)]);

        var errorDiagId = result.Diagnostics
            .FirstOrDefault(d => d.Id is "MOA012" or "MOA013")?.Id;

        var dtoSource = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.EndsWith("Dtos.g.cs", StringComparison.OrdinalIgnoreCase))
            ?.GetText().ToString() ?? string.Empty;

        return (dtoSource, errorDiagId);
    }

    [TestCase("Invoice", "Invoice")]
    [TestCase("billing.invoice", "BillingInvoice")]
    [TestCase("billing-invoice", "BillingInvoice")]
    [TestCase("billing_invoice", "BillingInvoice")]
    [TestCase("Acme.Platform.ErrorResponse", "AcmePlatformErrorResponse")]
    [TestCase("BillingInvoice", "BillingInvoice")]  // already normalised
    [TestCase("order__data", "OrderData")]           // repeated separators
    public void NormalizeSchemaTypeName_ReturnsExpectedResult(string openApiName, string expected)
    {
        var (source, errorDiagId) = GetNormalisedTypeName(openApiName);
        Assert.That(errorDiagId, Is.Null, $"No error diagnostic expected for '{openApiName}'");
        Assert.That(source, Does.Contain($"public sealed record {expected}"),
            $"Expected normalised type '{expected}' not found in generated source for '{openApiName}'");
    }

    [TestCase("123-invoice", "Value123Invoice")]
    public void NormalizeSchemaTypeName_LeadingDigit_PrefixedWithValue(string openApiName, string expected)
    {
        var (source, errorDiagId) = GetNormalisedTypeName(openApiName);
        Assert.That(errorDiagId, Is.Null);
        Assert.That(source, Does.Contain($"public sealed record {expected}"));
    }

    [TestCase("class")]
    [TestCase("event")]
    [TestCase("object")]
    public void NormalizeSchemaTypeName_CSharpKeyword_IsCapitalisedToValidIdentifier(string openApiName)
    {
        // Keywords like "class" → "Class" (first letter uppercased) which is valid C#
        var expected = char.ToUpperInvariant(openApiName[0]) + openApiName[1..];
        var (source, errorDiagId) = GetNormalisedTypeName(openApiName);
        Assert.That(errorDiagId, Is.Null);
        Assert.That(source, Does.Contain($"public sealed record {expected}"));
    }

    [TestCase("...")]
    [TestCase("---")]
    public void NormalizeSchemaTypeName_OnlySeparators_ReportsMOA013(string openApiName)
    {
        var yaml = $$"""
            openapi: "3.0.0"
            info:
              title: Test
              version: "1.0.0"
            paths:
              /noop:
                get:
                  operationId: noop
                  responses:
                    "200":
                      description: OK
            components:
              schemas:
                "{{openApiName}}":
                  type: object
                  properties:
                    id:
                      type: integer
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", yaml)]);

        Assert.That(result.Diagnostics, Has.Some.Matches<Diagnostic>(d =>
            d.Id == "MOA013" && d.Severity == DiagnosticSeverity.Error));
    }

    // ── Dotted object schema (YAML) ───────────────────────────────────────

    [Test]
    public void DottedObjectSchema_GeneratesNormalisedRecord_Yaml()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.DottedSchemaYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("public sealed record BillingInvoice"));
        Assert.That(source, Does.Contain("public sealed record AcmePlatformErrorResponse"));
    }

    [Test]
    public void DottedObjectSchema_DoesNotContainRawDottedName_Yaml()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.DottedSchemaYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Not.Contain("Billing."));
        Assert.That(source, Does.Not.Contain("Acme."));
    }

    // ── Dotted enum schema (YAML) ─────────────────────────────────────────

    [Test]
    public void DottedEnumSchema_GeneratesNormalisedEnum_Yaml()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.DottedSchemaYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("public enum BillingInvoiceStatus"));
    }

    [Test]
    public void DottedEnumSchema_HasCorrectMembers_Yaml()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.DottedSchemaYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("Draft"));
        Assert.That(source, Does.Contain("Sent"));
        Assert.That(source, Does.Contain("Paid"));
    }

    // ── $ref to dotted schema resolves correctly (YAML) ──────────────────

    [Test]
    public void DottedSchemaRef_IsResolvedToNormalisedType_Yaml()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.DottedSchemaYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        // BillingInvoice.status should reference BillingInvoiceStatus (the normalised enum name)
        Assert.That(source, Does.Contain("public BillingInvoiceStatus? Status { get; init; }"));
    }

    // ── Dotted schema (JSON) ──────────────────────────────────────────────

    [Test]
    public void DottedObjectSchema_GeneratesNormalisedRecord_Json()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.json", OpenApiFixtures.DottedSchemaJson)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("public sealed record BillingInvoice"));
        Assert.That(source, Does.Contain("public sealed record AcmePlatformErrorResponse"));
    }

    [Test]
    public void DottedEnumSchema_GeneratesNormalisedEnum_Json()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.json", OpenApiFixtures.DottedSchemaJson)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("public enum BillingInvoiceStatus"));
    }

    [Test]
    public void DottedSchemaRef_IsResolvedToNormalisedType_Json()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.json", OpenApiFixtures.DottedSchemaJson)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("public BillingInvoiceStatus? Status { get; init; }"));
    }

    // ── Request / response bodies with dotted schemas ─────────────────────

    [Test]
    public void DottedSchemaRequestBody_UsesNormalisedType_Yaml()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.DottedSchemaRequestResponseYaml)]);

        // Handler base should reference the normalised type
        var source = GeneratorTestHelper.GetGeneratedSource(result, "CreateOrderEndpointBase.g.cs");

        Assert.That(source, Does.Contain("OrderCreate"));
        Assert.That(source, Does.Not.Contain("Order.Create"));
    }

    [Test]
    public void DottedSchemaResponseBody_UsesNormalisedType_Yaml()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.DottedSchemaRequestResponseYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "CreateOrderEndpointBase.g.cs");

        Assert.That(source, Does.Contain("OrderView"));
        Assert.That(source, Does.Not.Contain("Order.View"));
    }

    // ── Arrays and dictionaries of dotted schemas ─────────────────────────

    [Test]
    public void DottedSchemaInArray_UsesNormalisedElementType_Yaml()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.DottedSchemaArrayAndDictYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        // The items array property should use ProductSummary[] (normalised)
        Assert.That(source, Does.Contain("ProductSummary"));
        Assert.That(source, Does.Not.Contain("Product.Summary"));
    }

    [Test]
    public void DottedSchemaInDictionary_UsesNormalisedValueType_Yaml()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.DottedSchemaArrayAndDictYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        // The dictionary property should use Dictionary<string, ProductSummary>
        Assert.That(source, Does.Contain("Dictionary<string, ProductSummary>"));
    }

    // ── allOf with dotted schemas ─────────────────────────────────────────

    [Test]
    public void DottedSchemaAllOf_GeneratesNormalisedRecords_Yaml()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.DottedSchemaAllOfYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("public sealed record ItemBase"));
        Assert.That(source, Does.Contain("public sealed record ItemExtended"));
        Assert.That(source, Does.Not.Contain("Item.Base"));
        Assert.That(source, Does.Not.Contain("Item.Extended"));
    }

    // ── Hyphen and underscore schema names ────────────────────────────────

    [Test]
    public void HyphenatedSchemaName_GeneratesNormalisedRecord_Yaml()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.HyphenUnderscoreSchemaYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("public sealed record BillingInvoice"));
        Assert.That(source, Does.Not.Contain("billing-invoice"));
    }

    [Test]
    public void UnderscoreSchemaName_GeneratesNormalisedRecord_Yaml()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.HyphenUnderscoreSchemaYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("public sealed record BillingInvoiceItem"));
        Assert.That(source, Does.Not.Contain("billing_invoice_item"));
    }

    // ── Leading-digit schema name ─────────────────────────────────────────

    [Test]
    public void LeadingDigitSchemaName_IsPrefixedWithValue_Yaml()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.LeadingDigitSchemaYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("public sealed record Value123Invoice"));
    }

    [Test]
    public void LeadingDigitSchemaRef_IsResolvedToValuePrefixedType_Yaml()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.LeadingDigitSchemaYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetItemEndpointBase.g.cs");

        Assert.That(source, Does.Contain("Value123Invoice"));
    }

    // ── C# keyword schema name ────────────────────────────────────────────

    [Test]
    public void KeywordSchemaName_IsNormalisedToCapitalisedIdentifier_Yaml()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.KeywordSchemaYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        // "class" → "Class" (valid C# identifier)
        Assert.That(source, Does.Contain("public sealed record Class"));
    }

    // ── ReadWriteSchemaHandling=Split with dotted schema ─────────────────

    [Test]
    public void DottedSchema_ReadWriteSplit_GeneratesScopedVariants_Yaml()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.DottedSchemaReadWriteSplitYaml)],
            readWriteSchemaHandling: "Split");

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("public sealed record OrderDataRequest"));
        Assert.That(source, Does.Contain("public sealed record OrderDataResponse"));
        Assert.That(source, Does.Not.Contain("Order.DataRequest"));
        Assert.That(source, Does.Not.Contain("Order.DataResponse"));
    }

    // ── Inline child of dotted parent ─────────────────────────────────────

    [Test]
    public void DottedParent_InlineChildUsesNormalisedParentName_Yaml()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.DottedParentInlineChildYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        // Inline address object under Acme.Customer → AcmeCustomerAddress
        Assert.That(source, Does.Contain("public sealed record AcmeCustomerAddress"));
        Assert.That(source, Does.Not.Contain("Acme.CustomerAddress"));
    }

    // ── Dotted enum as query parameter ────────────────────────────────────

    [Test]
    public void DottedEnumQueryParam_ParametersRecord_UsesFullyQualifiedNormalisedType()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.DottedEnumQueryParamYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "ListInvoicesEndpointBase.g.cs");

        // The Parameters record must reference the normalised type with the full contracts namespace
        Assert.That(source, Does.Contain("global::TestProject.Openapi.Contracts.BillingInvoiceStatus"));
        Assert.That(source, Does.Not.Contain("Billing.InvoiceStatus"));
    }

    // ── Collision detection ───────────────────────────────────────────────

    [Test]
    public void CollidingSchemaNames_ReportsMOA012_Yaml()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.CollisionSchemaYaml)]);

        Assert.That(result.Diagnostics, Has.Some.Matches<Diagnostic>(d =>
            d.Id == "MOA012" &&
            d.Severity == DiagnosticSeverity.Error));
    }

    [Test]
    public void CollidingSchemaNames_DiagnosticMessageContainsConflictingNames_Yaml()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.CollisionSchemaYaml)]);

        var moa012 = result.Diagnostics.FirstOrDefault(d => d.Id == "MOA012");
        Assert.That(moa012, Is.Not.Null);
        var message = moa012!.GetMessage();
        Assert.That(message, Does.Contain("BillingInvoice"));
    }

    [Test]
    public void CollidingSchemaNames_NoCodeEmitted_Yaml()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.CollisionSchemaYaml)]);

        // No DTO source should be emitted when a collision exists
        var dtoSource = result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.EndsWith("Dtos.g.cs", StringComparison.OrdinalIgnoreCase));

        Assert.That(dtoSource, Is.Null,
            "No DTO source file should be emitted when schema names collide");
    }

    // ── Unnormalisable name ───────────────────────────────────────────────

    [Test]
    public void UnnormalisableSchemaName_ReportsMOA013_Yaml()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.UnnormalisableSchemaYaml)]);

        Assert.That(result.Diagnostics, Has.Some.Matches<Diagnostic>(d =>
            d.Id == "MOA013" &&
            d.Severity == DiagnosticSeverity.Error));
    }

    [Test]
    public void UnnormalisableSchemaName_DiagnosticMessageContainsOriginalName_Yaml()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.UnnormalisableSchemaYaml)]);

        var moa013 = result.Diagnostics.FirstOrDefault(d => d.Id == "MOA013");
        Assert.That(moa013, Is.Not.Null);
        Assert.That(moa013!.GetMessage(), Does.Contain("..."));
    }

    // ── Simple valid names are unchanged (backward compat) ───────────────

    [Test]
    public void SimpleValidSchemaName_IsUnchanged_Yaml()
    {
        var additionalFiles = new[] { ("openapi.yaml", OpenApiFixtures.GetClientYaml) };
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: additionalFiles);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        // Original "Client" schema should still produce "Client" record unchanged
        Assert.That(source, Does.Contain("public sealed record Client"));
    }

    // ── Generated output compiles without errors ──────────────────────────

    /// <summary>
    /// Asserts that the output compilation produced by the generator has no unexpected
    /// errors.  CS0246, CS0234, and CS0400 (type/namespace not found) are accepted because
    /// the test compilation intentionally omits external assembly references (ASP.NET Core,
    /// System.Text.Json, etc.).  Any other error — especially CS1xxx syntax errors —
    /// indicates that an unnormalised dotted schema name leaked into the emitted C#.
    /// </summary>
    private static void AssertNoUnexpectedCompilationErrors(Compilation outputCompilation)
    {
        var unexpected = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error
                     && d.Id is not ("CS0246" or "CS0234" or "CS0400"))
            .ToList();

        Assert.That(unexpected, Is.Empty,
            $"Generated C# has unexpected compilation errors:{System.Environment.NewLine}" +
            string.Join(System.Environment.NewLine,
                unexpected.Select(e => $"  {e.Id}: {e.GetMessage()}")));
    }

    [Test]
    public void DottedSchema_GeneratedOutputCompilesWithoutRoslynErrors_Yaml()
    {
        var (result, outputCompilation) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.DottedSchemaYaml)]);

        // No MOA012/MOA013 diagnostics
        Assert.That(result.Diagnostics.Where(d => d.Id is "MOA012" or "MOA013"), Is.Empty,
            "No schema name collision or unnormalisable name diagnostics should be emitted");

        // No generator execution errors
        Assert.That(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error
                                                   && d.Id != "MOA001"), // MOA001 = missing handler (expected in test context)
            Is.Empty,
            "No generator errors should be emitted");

        // Verify the output compilation has no syntax errors from unnormalised names
        AssertNoUnexpectedCompilationErrors(outputCompilation);
    }

    [Test]
    public void DottedSchemaRequestResponse_GeneratedOutputCompilesWithoutRoslynErrors_Yaml()
    {
        var (result, outputCompilation) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.DottedSchemaRequestResponseYaml)]);

        Assert.That(result.Diagnostics.Where(d => d.Id is "MOA012" or "MOA013"), Is.Empty);

        // No generator execution errors
        Assert.That(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error
                                                   && d.Id != "MOA001"),
            Is.Empty);

        AssertNoUnexpectedCompilationErrors(outputCompilation);
    }

    [Test]
    public void DottedSchemaAllOf_GeneratedOutputCompilesWithoutRoslynErrors_Yaml()
    {
        var (result, outputCompilation) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.DottedSchemaAllOfYaml)]);

        Assert.That(result.Diagnostics.Where(d => d.Id is "MOA012" or "MOA013"), Is.Empty);

        // No generator execution errors
        Assert.That(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error
                                                   && d.Id != "MOA001"),
            Is.Empty);

        AssertNoUnexpectedCompilationErrors(outputCompilation);
    }

    [Test]
    public void DottedEnumPathParam_HandlerBase_UsesNormalisedType()
    {
        var (result, outputCompilation) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.DottedEnumPathParamYaml)]);

        Assert.That(result.Diagnostics.Where(d => d.Id is "MOA012" or "MOA013"), Is.Empty);

        // Handler base should use the normalised type name
        var handlerSource = GeneratorTestHelper.GetGeneratedSource(result, "GetInvoicesByStatusEndpointBase.g.cs");
        Assert.That(handlerSource, Does.Contain("BillingInvoiceStatus"),
            "Handler base should use normalised BillingInvoiceStatus for the path parameter type");
        Assert.That(handlerSource, Does.Not.Contain("Billing.InvoiceStatus"),
            "Handler base must not contain raw dotted name Billing.InvoiceStatus");

        AssertNoUnexpectedCompilationErrors(outputCompilation);
    }

    [Test]
    public void DottedEnumPathParam_EndpointMapping_UsesNormalisedType()
    {
        var (result, outputCompilation) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.DottedEnumPathParamYaml)]);

        Assert.That(result.Diagnostics.Where(d => d.Id is "MOA012" or "MOA013"), Is.Empty);

        // Endpoint lambda should also use the normalised type name
        var mappingSource = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");
        Assert.That(mappingSource, Does.Contain("BillingInvoiceStatus"),
            "Endpoint mapping should use normalised BillingInvoiceStatus for the path parameter type");
        Assert.That(mappingSource, Does.Not.Contain("Billing.InvoiceStatus"),
            "Endpoint mapping must not contain raw dotted name Billing.InvoiceStatus");

        AssertNoUnexpectedCompilationErrors(outputCompilation);
    }

    [Test]
    public void DottedEnumMultipartField_HandlerBase_UsesNormalisedType()
    {
        var (result, outputCompilation) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.DottedEnumMultipartFieldYaml)]);

        Assert.That(result.Diagnostics.Where(d => d.Id is "MOA012" or "MOA013"), Is.Empty);

        // Handler base multipart form record should use the normalised enum type
        var handlerSource = GeneratorTestHelper.GetGeneratedSource(result, "CreateInvoiceEndpointBase.g.cs");
        Assert.That(handlerSource, Does.Contain("BillingInvoiceStatus"),
            "Handler base form record should use normalised BillingInvoiceStatus");
        Assert.That(handlerSource, Does.Not.Contain("Billing.InvoiceStatus"),
            "Handler base must not contain raw dotted name Billing.InvoiceStatus");

        AssertNoUnexpectedCompilationErrors(outputCompilation);
    }

    // ── Generated-symbol collision diagnostics (MOA014) ───────────────────

    [Test]
    public void ScopedVariantCollision_Reports_MOA014()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.ScopedVariantCollisionYaml)],
            readWriteSchemaHandling: "Split");

        Assert.That(
            result.Diagnostics.Any(d => d.Id == "MOA014"),
            Is.True,
            "MOA014 should be emitted when a scoped variant name collides with an existing component");
    }

    [Test]
    public void InlineDerivedCollision_Reports_MOA014()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.InlineDerivedCollisionYaml)]);

        Assert.That(
            result.Diagnostics.Any(d => d.Id == "MOA014"),
            Is.True,
            "MOA014 should be emitted when an inline-derived type name collides with an existing component");
    }

    // ── Endpoint mapping uses normalised names ────────────────────────────

    [Test]
    public void DottedSchemaRef_EndpointMapping_UsesNormalisedType_Yaml()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.DottedSchemaYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");

        // The endpoint mapping should use BillingInvoice (normalised)
        Assert.That(source, Does.Contain("BillingInvoice"));
        Assert.That(source, Does.Not.Contain("Billing.Invoice"));
    }
}