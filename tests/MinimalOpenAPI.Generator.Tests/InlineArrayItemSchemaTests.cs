namespace MinimalOpenAPI.Generator.Tests;

/// <summary>
/// Tests for inline object schemas used as <c>array.items</c>.
/// The generator must produce a nested record for each such schema and use
/// that record type in the array property instead of falling back to <c>object[]</c>.
/// </summary>
[TestFixture]
public class InlineArrayItemSchemaTests
{
    /// <summary>
    /// Inline 200 response object whose <c>items</c> array property has an inline item schema.
    /// </summary>
    [TestFixture]
    public class InlineResponseWithArrayItemSchema
    {
        private static readonly (string, string)[] AdditionalFiles =
        [
            ("openapi.yaml", OpenApiFixtures.GetRecipesWithInlineArrayItemYaml)
        ];

        private const string HandlerImpl = """
            public class GetRecipesHandler : GetRecipesEndpointBase
            {
                public override System.Threading.Tasks.Task<
                    global::Microsoft.AspNetCore.Http.HttpResults.Ok<OkResponse>> HandleAsync(
                    System.Threading.CancellationToken ct) => throw new System.NotImplementedException();
            }
            """;

        [Test]
        public void ArrayItemInlineObjectProducesNestedRecord()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: HandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "GetRecipesEndpointBase.g.cs");

