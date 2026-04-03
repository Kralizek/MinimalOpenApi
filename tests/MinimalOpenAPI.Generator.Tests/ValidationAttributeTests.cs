namespace MinimalOpenAPI.Generator.Tests;

/// <summary>
/// Tests verifying that OpenAPI constraint keywords (<c>minLength</c>, <c>maxLength</c>,
/// <c>pattern</c>, <c>minimum</c>, <c>maximum</c>, <c>minItems</c>, <c>maxItems</c>)
/// emit the correct <c>System.ComponentModel.DataAnnotations</c> attributes on generated
/// DTO properties and <c>Parameters</c> record properties.
/// </summary>
[TestFixture]
public class ValidationAttributeTests
{
    // ── YAML fixtures ─────────────────────────────────────────────────────

    private const string StringConstraintsYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /items:
            post:
              operationId: createItem
              requestBody:
                required: true
                content:
                  application/json:
                    schema:
                      $ref: '#/components/schemas/CreateItemRequest'
              responses:
                "201":
                  description: Created
        components:
          schemas:
            CreateItemRequest:
              type: object
              required:
                - name
                - code
                - notes
              properties:
                name:
                  type: string
                  minLength: 1
                  maxLength: 100
                code:
                  type: string
                  maxLength: 10
                shortTag:
                  type: string
                  minLength: 2
                notes:
                  type: string
                  pattern: '^[\w\s]+$'
        """;

    private const string NumericConstraintsYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /orders:
            post:
              operationId: createOrder
              requestBody:
                required: true
                content:
                  application/json:
                    schema:
                      $ref: '#/components/schemas/CreateOrderRequest'
              responses:
                "201":
                  description: Created
        components:
          schemas:
            CreateOrderRequest:
              type: object
              required:
                - quantity
                - price
                - score
              properties:
                quantity:
                  type: integer
                  minimum: 1
                  maximum: 1000
                price:
                  type: number
                  minimum: 0.01
                  maximum: 9999.99
                score:
                  type: integer
                  minimum: 0
        """;

    private const string ArrayConstraintsYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /batch:
            post:
              operationId: createBatch
              requestBody:
                required: true
                content:
                  application/json:
                    schema:
                      $ref: '#/components/schemas/CreateBatchRequest'
              responses:
                "201":
                  description: Created
        components:
          schemas:
            CreateBatchRequest:
              type: object
              required:
                - items
                - tags
                - labels
              properties:
                items:
                  type: array
                  minItems: 1
                  maxItems: 50
                  items:
                    type: string
                tags:
                  type: array
                  minItems: 1
                  items:
                    type: string
                labels:
                  type: array
                  maxItems: 20
                  items:
                    type: string
        """;

    private const string QueryParameterConstraintsYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /search:
            get:
              operationId: search
              parameters:
                - name: query
                  in: query
                  required: false
                  schema:
                    type: string
                    minLength: 1
                    maxLength: 200
                - name: pageSize
                  in: query
                  required: false
                  schema:
                    type: integer
                    minimum: 1
                    maximum: 100
                - name: filter
                  in: query
                  required: false
                  schema:
                    type: string
                    pattern: '^[a-z]+$'
              responses:
                "200":
                  description: OK
        """;

    private const string JsonStringConstraints = """
        {
          "openapi": "3.0.0",
          "info": { "title": "Test API", "version": "1.0.0" },
          "paths": {
            "/items": {
              "post": {
                "operationId": "createItem",
                "requestBody": {
                  "required": true,
                  "content": {
                    "application/json": {
                      "schema": { "$ref": "#/components/schemas/CreateItemRequest" }
                    }
                  }
                },
                "responses": { "201": { "description": "Created" } }
              }
            }
          },
          "components": {
            "schemas": {
              "CreateItemRequest": {
                "type": "object",
                "required": ["name"],
                "properties": {
                  "name": { "type": "string", "minLength": 1, "maxLength": 100 }
                }
              }
            }
          }
        }
        """;

    private const string NoOpHandlerImpl = "// no handler needed";

    // ── String constraint tests (YAML) ────────────────────────────────────

