namespace MinimalOpenAPI.Generator.Tests;

/// <summary>
/// Tests for parameter-generation refinements:
/// <list type="bullet">
///   <item>#24 — default-value initializers on <c>Parameters</c> record properties.</item>
///   <item>#27 — <c>[EmailAddress]</c> / <c>[Url]</c> attributes for <c>format: email</c> / <c>format: uri</c>.</item>
///   <item>#30 — header-parameter casing: the declared name is preserved in <c>[FromHeader(Name = "...")]</c>.</item>
/// </list>
/// </summary>
[TestFixture]
public class ParameterRefinementTests
{
    private const string NoOpHandlerImpl = "// no handler needed";

    // ── YAML fixtures ─────────────────────────────────────────────────────

    private const string DefaultValuesYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /search:
            get:
              operationId: search
              parameters:
                - name: pageSize
                  in: query
                  required: false
                  schema:
                    type: integer
                    default: 20
                - name: page
                  in: query
                  required: false
                  schema:
                    type: integer
                    format: int64
                    default: 1
                - name: ratio
                  in: query
                  required: false
                  schema:
                    type: number
                    default: 0.5
                - name: score
                  in: query
                  required: false
                  schema:
                    type: number
                    format: float
                    default: 1.5
                - name: active
                  in: query
                  required: false
                  schema:
                    type: boolean
                    default: true
                - name: label
                  in: query
                  required: false
                  schema:
                    type: string
                    default: "all"
              responses:
                "200":
                  description: OK
        """;

    private const string StringFormatDefaultsYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /items:
            get:
              operationId: listItems
              parameters:
                - name: correlationId
                  in: query
                  required: false
                  schema:
                    type: string
                    format: uuid
                    default: "00000000-0000-0000-0000-000000000001"
                - name: since
                  in: query
                  required: false
                  schema:
                    type: string
                    format: date
                    default: "2024-01-01"
              responses:
                "200":
                  description: OK
        """;

    private const string NoDefaultYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /items:
            get:
              operationId: listItems
              parameters:
                - name: pageSize
                  in: query
                  required: false
                  schema:
                    type: integer
              responses:
                "200":
                  description: OK
        """;

    private const string EmailAndUriYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /users:
            get:
              operationId: listUsers
              parameters:
                - name: email
                  in: query
                  required: false
                  schema:
                    type: string
                    format: email
                - name: website
                  in: query
                  required: false
                  schema:
                    type: string
                    format: uri
              responses:
                "200":
                  description: OK
        """;

    private const string EmailAndUriDtoYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /users:
            post:
              operationId: createUser
              requestBody:
                required: true
                content:
                  application/json:
                    schema:
                      $ref: '#/components/schemas/CreateUserRequest'
              responses:
                "201":
                  description: Created
        components:
          schemas:
            CreateUserRequest:
              type: object
              required:
                - email
                - website
              properties:
                email:
                  type: string
                  format: email
                website:
                  type: string
                  format: uri
        """;

    private const string HeaderCasingYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /items:
            get:
              operationId: listItems
              parameters:
                - name: X-Correlation-Id
                  in: header
                  required: false
                  schema:
                    type: string
                - name: X-Request-Id
                  in: header
                  required: true
                  schema:
                    type: string
              responses:
                "200":
                  description: OK
        """;

