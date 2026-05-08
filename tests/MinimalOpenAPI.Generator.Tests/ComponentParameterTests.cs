namespace MinimalOpenAPI.Generator.Tests;

/// <summary>Tests for generation using reusable <c>components/parameters</c> references.</summary>
[TestFixture]
public class ComponentParameterTests
{
    // ── YAML tests ────────────────────────────────────────────────────────

    [Test]
    public void Yaml_ComponentRef_PathParameter_AppearsInHandlerSignature()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", OpenApiFixtures.GetLeadsWithComponentParametersYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetLeadsEndpointBase.g.cs");

        Assert.That(source, Does.Contain("System.Guid providerId"),
            "resolved path parameter from $ref should appear in handler signature");
    }

    [Test]
    public void Yaml_ComponentRef_InlineParameterPreservedAfterRef()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", OpenApiFixtures.GetLeadsWithComponentParametersYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetLeadsEndpointBase.g.cs");

        Assert.That(source, Does.Contain("int? Page"),
            "inline query parameter declared after $ref should still appear in the Parameters record");
        Assert.That(source, Does.Contain("FromQuery(Name = \"page\")"),
            "inline query parameter should carry a FromQuery attribute with the original name");
    }

    [Test]
    public void Yaml_ComponentRef_GeneratesRouteWithConstraint()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", OpenApiFixtures.GetLeadsWithComponentParametersYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");

        Assert.That(source, Does.Contain("/providers/{providerId:guid}/leads"),
            "route should include the :guid constraint for the resolved path parameter");
    }

    [Test]
    public void Yaml_MissingComponentRef_EmitsMoa008()
    {
        const string spec = """
            openapi: "3.0.0"
            info: { title: Test, version: "1.0.0" }
            paths:
              /items:
                get:
                  operationId: listItems
                  parameters:
                    - $ref: '#/components/parameters/Missing'
                  responses:
                    "200":
                      description: OK
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", spec)]);

        Assert.That(result.Diagnostics, Has.Some.Matches<Microsoft.CodeAnalysis.Diagnostic>(d =>
            d.Id == "MOA008"),
            "a $ref to a non-existent component parameter should emit MOA008");
    }

    [Test]
    public void Yaml_ExternalRef_EmitsMoa008()
    {
        const string spec = """
            openapi: "3.0.0"
            info: { title: Test, version: "1.0.0" }
            paths:
              /items:
                get:
                  operationId: listItems
                  parameters:
                    - $ref: './common.yaml#/components/parameters/Page'
                  responses:
                    "200":
                      description: OK
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", spec)]);

        Assert.That(result.Diagnostics, Has.Some.Matches<Microsoft.CodeAnalysis.Diagnostic>(d =>
            d.Id == "MOA008"),
            "an external $ref should emit MOA008");
    }

    // ── JSON tests ────────────────────────────────────────────────────────

    [Test]
    public void Json_ComponentRef_PathParameter_AppearsInHandlerSignature()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.json", OpenApiFixtures.GetLeadsWithComponentParametersJson)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetLeadsEndpointBase.g.cs");

        Assert.That(source, Does.Contain("System.Guid providerId"),
            "resolved path parameter from $ref should appear in handler signature");
    }

    [Test]
    public void Json_ComponentRef_InlineParameterPreservedAfterRef()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.json", OpenApiFixtures.GetLeadsWithComponentParametersJson)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetLeadsEndpointBase.g.cs");

        Assert.That(source, Does.Contain("int? Page"),
            "inline query parameter declared after $ref should still appear in the Parameters record");
        Assert.That(source, Does.Contain("FromQuery(Name = \"page\")"),
            "inline query parameter should carry a FromQuery attribute with the original name");
    }

    [Test]
    public void Json_ComponentRef_GeneratesRouteWithConstraint()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.json", OpenApiFixtures.GetLeadsWithComponentParametersJson)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");

        Assert.That(source, Does.Contain("/providers/{providerId:guid}/leads"),
            "route should include the :guid constraint for the resolved path parameter");
    }

    [Test]
    public void Json_MissingComponentRef_EmitsMoa008()
    {
        const string spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0.0" },
              "paths": {
                "/items": {
                  "get": {
                    "operationId": "listItems",
                    "parameters": [
                      { "$ref": "#/components/parameters/Missing" }
                    ],
                    "responses": { "200": { "description": "OK" } }
                  }
                }
              }
            }
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.json", spec)]);

        Assert.That(result.Diagnostics, Has.Some.Matches<Microsoft.CodeAnalysis.Diagnostic>(d =>
            d.Id == "MOA008"),
            "a $ref to a non-existent component parameter should emit MOA008");
    }

    [Test]
    public void Json_ExternalRef_EmitsMoa008()
    {
        const string spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0.0" },
              "paths": {
                "/items": {
                  "get": {
                    "operationId": "listItems",
                    "parameters": [
                      { "$ref": "./common.json#/components/parameters/Page" }
                    ],
                    "responses": { "200": { "description": "OK" } }
                  }
                }
              }
            }
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.json", spec)]);

        Assert.That(result.Diagnostics, Has.Some.Matches<Microsoft.CodeAnalysis.Diagnostic>(d =>
            d.Id == "MOA008"),
            "an external $ref should emit MOA008");
    }

    // ── Path-level parameters ──────────────────────────────────────────────

    [Test]
    public void Yaml_PathLevelParameters_AreAppliedToAllOperations_WithOverrideAndOrder()
    {
        const string spec = """
            openapi: "3.0.0"
            info: { title: Test, version: "1.0.0" }
            paths:
              /stores/{storeId}/orders:
                parameters:
                  - $ref: '#/components/parameters/StoreId'
                  - name: includeArchived
                    in: query
                    required: false
                    schema:
                      type: string
                  - name: region
                    in: query
                    required: false
                    schema:
                      type: string
                get:
                  operationId: listOrders
                  parameters:
                    - name: includeArchived
                      in: query
                      required: false
                      schema:
                        type: boolean
                    - name: page
                      in: query
                      required: false
                      schema:
                        type: integer
                  responses:
                    "200":
                      description: OK
                post:
                  operationId: createOrder
                  responses:
                    "201":
                      description: Created
            components:
              parameters:
                StoreId:
                  name: storeId
                  in: path
                  required: true
                  schema:
                    type: string
                    format: uuid
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", spec)]);

        var listOrders = GeneratorTestHelper.GetGeneratedSource(result, "ListOrdersEndpointBase.g.cs");
        var createOrder = GeneratorTestHelper.GetGeneratedSource(result, "CreateOrderEndpointBase.g.cs");
        var mapping = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");

        Assert.That(listOrders, Does.Contain("System.Guid storeId"));
        Assert.That(createOrder, Does.Contain("System.Guid storeId"));
        Assert.That(mapping, Does.Contain("/stores/{storeId:guid}/orders"));

        Assert.That(listOrders, Does.Contain("public bool? IncludeArchived { get; init; }"),
            "operation-level includeArchived should override path-level includeArchived by (name, in)");
        Assert.That(createOrder, Does.Contain("public string? IncludeArchived { get; init; }"),
            "path-level includeArchived should still apply to operations without overrides");
        Assert.That(listOrders, Does.Contain("public string? Region { get; init; }"));
        Assert.That(createOrder, Does.Contain("public string? Region { get; init; }"));

        var includeArchivedIndex = listOrders.IndexOf("public bool? IncludeArchived { get; init; }", StringComparison.Ordinal);
        var regionIndex = listOrders.IndexOf("public string? Region { get; init; }", StringComparison.Ordinal);
        var pageIndex = listOrders.IndexOf("public int? Page { get; init; }", StringComparison.Ordinal);
        Assert.That(includeArchivedIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(regionIndex, Is.GreaterThan(includeArchivedIndex));
        Assert.That(pageIndex, Is.GreaterThan(regionIndex),
            "merged order should keep path slots first and append additional operation-level parameters");
    }

    [Test]
    public void Json_PathLevelParameters_AreAppliedToAllOperations_WithOverrideAndOrder()
    {
        const string spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0.0" },
              "paths": {
                "/stores/{storeId}/orders": {
                  "parameters": [
                    { "$ref": "#/components/parameters/StoreId" },
                    { "name": "includeArchived", "in": "query", "required": false, "schema": { "type": "string" } },
                    { "name": "region", "in": "query", "required": false, "schema": { "type": "string" } }
                  ],
                  "get": {
                    "operationId": "listOrders",
                    "parameters": [
                      { "name": "includeArchived", "in": "query", "required": false, "schema": { "type": "boolean" } },
                      { "name": "page", "in": "query", "required": false, "schema": { "type": "integer" } }
                    ],
                    "responses": { "200": { "description": "OK" } }
                  },
                  "post": {
                    "operationId": "createOrder",
                    "responses": { "201": { "description": "Created" } }
                  }
                }
              },
              "components": {
                "parameters": {
                  "StoreId": {
                    "name": "storeId",
                    "in": "path",
                    "required": true,
                    "schema": { "type": "string", "format": "uuid" }
                  }
                }
              }
            }
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.json", spec)]);

        var listOrders = GeneratorTestHelper.GetGeneratedSource(result, "ListOrdersEndpointBase.g.cs");
        var createOrder = GeneratorTestHelper.GetGeneratedSource(result, "CreateOrderEndpointBase.g.cs");
        var mapping = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");

        Assert.That(listOrders, Does.Contain("System.Guid storeId"));
        Assert.That(createOrder, Does.Contain("System.Guid storeId"));
        Assert.That(mapping, Does.Contain("/stores/{storeId:guid}/orders"));

        Assert.That(listOrders, Does.Contain("public bool? IncludeArchived { get; init; }"));
        Assert.That(createOrder, Does.Contain("public string? IncludeArchived { get; init; }"));
        Assert.That(listOrders, Does.Contain("public string? Region { get; init; }"));
        Assert.That(createOrder, Does.Contain("public string? Region { get; init; }"));

        var includeArchivedIndex = listOrders.IndexOf("public bool? IncludeArchived { get; init; }", StringComparison.Ordinal);
        var regionIndex = listOrders.IndexOf("public string? Region { get; init; }", StringComparison.Ordinal);
        var pageIndex = listOrders.IndexOf("public int? Page { get; init; }", StringComparison.Ordinal);
        Assert.That(includeArchivedIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(regionIndex, Is.GreaterThan(includeArchivedIndex));
        Assert.That(pageIndex, Is.GreaterThan(regionIndex));
    }

    [Test]
    public void Yaml_MissingPathLevelComponentRef_EmitsMoa008()
    {
        const string spec = """
            openapi: "3.0.0"
            info: { title: Test, version: "1.0.0" }
            paths:
              /libraries/{libraryId}/books:
                parameters:
                  - $ref: '#/components/parameters/Missing'
                get:
                  operationId: listBooks
                  responses:
                    "200":
                      description: OK
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", spec)]);

        Assert.That(result.Diagnostics, Has.Some.Matches<Microsoft.CodeAnalysis.Diagnostic>(d =>
            d.Id == "MOA008"));
    }

    [Test]
    public void Json_ExternalPathLevelComponentRef_EmitsMoa008()
    {
        const string spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0.0" },
              "paths": {
                "/libraries/{libraryId}/books": {
                  "parameters": [
                    { "$ref": "./common.json#/components/parameters/LibraryId" }
                  ],
                  "get": {
                    "operationId": "listBooks",
                    "responses": { "200": { "description": "OK" } }
                  }
                }
              }
            }
            """;

        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.json", spec)]);

        Assert.That(result.Diagnostics, Has.Some.Matches<Microsoft.CodeAnalysis.Diagnostic>(d =>
            d.Id == "MOA008"));
    }

    // ── Non-path component parameter tests ───────────────────────────────
    //
    // These tests verify that query, header and cookie parameters defined under
    // components/parameters resolve correctly and receive the right binding attributes.

    [Test]
    public void Yaml_QueryComponentParameter_ResolvesWithFromQueryAttribute()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", OpenApiFixtures.SearchWithAllNonPathComponentParametersYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "SearchEndpointBase.g.cs");

        Assert.That(source, Does.Contain("int? Page"),
            "component query parameter should appear as int? Page in the Parameters record");
        Assert.That(source, Does.Contain("FromQuery(Name = \"page\")"),
            "component query parameter should carry a FromQuery binding attribute");
    }

    [Test]
    public void Yaml_HeaderComponentParameter_ResolvesWithFromHeaderAttribute()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", OpenApiFixtures.SearchWithAllNonPathComponentParametersYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "SearchEndpointBase.g.cs");

        Assert.That(source, Does.Contain("string? XCorrelationId"),
            "component header parameter should appear as string? XCorrelationId in the Parameters record");
        Assert.That(source, Does.Contain("FromHeader(Name = \"X-Correlation-Id\")"),
            "component header parameter should carry a FromHeader binding attribute with the original name");
    }

    [Test]
    public void Yaml_CookieComponentParameter_ResolvesWithoutBindingAttribute()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.yaml", OpenApiFixtures.SearchWithAllNonPathComponentParametersYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "SearchEndpointBase.g.cs");

        Assert.That(source, Does.Contain("string? Session"),
            "component cookie parameter should appear as string? Session in the Parameters record");
        Assert.That(source, Does.Not.Contain("FromCookie"),
            "cookie parameters have no dedicated binding attribute in ASP.NET Core minimal APIs");
    }

    [Test]
    public void Json_QueryComponentParameter_ResolvesWithFromQueryAttribute()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.json", OpenApiFixtures.SearchWithAllNonPathComponentParametersJson)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "SearchEndpointBase.g.cs");

        Assert.That(source, Does.Contain("int? Page"),
            "component query parameter should appear as int? Page in the Parameters record");
        Assert.That(source, Does.Contain("FromQuery(Name = \"page\")"),
            "component query parameter should carry a FromQuery binding attribute");
    }

    [Test]
    public void Json_HeaderComponentParameter_ResolvesWithFromHeaderAttribute()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.json", OpenApiFixtures.SearchWithAllNonPathComponentParametersJson)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "SearchEndpointBase.g.cs");

        Assert.That(source, Does.Contain("string? XCorrelationId"),
            "component header parameter should appear as string? XCorrelationId in the Parameters record");
        Assert.That(source, Does.Contain("FromHeader(Name = \"X-Correlation-Id\")"),
            "component header parameter should carry a FromHeader binding attribute with the original name");
    }

    [Test]
    public void Json_CookieComponentParameter_ResolvesWithoutBindingAttribute()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "// no handler",
            additionalFiles: [("openapi.json", OpenApiFixtures.SearchWithAllNonPathComponentParametersJson)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "SearchEndpointBase.g.cs");

        Assert.That(source, Does.Contain("string? Session"),
            "component cookie parameter should appear as string? Session in the Parameters record");
        Assert.That(source, Does.Not.Contain("FromCookie"),
            "cookie parameters have no dedicated binding attribute in ASP.NET Core minimal APIs");
    }
}