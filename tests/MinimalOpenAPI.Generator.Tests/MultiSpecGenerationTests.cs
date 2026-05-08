using MinimalOpenAPI.Generator;

namespace MinimalOpenAPI.Generator.Tests;

/// <summary>
/// Tests for multiple <c>&lt;OpenApi&gt;</c> items per project:
/// each spec is isolated in its own <c>{RootNamespace}.{SpecName}</c> sub-namespace,
/// preventing type-name collisions between specs.
/// </summary>
[TestFixture]
public class MultiSpecGenerationTests
{
    // A minimal "payment" spec with a Payment schema
    private const string PaymentApiYaml = """
        openapi: "3.0.0"
        info:
          title: Payment API
          version: "1.0.0"
        paths:
          /payments/{id}:
            get:
              operationId: getPayment
              parameters:
                - name: id
                  in: path
                  required: true
                  schema:
                    type: string
                    format: uuid
              responses:
                "200":
                  description: OK
                  content:
                    application/json:
                      schema:
                        $ref: '#/components/schemas/Payment'
                "404":
                  description: Not found
        components:
          schemas:
            Payment:
              type: object
              required:
                - id
                - amount
              properties:
                id:
                  type: string
                  format: uuid
                amount:
                  type: number
        """;

    // A minimal "shipping" spec with a Shipment schema (same property name 'id' as Payment — collision without isolation)
    private const string ShippingApiYaml = """
        openapi: "3.0.0"
        info:
          title: Shipping API
          version: "1.0.0"
        paths:
          /shipments/{id}:
            get:
              operationId: getShipment
              parameters:
                - name: id
                  in: path
                  required: true
                  schema:
                    type: string
                    format: uuid
              responses:
                "200":
                  description: OK
                  content:
                    application/json:
                      schema:
                        $ref: '#/components/schemas/Shipment'
                "404":
                  description: Not found
        components:
          schemas:
            Shipment:
              type: object
              required:
                - id
                - destination
              properties:
                id:
                  type: string
                  format: uuid
                destination:
                  type: string
        """;

    private const string PaymentHandlerImpl = """
        public class GetPaymentHandler : GetPaymentEndpointBase
        {
            public override System.Threading.Tasks.Task<object> HandleAsync(
                System.Guid id, System.Threading.CancellationToken ct)
                    => throw new System.NotImplementedException();
        }
        """;

    private const string ShipmentHandlerImpl = """
        public class GetShipmentHandler : GetShipmentEndpointBase
        {
            public override System.Threading.Tasks.Task<object> HandleAsync(
                System.Guid id, System.Threading.CancellationToken ct)
                    => throw new System.NotImplementedException();
        }
        """;

    [Test]
    public void TwoSpecs_EachGetsIsolatedNamespace()
    {
        var additionalFiles = new[]
        {
            ("payment-api.yaml", PaymentApiYaml),
            ("shipping-api.yaml", ShippingApiYaml),
        };

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: PaymentHandlerImpl + ShipmentHandlerImpl,
            additionalFiles: additionalFiles);

        // Each spec's DTOs are in their own namespace
        var paymentDtos = GeneratorTestHelper.GetGeneratedSource(result, "PaymentApi.Dtos.g.cs");
        var shippingDtos = GeneratorTestHelper.GetGeneratedSource(result, "ShippingApi.Dtos.g.cs");