            Assert.That(source, Does.Contain("public sealed record OkResponseItemsItem"));
        }

        [Test]
        public void ArrayItemRecordContainsCorrectProperties()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: HandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "GetRecipesEndpointBase.g.cs");

            Assert.That(source, Does.Contain("[global::System.Text.Json.Serialization.JsonPropertyName(\"id\")]"));
            Assert.That(source, Does.Contain("[global::System.Text.Json.Serialization.JsonPropertyName(\"name\")]"));
            Assert.That(source, Does.Contain("public required global::System.Guid Id { get; init; }"));
            Assert.That(source, Does.Contain("public required string Name { get; init; }"));
        }

        [Test]
        public void ParentResponseRecordUsesGeneratedItemType()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: HandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "GetRecipesEndpointBase.g.cs");

            // The 'items' property must use OkResponseItemsItem[], not object[].
            Assert.That(source, Does.Contain("public required OkResponseItemsItem[] Items { get; init; }"));
        }

        [Test]
        public void ArrayItemRecordIsEmittedBeforeResponseRecord()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: HandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "GetRecipesEndpointBase.g.cs");

            var itemIndex = source.IndexOf("public sealed record OkResponseItemsItem", StringComparison.Ordinal);
            var responseIndex = source.IndexOf($"public sealed record OkResponse{Environment.NewLine}", StringComparison.Ordinal);

            Assert.That(itemIndex, Is.LessThan(responseIndex),
                "OkResponseItemsItem must be declared before OkResponse so it is in scope.");
        }
    }

    /// <summary>
    /// Inline request body whose <c>entries</c> array property has an inline item schema.
    /// </summary>
    [TestFixture]
    public class InlineRequestBodyWithArrayItemSchema
    {
        private static readonly (string, string)[] AdditionalFiles =
        [
            ("openapi.yaml", OpenApiFixtures.BatchCreateWithInlineArrayItemYaml)
        ];

        private const string HandlerImpl = """
            public class BatchCreateHandler : BatchCreateEndpointBase
            {
                public override System.Threading.Tasks.Task<
                    global::Microsoft.AspNetCore.Http.HttpResults.Ok> HandleAsync(
                    Request request,
                    System.Threading.CancellationToken ct) => throw new System.NotImplementedException();
            }
            """;

        [Test]
        public void ArrayItemInlineObjectProducesNestedRecord()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: HandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "BatchCreateEndpointBase.g.cs");

            Assert.That(source, Does.Contain("public sealed record RequestEntriesItem"));
        }

        [Test]
        public void ArrayItemRecordContainsCorrectProperties()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: HandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "BatchCreateEndpointBase.g.cs");

            Assert.That(source, Does.Contain("public required string Name { get; init; }"));
        }

        [Test]
        public void RequestRecordUsesGeneratedItemType()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: HandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "BatchCreateEndpointBase.g.cs");

            Assert.That(source, Does.Contain("public required RequestEntriesItem[] Entries { get; init; }"));
        }

        [Test]
        public void ArrayItemRecordIsEmittedBeforeRequestRecord()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: HandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "BatchCreateEndpointBase.g.cs");

            var itemIndex = source.IndexOf("public sealed record RequestEntriesItem", StringComparison.Ordinal);
            var requestIndex = source.IndexOf($"public sealed record Request{Environment.NewLine}", StringComparison.Ordinal);

            Assert.That(itemIndex, Is.LessThan(requestIndex),
                "RequestEntriesItem must be declared before Request so it is in scope.");
        }
    }

    /// <summary>
    /// Array item schema itself contains a nested inline object property.
    /// Verifies recursive collection through the array-item boundary.
    /// </summary>
    [TestFixture]
    public class ArrayItemWithNestedInlineObjectProperty
    {
        private static readonly (string, string)[] AdditionalFiles =
        [
            ("openapi.yaml", OpenApiFixtures.GetItemsWithNestedInlineObjectInArrayItemYaml)
        ];

        private const string HandlerImpl = """
            public class GetItemsHandler : GetItemsEndpointBase
            {
                public override System.Threading.Tasks.Task<
                    global::Microsoft.AspNetCore.Http.HttpResults.Ok<OkResponse>> HandleAsync(
                    System.Threading.CancellationToken ct) => throw new System.NotImplementedException();
            }
            """;

        [Test]
        public void ArrayItemRecordIsGenerated()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: HandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "GetItemsEndpointBase.g.cs");

            Assert.That(source, Does.Contain("public sealed record OkResponseItemsItem"));
        }

        [Test]
        public void NestedInlineObjectInsideArrayItemProducesRecord()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: HandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "GetItemsEndpointBase.g.cs");

            Assert.That(source, Does.Contain("public sealed record OkResponseItemsItemMetadata"));
        }

        [Test]
        public void ArrayItemRecordUsesNestedMetadataType()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: HandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "GetItemsEndpointBase.g.cs");

            Assert.That(source, Does.Contain("public required OkResponseItemsItemMetadata Metadata { get; init; }"));
        }

        [Test]
        public void DeclarationOrderIsDeepesDependencyFirst()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: HandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "GetItemsEndpointBase.g.cs");

            var metaIndex = source.IndexOf("public sealed record OkResponseItemsItemMetadata", StringComparison.Ordinal);
            var itemIndex = source.IndexOf($"public sealed record OkResponseItemsItem{Environment.NewLine}", StringComparison.Ordinal);
            var responseIndex = source.IndexOf($"public sealed record OkResponse{Environment.NewLine}", StringComparison.Ordinal);

            Assert.That(metaIndex, Is.LessThan(itemIndex),
                "OkResponseItemsItemMetadata must appear before OkResponseItemsItem.");
            Assert.That(itemIndex, Is.LessThan(responseIndex),
                "OkResponseItemsItem must appear before OkResponse.");
        }
    }

    /// <summary>
    /// Array item schema contains a nested array property whose item is also an inline object.
    /// Verifies recursive collection through two array-item boundaries.
    /// </summary>
    [TestFixture]
    public class ArrayItemWithNestedArrayProperty
    {
        private static readonly (string, string)[] AdditionalFiles =
        [
            ("openapi.yaml", OpenApiFixtures.GetItemsWithNestedArrayInArrayItemYaml)
        ];

        private const string HandlerImpl = """
            public class GetItemsHandler : GetItemsEndpointBase
            {
                public override System.Threading.Tasks.Task<
                    global::Microsoft.AspNetCore.Http.HttpResults.Ok<OkResponse>> HandleAsync(
                    System.Threading.CancellationToken ct) => throw new System.NotImplementedException();
            }
            """;

        [Test]
        public void TopLevelArrayItemRecordIsGenerated()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: HandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "GetItemsEndpointBase.g.cs");

            Assert.That(source, Does.Contain("public sealed record OkResponseItemsItem"));
        }

        [Test]
        public void NestedArrayItemRecordIsGenerated()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: HandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "GetItemsEndpointBase.g.cs");

            Assert.That(source, Does.Contain("public sealed record OkResponseItemsItemStepsItem"));
        }

        [Test]
        public void ArrayItemRecordUsesNestedStepsItemType()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: HandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "GetItemsEndpointBase.g.cs");

            Assert.That(source, Does.Contain("public required OkResponseItemsItemStepsItem[] Steps { get; init; }"));
        }

        [Test]
        public void DeclarationOrderIsDeepestFirst()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: HandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "GetItemsEndpointBase.g.cs");

            var stepsItemIndex = source.IndexOf("public sealed record OkResponseItemsItemStepsItem", StringComparison.Ordinal);
            var itemIndex = source.IndexOf($"public sealed record OkResponseItemsItem{Environment.NewLine}", StringComparison.Ordinal);
            var responseIndex = source.IndexOf($"public sealed record OkResponse{Environment.NewLine}", StringComparison.Ordinal);

            Assert.That(stepsItemIndex, Is.LessThan(itemIndex),
                "OkResponseItemsItemStepsItem must appear before OkResponseItemsItem.");
            Assert.That(itemIndex, Is.LessThan(responseIndex),
                "OkResponseItemsItem must appear before OkResponse.");
        }
    }

    /// <summary>
    /// Component-level schema with an array property whose item schema is an inline object.
    /// Verifies that <see cref="DtoGenerator"/> also handles array item inline schemas.
    /// </summary>
    [TestFixture]
    public class ComponentSchemaWithInlineArrayItemSchema
    {
        private static readonly (string, string)[] AdditionalFiles =
        [
            ("openapi.yaml", OpenApiFixtures.GetCatalogWithInlineArrayItemComponentYaml)
        ];

        private const string HandlerImpl = """
            public class GetCatalogHandler : GetCatalogEndpointBase
            {
                public override System.Threading.Tasks.Task<
                    global::Microsoft.AspNetCore.Http.HttpResults.Ok<global::TestProject.Openapi.Contracts.Catalog>> HandleAsync(
                    System.Threading.CancellationToken ct) => throw new System.NotImplementedException();
            }
            """;

        [Test]
        public void ArrayItemInlineObjectProducesTopLevelRecord()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: HandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

            Assert.That(source, Does.Contain("public sealed record CatalogEntriesItem"));
        }

        [Test]
        public void ArrayItemRecordContainsCorrectProperties()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: HandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

            Assert.That(source, Does.Contain("public required global::System.Guid Id { get; init; }"));
            Assert.That(source, Does.Contain("public required string Title { get; init; }"));
        }

        [Test]
        public void CatalogRecordUsesGeneratedItemType()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: HandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

            Assert.That(source, Does.Contain("public required CatalogEntriesItem[] Entries { get; init; }"));
        }

        [Test]
        public void ArrayItemRecordIsEmittedBeforeParentRecord()
        {
            var (result, _) = GeneratorTestHelper.RunGenerator(
                userSource: HandlerImpl,
                additionalFiles: AdditionalFiles);

            var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

            var itemIndex = source.IndexOf("public sealed record CatalogEntriesItem", StringComparison.Ordinal);
            var catalogIndex = source.IndexOf($"public sealed record Catalog{Environment.NewLine}", StringComparison.Ordinal);

            Assert.That(itemIndex, Is.LessThan(catalogIndex),
                "CatalogEntriesItem must be declared before Catalog so it is in scope.");
        }
    }
}