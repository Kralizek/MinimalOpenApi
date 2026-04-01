namespace MinimalOpenAPI.Generator.Tests;

/// <summary>Tests that <c>format: date</c> schemas are mapped to <c>global::System.DateOnly</c>.</summary>
[TestFixture]
public class DateOnlyMappingTests
{
    [Test]
    public void YamlDateFieldMapsToDateOnly()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetEventHandlerImpl,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.GetEventYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("public required global::System.DateOnly Date { get; init; }"));
    }

    [Test]
    public void YamlNullableDateFieldMapsToNullableDateOnly()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetEventHandlerImpl,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.GetEventYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("public global::System.DateOnly? Notes { get; init; }"));
    }

    [Test]
    public void JsonDateFieldMapsToDateOnly()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetEventHandlerImpl,
            additionalFiles: [("openapi.json", OpenApiFixtures.GetEventJson)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("public required global::System.DateOnly Date { get; init; }"));
    }

    [Test]
    public void JsonNullableDateFieldMapsToNullableDateOnly()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: GetEventHandlerImpl,
            additionalFiles: [("openapi.json", OpenApiFixtures.GetEventJson)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("public global::System.DateOnly? Notes { get; init; }"));
    }

    private const string GetEventHandlerImpl = """
        public class GetEventHandler : GetEventEndpointBase
        {
            public override System.Threading.Tasks.Task<object> Handle(
                global::System.Guid eventId,
                System.Threading.CancellationToken ct) => throw new System.NotImplementedException();
        }
        """;
}