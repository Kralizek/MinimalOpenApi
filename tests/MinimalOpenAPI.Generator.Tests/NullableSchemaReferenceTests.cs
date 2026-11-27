using System.Reflection;
using System.Text.Json;

using Microsoft.CodeAnalysis;

namespace MinimalOpenAPI.Generator.Tests;

/// <summary>
/// Generated nullability of properties that reference component schemas (#107): optionality comes
/// from the containing object's <c>required</c> list, and explicit nullability from the referenced
/// component (OpenAPI 3.0 <c>nullable: true</c> or an OpenAPI 3.1 <c>"null"</c> type).
/// </summary>
[TestFixture]
public class NullableSchemaReferenceTests
{
    private const string Contract30 = """
        openapi: "3.0.3"
        info: { title: Status, version: "1" }
        paths:
          /status:
            get:
              operationId: getStatus
              responses:
                "200":
                  description: Status.
                  content:
                    application/json:
                      schema: { $ref: '#/components/schemas/Status' }
        components:
          schemas:
            Status:
              type: object
              required: [requiredNullable, requiredPlain, requiredNullableKind]
              properties:
                optionalNullable: { $ref: '#/components/schemas/Failure' }
                optionalPlain: { $ref: '#/components/schemas/Detail' }
                requiredNullable: { $ref: '#/components/schemas/Failure' }
                requiredPlain: { $ref: '#/components/schemas/Detail' }
                requiredNullableKind: { $ref: '#/components/schemas/FailureKind' }
            Failure:
              type: object
              nullable: true
              required: [code]
              properties:
                code: { type: string }
            Detail:
              type: object
              required: [message]
              properties:
                message: { type: string }
            FailureKind:
              type: string
              nullable: true
              enum: [transient, fatal]
        """;

    private static readonly string Contract31 = Contract30
        .Replace("openapi: \"3.0.3\"", "openapi: \"3.1.0\"")
        .Replace("""
                  type: object
                  nullable: true
            """, """
                  type: [object, "null"]
            """)
        .Replace("""
                  type: string
                  nullable: true
            """, """
                  type: [string, "null"]
            """);