        Assert.That(paymentDtos, Does.Contain("namespace TestProject.PaymentApi.Contracts;"));
        Assert.That(shippingDtos, Does.Contain("namespace TestProject.ShippingApi.Contracts;"));
    }

    [Test]
    public void TwoSpecs_EachGetsIsolatedEndpointsNamespace()
    {
        var additionalFiles = new[]
        {
            ("payment-api.yaml", PaymentApiYaml),
            ("shipping-api.yaml", ShippingApiYaml),
        };

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: PaymentHandlerImpl + ShipmentHandlerImpl,
            additionalFiles: additionalFiles);

        var paymentHandler = GeneratorTestHelper.GetGeneratedSource(result, "PaymentApi.GetPaymentEndpointBase.g.cs");
        var shippingHandler = GeneratorTestHelper.GetGeneratedSource(result, "ShippingApi.GetShipmentEndpointBase.g.cs");

        Assert.That(paymentHandler, Does.Contain("namespace TestProject.PaymentApi.Endpoints;"));
        Assert.That(shippingHandler, Does.Contain("namespace TestProject.ShippingApi.Endpoints;"));
    }

    [Test]
    public void TwoSpecs_EachGetsIsolatedDiRegistration()
    {
        var additionalFiles = new[]
        {
            ("payment-api.yaml", PaymentApiYaml),
            ("shipping-api.yaml", ShippingApiYaml),
        };

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: PaymentHandlerImpl + ShipmentHandlerImpl,
            additionalFiles: additionalFiles);

        var paymentDi = GeneratorTestHelper.GetGeneratedSource(result, "PaymentApi.DependencyInjection.g.cs");
        var shippingDi = GeneratorTestHelper.GetGeneratedSource(result, "ShippingApi.DependencyInjection.g.cs");

        Assert.That(paymentDi, Does.Contain("namespace TestProject.PaymentApi.Endpoints;"));
        Assert.That(shippingDi, Does.Contain("namespace TestProject.ShippingApi.Endpoints;"));
    }

    [Test]
    public void TwoSpecs_ModuleInitializersUseNewMappingSignature()
    {
        var additionalFiles = new[]
        {
            ("payment-api.yaml", PaymentApiYaml),
            ("shipping-api.yaml", ShippingApiYaml),
        };

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: PaymentHandlerImpl + ShipmentHandlerImpl,
            additionalFiles: additionalFiles);

        var paymentDi = GeneratorTestHelper.GetGeneratedSource(result, "PaymentApi.DependencyInjection.g.cs");
        var shippingDi = GeneratorTestHelper.GetGeneratedSource(result, "ShippingApi.DependencyInjection.g.cs");

        // Verify the new (builder, group) => delegate signature is used
        Assert.That(paymentDi, Does.Contain("(builder, group) =>"));
        Assert.That(shippingDi, Does.Contain("(builder, group) =>"));
    }

    [Test]
    public void TwoSpecs_DtoTypesAreFullyQualifiedWithSpecSegment()
    {
        var additionalFiles = new[]
        {
            ("payment-api.yaml", PaymentApiYaml),
            ("shipping-api.yaml", ShippingApiYaml),
        };

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: PaymentHandlerImpl + ShipmentHandlerImpl,
            additionalFiles: additionalFiles);

        var paymentMapping = GeneratorTestHelper.GetGeneratedSource(result, "PaymentApi.EndpointMapping.g.cs");
        var shippingMapping = GeneratorTestHelper.GetGeneratedSource(result, "ShippingApi.EndpointMapping.g.cs");

        Assert.That(paymentMapping, Does.Contain("TestProject.PaymentApi.Contracts.Payment"));
        Assert.That(shippingMapping, Does.Contain("TestProject.ShippingApi.Contracts.Shipment"));
    }

    [Test]
    public void ExplicitNamespaceMetadata_OverridesFileNameDerivedName()
    {
        // When MinimalOpenApiNamespace metadata is provided it takes priority over the file name.
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "",
            additionalFiles: [("openapi.yaml", OpenApiFixtures.GetClientYaml)],
            specNameOverride: "Clients");

        var handler = GeneratorTestHelper.GetGeneratedSource(result, "Clients.GetClientEndpointBase.g.cs");
        Assert.That(handler, Does.Contain("namespace TestProject.Clients.Endpoints;"));
    }
}

[TestFixture]
public class DuplicateSpecNameDiagnosticsTests
{
    [Test]
    public void TwoFilesWithSameDerivedSpecName_EmitsMoa009AndSkipsGenerationForConflicts()
    {
        var additionalFiles = new[]
        {
            ("apis/admin/openapi.yaml", OpenApiFixtures.GetClientYaml),
            ("apis/public/openapi.yaml", OpenApiFixtures.GetClientYaml),
        };

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "",
            additionalFiles: additionalFiles);

