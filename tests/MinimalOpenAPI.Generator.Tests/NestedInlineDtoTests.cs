namespace MinimalOpenAPI.Generator.Tests;

/// <summary>
/// Tests for generating top-level nested records when a component schema property is
/// itself an inline object (no <c>$ref</c>).
/// </summary>
[TestFixture]
public class NestedInlineDtoTests
{
    [TestFixture]
    public class SingleLevelNesting
    {
        private static readonly (string, string)[] AdditionalFiles =
        [
            ("openapi.yaml", OpenApiFixtures.GetOrderWithNestedAddressYaml)
        ];

        private const string GetOrderHandlerImpl = """
            public class GetOrderHandler : GetOrderEndpointBase
            {
                public override System.Threading.Tasks.Task<
                    global::Microsoft.AspNetCore.Http.HttpResults.Results<
                        global::Microsoft.AspNetCore.Http.HttpResults.Ok<global::TestProject.Openapi.Contracts.Order>,
                        global::Microsoft.AspNetCore.Http.HttpResults.NotFound>> HandleAsync(
                    global::System.Guid orderId,
                    System.Threading.CancellationToken ct) => throw new System.NotImplementedException();
            }
            """;

        [Test]
        public void InlineObjectPropertyGeneratesTopLevelNestedRecord()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: GetOrderHandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

            // The inline 'address' property of 'Order' should produce an 'OrderAddress' record.
            Assert.That(source, Does.Contain("public sealed record OrderAddress"));
        }

        [Test]
        public void NestedRecordContainsCorrectProperties()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: GetOrderHandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