    [Test]
    public void StringPropertyWithBothMinAndMaxLengthEmitsStringLength()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.yaml", StringConstraintsYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("[global::System.ComponentModel.DataAnnotations.StringLength(100, MinimumLength = 1)]"));
    }

    [Test]
    public void StringPropertyWithOnlyMaxLengthEmitsMaxLength()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.yaml", StringConstraintsYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("[global::System.ComponentModel.DataAnnotations.MaxLength(10)]"));
    }

    [Test]
    public void StringPropertyWithOnlyMinLengthEmitsMinLength()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.yaml", StringConstraintsYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("[global::System.ComponentModel.DataAnnotations.MinLength(2)]"));
    }

    [Test]
    public void StringPropertyWithPatternEmitsRegularExpression()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.yaml", StringConstraintsYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("[global::System.ComponentModel.DataAnnotations.RegularExpression("));
        Assert.That(source, Does.Contain(@"[\w\s]+"));
    }

    // ── Numeric constraint tests (YAML) ───────────────────────────────────

    [Test]
    public void IntegerPropertyWithBothMinAndMaxEmitsRange()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.yaml", NumericConstraintsYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("[global::System.ComponentModel.DataAnnotations.Range(1, 1000)]"));
    }

    [Test]
    public void NumberPropertyWithBothMinAndMaxEmitsRange()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.yaml", NumericConstraintsYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("[global::System.ComponentModel.DataAnnotations.Range("));
        Assert.That(source, Does.Contain("0.01"));
        Assert.That(source, Does.Contain("9999.99"));
    }

    [Test]
    public void IntegerPropertyWithOnlyMinimumEmitsRangeWithIntMaxValue()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.yaml", NumericConstraintsYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("[global::System.ComponentModel.DataAnnotations.Range(0, int.MaxValue)]"));
    }

    // ── Array constraint tests (YAML) ─────────────────────────────────────

    [Test]
    public void ArrayPropertyWithBothMinAndMaxItemsEmitsBothAttributes()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.yaml", ArrayConstraintsYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("[global::System.ComponentModel.DataAnnotations.MinLength(1)]"));
        Assert.That(source, Does.Contain("[global::System.ComponentModel.DataAnnotations.MaxLength(50)]"));
    }

    [Test]
    public void ArrayPropertyWithOnlyMinItemsEmitsMinLength()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.yaml", ArrayConstraintsYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        // tags has only minItems: 1 (minLength for strings was already tested above)
        // Look for the pattern near the Tags property
        Assert.That(source, Does.Contain("[global::System.ComponentModel.DataAnnotations.MinLength(1)]"));
    }

    [Test]
    public void ArrayPropertyWithOnlyMaxItemsEmitsMaxLength()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.yaml", ArrayConstraintsYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("[global::System.ComponentModel.DataAnnotations.MaxLength(20)]"));
    }

    // ── Parameters record constraint tests (YAML) ─────────────────────────

    [Test]
    public void QueryStringParameterWithLengthConstraintsEmitsStringLength()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.yaml", QueryParameterConstraintsYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "SearchEndpointBase.g.cs");

        Assert.That(source, Does.Contain("[global::System.ComponentModel.DataAnnotations.StringLength(200, MinimumLength = 1)]"));
    }

    [Test]
    public void QueryIntegerParameterWithRangeEmitsRangeAttribute()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.yaml", QueryParameterConstraintsYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "SearchEndpointBase.g.cs");

        Assert.That(source, Does.Contain("[global::System.ComponentModel.DataAnnotations.Range(1, 100)]"));
    }

    [Test]
    public void QueryStringParameterWithPatternEmitsRegularExpression()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.yaml", QueryParameterConstraintsYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "SearchEndpointBase.g.cs");

        Assert.That(source, Does.Contain("[global::System.ComponentModel.DataAnnotations.RegularExpression("));
        Assert.That(source, Does.Contain("^[a-z]+$"));
    }

    // ── JSON parser parity test ───────────────────────────────────────────

    [Test]
    public void JsonParserEmitsStringLengthAttributeLikeYamlParser()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.json", JsonStringConstraints)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("[global::System.ComponentModel.DataAnnotations.StringLength(100, MinimumLength = 1)]"));
    }

    // ── No constraint: attributes must not appear ─────────────────────────

    [Test]
    public void PropertyWithNoConstraintsEmitsNoValidationAttributes()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.yaml", OpenApiFixtures.GetClientYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Not.Contain("DataAnnotations"));
    }
}