        var moa009 = result.Diagnostics.Where(d => d.Id == "MOA009").ToArray();
        Assert.That(moa009, Has.Length.EqualTo(2), "MOA009 should be emitted once per conflicting OpenAPI file.");
        // "openapi.yaml" derives to "Openapi" with the current PascalCase normalization rules.
        Assert.That(moa009.All(d => d.GetMessage().Contains("spec name 'Openapi'", StringComparison.Ordinal)), Is.True);
        Assert.That(moa009.All(d => d.GetMessage().Contains("Set the Namespace metadata on one or more <OpenApi> items to provide unique generated namespaces.", StringComparison.Ordinal)), Is.True);
        Assert.That(moa009.All(d => d.GetMessage().Contains("apis/admin/openapi.yaml", StringComparison.Ordinal)), Is.True);
        Assert.That(moa009.All(d => d.GetMessage().Contains("apis/public/openapi.yaml", StringComparison.Ordinal)), Is.True);
        Assert.That(result.GeneratedTrees.Any(t => t.FilePath.Contains("MinimalOpenApi.Openapi.", StringComparison.Ordinal)), Is.False);
    }

    [Test]
    public void SameFileNamesWithDifferentNamespaceMetadata_DoNotEmitMoa009()
    {
        var additionalFiles = new[]
        {
            ("apis/admin/openapi.yaml", OpenApiFixtures.GetClientYaml),
            ("apis/public/openapi.yaml", OpenApiFixtures.GetClientYaml),
        };

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "",
            additionalFiles: additionalFiles,
            specNameOverridesByFilePath: new Dictionary<string, string>
            {
                ["apis/admin/openapi.yaml"] = "AdminApi",
                ["apis/public/openapi.yaml"] = "PublicApi",
            });

        Assert.That(result.Diagnostics.Any(d => d.Id == "MOA009"), Is.False);
        Assert.That(result.GeneratedTrees.Any(t => t.FilePath.EndsWith("AdminApi.GetClientEndpointBase.g.cs", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(result.GeneratedTrees.Any(t => t.FilePath.EndsWith("PublicApi.GetClientEndpointBase.g.cs", StringComparison.OrdinalIgnoreCase)), Is.True);
    }

    [Test]
    public void NormalizedFileNamesThatCollide_EmitMoa009()
    {
        var additionalFiles = new[]
        {
            ("payment-api.yaml", OpenApiFixtures.GetClientYaml),
            ("payment_api.yaml", OpenApiFixtures.GetClientYaml),
        };

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "",
            additionalFiles: additionalFiles);

        var moa009 = result.Diagnostics.Where(d => d.Id == "MOA009").ToArray();
        Assert.That(moa009, Has.Length.EqualTo(2));
        Assert.That(moa009.All(d => d.GetMessage().Contains("spec name 'PaymentApi'", StringComparison.Ordinal)), Is.True);
    }
}

/// <summary>
/// Tests for the spec-name derivation logic — exercised through the generator
/// so the exact filename-to-namespace mapping is covered without requiring
/// access to the internal <c>DeriveSpecName</c> method directly.
/// </summary>
[TestFixture]
public class DeriveSpecNameTests
{
    private static readonly string SimpleHandler = """
        public class GetPaymentHandler : GetPaymentEndpointBase
        {
            public override System.Threading.Tasks.Task<object> HandleAsync(
                System.Guid id, System.Threading.CancellationToken ct)
                    => throw new System.NotImplementedException();
        }
        """;

    private const string MinimalPaymentYaml = """
        openapi: "3.0.0"
        info:
          title: Payment API
          version: "1.0.0"
        paths:
          /payments/{id}:
            get:
              operationId: getPayment
              parameters:
                - name: id
                  in: path
                  required: true
                  schema:
                    type: string
                    format: uuid
              responses:
                "200":
                  description: OK
                "404":
                  description: Not found
        """;

    [TestCase("payment.yaml", "Payment")]
    [TestCase("payment-api.yaml", "PaymentApi")]
    [TestCase("payment_api.yaml", "PaymentApi")]
    [TestCase("myPaymentService.yaml", "MyPaymentService")]
    public void FileNameIsDerivedToPascalCaseSpecName(string fileName, string expectedSpecName)
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: SimpleHandler,
            additionalFiles: [(fileName, MinimalPaymentYaml)]);

        // The handler base is generated at MinimalOpenApi.{SpecName}.GetPaymentEndpointBase.g.cs
        var handler = GeneratorTestHelper.GetGeneratedSource(result, $"{expectedSpecName}.GetPaymentEndpointBase.g.cs");
        Assert.That(handler, Does.Contain($"namespace TestProject.{expectedSpecName}.Endpoints;"));
    }

    [Test]
    public void ExplicitNamespaceMetadata_OverridesFileNameDerivedName()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: SimpleHandler,
            additionalFiles: [("openapi.yaml", MinimalPaymentYaml)],
            specNameOverride: "Payments");

        var handler = GeneratorTestHelper.GetGeneratedSource(result, "Payments.GetPaymentEndpointBase.g.cs");
        Assert.That(handler, Does.Contain("namespace TestProject.Payments.Endpoints;"));
    }
}