            Assert.That(source, Does.Contain("[JsonPropertyName(\"street\")]"));
            Assert.That(source, Does.Contain("[JsonPropertyName(\"city\")]"));
            Assert.That(source, Does.Contain("public required string Street { get; init; }"));
            Assert.That(source, Does.Contain("public required string City { get; init; }"));
        }

        [Test]
        public void ParentRecordUsesNestedRecordTypeName()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: GetOrderHandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

            // The 'address' property in 'Order' should use 'OrderAddress' as its type, not 'object'.
            Assert.That(source, Does.Contain("public required OrderAddress Address { get; init; }"));
        }

        [Test]
        public void NestedRecordIsEmittedBeforeParentRecord()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: GetOrderHandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

            var nestedIndex = source.IndexOf("public sealed record OrderAddress", StringComparison.Ordinal);
            // Use a line-ending-terminated search to avoid matching "OrderAddress" when looking for "Order"
            var parentIndex = source.IndexOf($"public sealed record Order{Environment.NewLine}", StringComparison.Ordinal);

            Assert.That(nestedIndex, Is.LessThan(parentIndex),
                "OrderAddress must be declared before Order so it is in scope.");
        }

        [Test]
        public void ParentRecordIsAlsoGenerated()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: GetOrderHandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

            Assert.That(source, Does.Contain("public sealed record Order"));
            Assert.That(source, Does.Contain("[JsonPropertyName(\"id\")]"));
            Assert.That(source, Does.Contain("public required global::System.Guid Id { get; init; }"));
        }

        [Test]
        public void NullableInlineObjectPropertyIsNullable()
        {
            // 'note' is a plain string (not inline object), but 'address' is required inline object.
            // Verify that a non-required inline object property would be nullable.
            // In the fixture 'address' IS required, so its type should be 'OrderAddress' (not nullable).
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: GetOrderHandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

            // 'note' is a nullable string
            Assert.That(source, Does.Contain("public string? Note { get; init; }"));
        }
    }

    [TestFixture]
    public class DeepNesting
    {
        private static readonly (string, string)[] AdditionalFiles =
        [
            ("openapi.yaml", OpenApiFixtures.GetShipmentWithDeepNestingYaml)
        ];

        private const string GetShipmentHandlerImpl = """
            public class GetShipmentHandler : GetShipmentEndpointBase
            {
                public override System.Threading.Tasks.Task<
                    global::Microsoft.AspNetCore.Http.HttpResults.Results<
                        global::Microsoft.AspNetCore.Http.HttpResults.Ok<global::TestProject.Openapi.Contracts.Shipment>,
                        global::Microsoft.AspNetCore.Http.HttpResults.NotFound>> HandleAsync(
                    global::System.Guid shipmentId,
                    System.Threading.CancellationToken ct) => throw new System.NotImplementedException();
            }
            """;

        [Test]
        public void SecondLevelInlineObjectGeneratesRecord()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: GetShipmentHandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

            // Shipment.destination → ShipmentDestination
            // ShipmentDestination.address → ShipmentDestinationAddress
            Assert.That(source, Does.Contain("public sealed record ShipmentDestination"));
            Assert.That(source, Does.Contain("public sealed record ShipmentDestinationAddress"));
        }

        [Test]
        public void DeepNestedRecordsAreEmittedInDependencyOrder()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: GetShipmentHandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

            var addressIndex = source.IndexOf("public sealed record ShipmentDestinationAddress", StringComparison.Ordinal);
            // Use line-ending-terminated searches to avoid prefix collisions between names
            var destinationIndex = source.IndexOf($"public sealed record ShipmentDestination{Environment.NewLine}", StringComparison.Ordinal);
            var shipmentIndex = source.IndexOf($"public sealed record Shipment{Environment.NewLine}", StringComparison.Ordinal);

            Assert.That(addressIndex, Is.LessThan(destinationIndex),
                "ShipmentDestinationAddress must appear before ShipmentDestination.");
            Assert.That(destinationIndex, Is.LessThan(shipmentIndex),
                "ShipmentDestination must appear before Shipment.");
        }

        [Test]
        public void IntermediateRecordUsesCorrectTypeName()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: GetShipmentHandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

            // ShipmentDestination.address property should use ShipmentDestinationAddress type.
            Assert.That(source, Does.Contain("public required ShipmentDestinationAddress Address { get; init; }"));
        }

        [Test]
        public void TopLevelRecordUsesFirstLevelNestedTypeName()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: GetShipmentHandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

            // Shipment.destination should use ShipmentDestination type.
            Assert.That(source, Does.Contain("public required ShipmentDestination Destination { get; init; }"));
        }
    }

    [TestFixture]
    public class SnakeCasePropertyNames
    {
        private static readonly (string, string)[] AdditionalFiles =
        [
            ("openapi.yaml", OpenApiFixtures.GetInvoiceWithMixedCasePropertiesYaml)
        ];

        private const string GetInvoiceHandlerImpl = """
            public class GetInvoiceHandler : GetInvoiceEndpointBase
            {
                public override System.Threading.Tasks.Task<
                    global::Microsoft.AspNetCore.Http.HttpResults.Results<
                        global::Microsoft.AspNetCore.Http.HttpResults.Ok<global::TestProject.Openapi.Contracts.Invoice>,
                        global::Microsoft.AspNetCore.Http.HttpResults.NotFound>> HandleAsync(
                    global::System.Guid invoiceId,
                    System.Threading.CancellationToken ct) => throw new System.NotImplementedException();
            }
            """;

        [Test]
        public void SnakeCasePropertyNameBecomesPascalCaseCSharpIdentifier()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: GetInvoiceHandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

            // snake_case property names must produce PascalCase C# property identifiers.
            Assert.That(source, Does.Contain("public required global::System.Guid InvoiceId { get; init; }"));
            Assert.That(source, Does.Contain("public global::System.DateOnly? DueDate { get; init; }"));
        }

        [Test]
        public void SnakeCasePropertyPreservesJsonPropertyNameAttribute()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: GetInvoiceHandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

            // The original JSON property name must be kept verbatim in the attribute.
            Assert.That(source, Does.Contain("[JsonPropertyName(\"invoice_id\")]"));
            Assert.That(source, Does.Contain("[JsonPropertyName(\"billing_address\")]"));
            Assert.That(source, Does.Contain("[JsonPropertyName(\"due_date\")]"));
        }

        [Test]
        public void SnakeCaseInlineObjectPropertyProducesCorrectNestedRecordName()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: GetInvoiceHandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

            // 'billing_address' (snake_case) on 'Invoice' → derived name 'InvoiceBillingAddress'
            Assert.That(source, Does.Contain("public sealed record InvoiceBillingAddress"));
        }

        [Test]
        public void NestedRecordSnakeCasePropertiesArePascalCase()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: GetInvoiceHandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

            // Properties inside InvoiceBillingAddress use PascalCase identifiers.
            Assert.That(source, Does.Contain("public required string StreetName { get; init; }"));
            Assert.That(source, Does.Contain("public string? ZipCode { get; init; }"));
        }

        [Test]
        public void ParentRecordUsesNestedRecordTypeNameForSnakeCaseProperty()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: GetInvoiceHandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

            // Invoice.billing_address property type should be InvoiceBillingAddress, not object.
            Assert.That(source, Does.Contain("public required InvoiceBillingAddress BillingAddress { get; init; }"));
        }
    }
}