    private static string Dtos(string contract)
    {
        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", contract)]);
        Assert.That(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error), Is.Empty);
        return GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");
    }

    private static string StatusRecord(string dtos)
    {
        var start = dtos.IndexOf("public sealed record Status", StringComparison.Ordinal);
        return dtos[start..dtos.IndexOf("\n}", start, StringComparison.Ordinal)];
    }

    [Test]
    public void Contract31Fixture_UsesNullTypeArrays()
    {
        Assert.That(Contract31, Does.Contain("type: [object, \"null\"]").And.Contain("type: [string, \"null\"]"));
    }

    [TestCase("3.0")]
    [TestCase("3.1")]
    public void OptionalReferenceToNullableComponent_IsNullableAndNotRequired(string version)
    {
        var status = StatusRecord(Dtos(version == "3.0" ? Contract30 : Contract31));

        Assert.That(status, Does.Contain("public Failure? OptionalNullable { get; init; }"));
    }

    [TestCase("3.0")]
    [TestCase("3.1")]
    public void OptionalReferenceToNonNullableComponent_IsNullableAndNotRequired(string version)
    {
        var status = StatusRecord(Dtos(version == "3.0" ? Contract30 : Contract31));

        Assert.That(status, Does.Contain("public Detail? OptionalPlain { get; init; }"));
    }

    [TestCase("3.0")]
    [TestCase("3.1")]
    public void RequiredReferenceToNullableComponent_IsNullable(string version)
    {
        var status = StatusRecord(Dtos(version == "3.0" ? Contract30 : Contract31));

        Assert.Multiple(() =>
        {
            Assert.That(status, Does.Contain("public Failure? RequiredNullable { get; init; }"));
            Assert.That(status, Does.Contain("public FailureKind? RequiredNullableKind { get; init; }"),
                "a nullable enum component maps to a nullable value type");
        });
    }

    [TestCase("3.0")]
    [TestCase("3.1")]
    public void RequiredReferenceToNonNullableComponent_IsRequiredAndNotNullable(string version)
    {
        var status = StatusRecord(Dtos(version == "3.0" ? Contract30 : Contract31));

        Assert.That(status, Does.Contain("public required Detail RequiredPlain { get; init; }"));
    }

    [Test]
    public void InlineOperationRecord_RequiredReferenceToNullableComponent_IsNullable()
    {
        var contract = Contract30.Replace(
            "schema: { $ref: '#/components/schemas/Status' }",
            """
            schema:
                                    type: object
                                    required: [failure, detail]
                                    properties:
                                      failure: { $ref: '#/components/schemas/Failure' }
                                      detail: { $ref: '#/components/schemas/Detail' }
            """);
        var (result, _) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", contract)]);
        var handler = GeneratorTestHelper.GetGeneratedSource(result, "GetStatusEndpointBase.g.cs");

        Assert.Multiple(() =>
        {
            Assert.That(handler, Does.Contain("public global::TestProject.Openapi.Contracts.Failure? Failure { get; init; }"));
            Assert.That(handler, Does.Contain("public required global::TestProject.Openapi.Contracts.Detail Detail { get; init; }"));
        });
    }

    [Test]
    public void Serialization_RoundTripsMissingNullAndPresentValues()
    {
        var (_, outputCompilation) = GeneratorTestHelper.RunGenerator("", [("openapi.yaml", Contract30)]);
        var statusType = LoadGeneratedType(outputCompilation, "TestProject.Openapi.Contracts.Status");
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { RespectNullableAnnotations = true };

        // Explicit nulls for nullable references, optional properties missing.
        var withNulls = JsonSerializer.Deserialize(
            """{"requiredNullable":null,"requiredPlain":{"message":"m"},"requiredNullableKind":null}""", statusType, options)!;
        Assert.Multiple(() =>
        {
            Assert.That(Property(withNulls, "OptionalNullable"), Is.Null);
            Assert.That(Property(withNulls, "OptionalPlain"), Is.Null);
            Assert.That(Property(withNulls, "RequiredNullable"), Is.Null);
            Assert.That(Property(withNulls, "RequiredNullableKind"), Is.Null);
        });

        // Present values.
        var present = JsonSerializer.Deserialize(
            """{"optionalNullable":{"code":"a"},"requiredNullable":{"code":"b"},"requiredPlain":{"message":"m"},"requiredNullableKind":"fatal"}""", statusType, options)!;
        Assert.Multiple(() =>
        {
            Assert.That(Property(Property(present, "OptionalNullable")!, "Code"), Is.EqualTo("a"));
            Assert.That(Property(Property(present, "RequiredNullable")!, "Code"), Is.EqualTo("b"));
            Assert.That(Property(present, "RequiredNullableKind")?.ToString(), Is.EqualTo("Fatal"));
        });

        // A required, non-nullable reference still rejects a missing value and an explicit null.
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(
            """{"requiredNullable":null,"requiredNullableKind":null}""", statusType, options));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(
            """{"requiredNullable":null,"requiredPlain":null,"requiredNullableKind":null}""", statusType, options));
    }

    private static object? Property(object instance, string name)
        => instance.GetType().GetProperty(name)!.GetValue(instance);

    private static Type LoadGeneratedType(Compilation outputCompilation, string typeName)
    {
        var errors = GeneratorTestHelper.GetCompilationErrors(outputCompilation);
        Assert.That(errors, Is.Empty, string.Join(Environment.NewLine, errors));

        using var stream = new MemoryStream();
        var emit = GeneratorTestHelper.WithFrameworkReferences(outputCompilation).Emit(stream);
        Assert.That(emit.Success, Is.True, string.Join(Environment.NewLine, emit.Diagnostics));

        var assembly = Assembly.Load(stream.ToArray());
        return assembly.GetType(typeName, throwOnError: true)!;
    }
}