    private const string HeaderDefaultYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /items:
            get:
              operationId: listItems
              parameters:
                - name: X-Api-Version
                  in: header
                  required: false
                  schema:
                    type: string
                    default: "v1"
              responses:
                "200":
                  description: OK
        """;

    // ── JSON fixtures ─────────────────────────────────────────────────────

    private const string DefaultValuesJson = """
        {
          "openapi": "3.0.0",
          "info": { "title": "Test API", "version": "1.0.0" },
          "paths": {
            "/search": {
              "get": {
                "operationId": "search",
                "parameters": [
                  {
                    "name": "pageSize",
                    "in": "query",
                    "required": false,
                    "schema": { "type": "integer", "default": 20 }
                  },
                  {
                    "name": "active",
                    "in": "query",
                    "required": false,
                    "schema": { "type": "boolean", "default": true }
                  },
                  {
                    "name": "label",
                    "in": "query",
                    "required": false,
                    "schema": { "type": "string", "default": "all" }
                  }
                ],
                "responses": { "200": { "description": "OK" } }
              }
            }
          }
        }
        """;

    // ── #24: Default value tests (YAML) ──────────────────────────────────

    [Test]
    public void IntegerQueryParameter_WithDefault_EmitsInitializer()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.yaml", DefaultValuesYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "SearchEndpointBase.g.cs");

        Assert.That(source, Does.Contain("PageSize { get; init; } = 20;"));
    }

    [Test]
    public void Int64QueryParameter_WithDefault_EmitsLongSuffixInitializer()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.yaml", DefaultValuesYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "SearchEndpointBase.g.cs");

        Assert.That(source, Does.Contain("Page { get; init; } = 1L;"));
    }

    [Test]
    public void NumberQueryParameter_WithDefault_EmitsDoubleInitializer()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.yaml", DefaultValuesYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "SearchEndpointBase.g.cs");

        Assert.That(source, Does.Contain("Ratio { get; init; } = 0.5;"));
    }

    [Test]
    public void FloatQueryParameter_WithDefault_EmitsFloatInitializer()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.yaml", DefaultValuesYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "SearchEndpointBase.g.cs");

        Assert.That(source, Does.Contain("Score { get; init; } = 1.5f;"));
    }

    [Test]
    public void BooleanQueryParameter_WithDefault_EmitsLiteralInitializer()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.yaml", DefaultValuesYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "SearchEndpointBase.g.cs");

        Assert.That(source, Does.Contain("Active { get; init; } = true;"));
    }

    [Test]
    public void StringQueryParameter_WithDefault_EmitsStringLiteralInitializer()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.yaml", DefaultValuesYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "SearchEndpointBase.g.cs");

        Assert.That(source, Does.Contain("Label { get; init; } = \"all\";"));
    }

    [Test]
    public void UuidQueryParameter_WithDefault_EmitsGuidParseInitializer()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.yaml", StringFormatDefaultsYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "ListItemsEndpointBase.g.cs");

        Assert.That(source, Does.Contain("CorrelationId { get; init; } = global::System.Guid.Parse("));
    }

    [Test]
    public void DateQueryParameter_WithDefault_EmitsDateOnlyParseInitializer()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.yaml", StringFormatDefaultsYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "ListItemsEndpointBase.g.cs");

        Assert.That(source, Does.Contain("Since { get; init; } = global::System.DateOnly.Parse(\"2024-01-01\""));
    }

    [Test]
    public void HeaderParameter_WithDefault_EmitsStringLiteralInitializer()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.yaml", HeaderDefaultYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "ListItemsEndpointBase.g.cs");

        Assert.That(source, Does.Contain("XApiVersion { get; init; } = \"v1\";"));
    }

    [Test]
    public void QueryParameter_WithNoDefault_EmitsNoInitializer()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.yaml", NoDefaultYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "ListItemsEndpointBase.g.cs");

        Assert.That(source, Does.Contain("PageSize { get; init; }"));
        Assert.That(source, Does.Not.Contain("PageSize { get; init; } ="));
    }

    // ── #24: Default value tests (JSON) ──────────────────────────────────

    [Test]
    public void JsonParser_IntegerParameter_WithDefault_EmitsInitializer()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.json", DefaultValuesJson)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "SearchEndpointBase.g.cs");

        Assert.That(source, Does.Contain("PageSize { get; init; } = 20;"));
    }

    [Test]
    public void JsonParser_BooleanParameter_WithDefault_EmitsInitializer()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.json", DefaultValuesJson)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "SearchEndpointBase.g.cs");

        Assert.That(source, Does.Contain("Active { get; init; } = true;"));
    }

    [Test]
    public void JsonParser_StringParameter_WithDefault_EmitsInitializer()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.json", DefaultValuesJson)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "SearchEndpointBase.g.cs");

        Assert.That(source, Does.Contain("Label { get; init; } = \"all\";"));
    }

    // ── #27: EmailAddress / Url attribute tests ───────────────────────────

    [Test]
    public void StringParameter_WithEmailFormat_EmitsEmailAddressAttribute()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.yaml", EmailAndUriYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "ListUsersEndpointBase.g.cs");

        Assert.That(source, Does.Contain("[global::System.ComponentModel.DataAnnotations.EmailAddress]"));
    }

    [Test]
    public void StringParameter_WithUriFormat_EmitsUrlAttribute()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.yaml", EmailAndUriYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "ListUsersEndpointBase.g.cs");

        Assert.That(source, Does.Contain("[global::System.ComponentModel.DataAnnotations.Url]"));
    }

    [Test]
    public void DtoProperty_WithEmailFormat_EmitsEmailAddressAttribute()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.yaml", EmailAndUriDtoYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("[global::System.ComponentModel.DataAnnotations.EmailAddress]"));
    }

    [Test]
    public void DtoProperty_WithUriFormat_EmitsUrlAttribute()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.yaml", EmailAndUriDtoYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(source, Does.Contain("[global::System.ComponentModel.DataAnnotations.Url]"));
    }

    [Test]
    public void StringParameter_WithNoFormat_DoesNotEmitEmailOrUrlAttribute()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.yaml", NoDefaultYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "ListItemsEndpointBase.g.cs");

        Assert.That(source, Does.Not.Contain("EmailAddress"));
        Assert.That(source, Does.Not.Contain("[global::System.ComponentModel.DataAnnotations.Url]"));
    }

    // ── #30: Header parameter casing tests ───────────────────────────────

    [Test]
    public void HeaderParameter_PreservesExactDeclaredNameInFromHeaderAttribute()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.yaml", HeaderCasingYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "ListItemsEndpointBase.g.cs");

        // The exact OpenAPI-declared name must appear in the [FromHeader] attribute.
        Assert.That(source, Does.Contain("[global::Microsoft.AspNetCore.Mvc.FromHeader(Name = \"X-Correlation-Id\")]"));
        Assert.That(source, Does.Contain("[global::Microsoft.AspNetCore.Mvc.FromHeader(Name = \"X-Request-Id\")]"));
    }

    [Test]
    public void HeaderParameter_PropertyNameIsPascalCase()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.yaml", HeaderCasingYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "ListItemsEndpointBase.g.cs");

        // The C# property name is PascalCase; the binding Name preserves the original.
        Assert.That(source, Does.Contain("XCorrelationId { get; init; }"));
        Assert.That(source, Does.Contain("XRequestId { get; init; }"));
    }

    [Test]
    public void HeaderParameter_RequiredAndOptional_TypingIsCorrect()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: NoOpHandlerImpl,
            additionalFiles: [("openapi.yaml", HeaderCasingYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "ListItemsEndpointBase.g.cs");

        // X-Correlation-Id is optional → string?
        Assert.That(source, Does.Contain("public string? XCorrelationId"));
        // X-Request-Id is required → string (non-nullable)
        Assert.That(source, Does.Contain("public string XRequestId"));
    }
}