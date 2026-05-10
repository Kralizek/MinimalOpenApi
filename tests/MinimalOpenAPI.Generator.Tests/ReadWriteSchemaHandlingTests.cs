namespace MinimalOpenAPI.Generator.Tests;

[TestFixture]
public class ReadWriteSchemaHandlingTests
{
    [Test]
    public void Auto_RequestAndResponseDtos_FilterReadOnlyAndWriteOnlyProperties()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", DirectionalSpecYaml)]);

        var dtos = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");
        var userRequest = GetRecordBlock(dtos, "UserRequest");
        var userResponse = GetRecordBlock(dtos, "UserResponse");
        var profileRequest = GetRecordBlock(dtos, "ProfileRequest");
        var profileResponse = GetRecordBlock(dtos, "ProfileResponse");

        Assert.That(dtos, Does.Contain("public sealed record UserRequest"));
        Assert.That(dtos, Does.Contain("public sealed record UserResponse"));
        Assert.That(userRequest, Does.Contain("[JsonPropertyName(\"password\")]"));
        Assert.That(userRequest, Does.Not.Contain("public required global::System.Guid Id { get; init; }"));
        Assert.That(userResponse, Does.Not.Contain("public required string Password { get; init; }"));
        Assert.That(profileRequest, Does.Not.Contain("public required global::System.DateTimeOffset CreatedAt { get; init; }"));
        Assert.That(profileResponse, Does.Contain("public required global::System.DateTimeOffset CreatedAt { get; init; }"));
        Assert.That(dtos, Does.Contain("public sealed record ProfileRequest"));
        Assert.That(dtos, Does.Contain("public sealed record ProfileResponse"));
    }

    [Test]
    public void Auto_RemovesFilteredRequiredPropertiesFromScopedVariants()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", DirectionalSpecYaml)]);

        var dtos = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");
        var userRequest = GetRecordBlock(dtos, "UserRequest");
        var userResponse = GetRecordBlock(dtos, "UserResponse");

        Assert.That(userRequest, Does.Not.Contain("public required global::System.Guid Id { get; init; }"));
        Assert.That(userResponse, Does.Not.Contain("public required string Password { get; init; }"));
        Assert.That(userResponse, Does.Contain("public required global::System.Guid Id { get; init; }"));
        Assert.That(userRequest, Does.Contain("public required string Password { get; init; }"));
    }

    [Test]
    public void Auto_PropagatesDirectionalityThroughReferencesArraysAndDictionaries()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", DirectionalSpecYaml)]);

        var dtos = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");

        Assert.That(dtos, Does.Contain("public sealed record TagRequest"));
        Assert.That(dtos, Does.Contain("public sealed record TagResponse"));
        Assert.That(dtos, Does.Contain("public sealed record SecretValueRequest"));
        Assert.That(dtos, Does.Contain("public sealed record SecretValueResponse"));
        Assert.That(dtos, Does.Contain("global::System.Collections.Generic.Dictionary<string, SecretValueRequest>"));
        Assert.That(dtos, Does.Contain("global::System.Collections.Generic.Dictionary<string, SecretValueResponse>"));
    }

    [Test]
    public void Auto_PropagatesDirectionalityThroughNestedInlineObjects()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", InlineDirectionalSpecYaml)]);

        var dtos = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");
        var containerRequestDetails = GetRecordBlock(dtos, "ContainerRequestDetails");
        var containerResponseDetails = GetRecordBlock(dtos, "ContainerResponseDetails");

        Assert.That(dtos, Does.Contain("public sealed record ContainerRequest"));
        Assert.That(dtos, Does.Contain("public sealed record ContainerResponse"));
        Assert.That(dtos, Does.Contain("public sealed record ContainerRequestDetails"));
        Assert.That(dtos, Does.Contain("public sealed record ContainerResponseDetails"));
        Assert.That(containerRequestDetails, Does.Not.Contain("[JsonPropertyName(\"serverNote\")]"));
        Assert.That(containerResponseDetails, Does.Contain("[JsonPropertyName(\"serverNote\")]"));
    }

    [Test]
    public void Auto_UsesScopedTypesInEndpointSignatures_WhenDirectional()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", DirectionalSpecYaml)]);

        var endpointMapping = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");
        var createHandler = GeneratorTestHelper.GetGeneratedSource(result, "CreateUserEndpointBase.g.cs");
        var getHandler = GeneratorTestHelper.GetGeneratedSource(result, "GetUserEndpointBase.g.cs");

        Assert.That(endpointMapping, Does.Contain("global::TestProject.Openapi.Contracts.UserRequest request"));
        Assert.That(endpointMapping, Does.Contain("Produces<global::TestProject.Openapi.Contracts.UserResponse>("));
        Assert.That(createHandler, Does.Contain("global::TestProject.Openapi.Contracts.UserRequest request"));
        Assert.That(createHandler, Does.Contain("Created<global::TestProject.Openapi.Contracts.UserResponse>"));
        Assert.That(getHandler, Does.Contain("Ok<global::TestProject.Openapi.Contracts.UserResponse>"));
    }

    [Test]
    public void Ignore_KeepsNeutralShapesAndSignatures()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", DirectionalSpecYaml)],
            readWriteSchemaHandling: "Ignore");

        var dtos = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");
        var endpointMapping = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");

        Assert.That(dtos, Does.Not.Contain("public sealed record UserRequest"));
        Assert.That(dtos, Does.Not.Contain("public sealed record UserResponse"));
        Assert.That(dtos, Does.Contain("public required global::System.Guid Id { get; init; }"));
        Assert.That(dtos, Does.Contain("public required string Password { get; init; }"));
        Assert.That(endpointMapping, Does.Contain("global::TestProject.Openapi.Contracts.User request"));
        Assert.That(endpointMapping, Does.Contain("Produces<global::TestProject.Openapi.Contracts.User>("));
    }

    [Test]
    public void Auto_KeepsNeutralTypesWhenGraphIsNeutral()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", NeutralSpecYaml)],
            readWriteSchemaHandling: "Auto");

        var dtos = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");
        var endpointMapping = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");

        Assert.That(dtos, Does.Contain("public sealed record Product"));
        Assert.That(dtos, Does.Not.Contain("public sealed record ProductRequest"));
        Assert.That(dtos, Does.Not.Contain("public sealed record ProductResponse"));
        Assert.That(endpointMapping, Does.Contain("global::TestProject.Openapi.Contracts.Product request"));
        Assert.That(endpointMapping, Does.Contain("Produces<global::TestProject.Openapi.Contracts.Product>("));
    }

    [Test]
    public void Split_UsesScopedTypesForOperationBodyGraphsEvenWhenNeutral()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", NeutralSpecYaml)],
            readWriteSchemaHandling: "Split");

        var dtos = GeneratorTestHelper.GetGeneratedSource(result, "Dtos.g.cs");
        var endpointMapping = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");

        Assert.That(dtos, Does.Contain("public sealed record ProductRequest"));
        Assert.That(dtos, Does.Contain("public sealed record ProductResponse"));
        Assert.That(endpointMapping, Does.Contain("global::TestProject.Openapi.Contracts.ProductRequest request"));
        Assert.That(endpointMapping, Does.Contain("Produces<global::TestProject.Openapi.Contracts.ProductResponse>("));
    }

    [Test]
    public void MissingReadWriteSchemaHandlingMetadata_DefaultsToAuto()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", DirectionalSpecYaml)]);

        var endpointMapping = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");

        Assert.That(endpointMapping, Does.Contain("global::TestProject.Openapi.Contracts.UserRequest request"));
    }

    [Test]
    public void ReadWriteSchemaHandling_AcceptsCaseInsensitiveValues()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", NeutralSpecYaml)],
            readWriteSchemaHandling: "sPLit");

        var endpointMapping = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");
        Assert.That(endpointMapping, Does.Contain("global::TestProject.Openapi.Contracts.ProductRequest request"));
    }

    [Test]
    public void InvalidReadWriteSchemaHandling_ProducesDiagnostic()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: string.Empty,
            additionalFiles: [("openapi.yaml", NeutralSpecYaml)],
            readWriteSchemaHandling: "UnsupportedValue");

        var diagnostic = result.Diagnostics.FirstOrDefault(d => d.Id == "MOA010");
        Assert.That(diagnostic, Is.Not.Null);
        Assert.That(diagnostic!.GetMessage(), Does.Contain("ReadWriteSchemaHandling='UnsupportedValue'"));
    }

    private const string DirectionalSpecYaml = """
        openapi: "3.0.0"
        info:
          title: Directional API
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
                      $ref: '#/components/schemas/User'
              responses:
                "201":
                  description: Created
                  content:
                    application/json:
                      schema:
                        $ref: '#/components/schemas/User'
          /users/{id}:
            get:
              operationId: getUser
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
                        $ref: '#/components/schemas/User'
        components:
          schemas:
            User:
              type: object
              required:
                - id
                - password
                - profile
              properties:
                id:
                  type: string
                  format: uuid
                  readOnly: true
                password:
                  type: string
                  writeOnly: true
                profile:
                  $ref: '#/components/schemas/Profile'
                tags:
                  type: array
                  items:
                    $ref: '#/components/schemas/Tag'
                attributes:
                  type: object
                  additionalProperties:
                    $ref: '#/components/schemas/SecretValue'
            Profile:
              type: object
              required:
                - createdAt
              properties:
                createdAt:
                  type: string
                  format: date-time
                  readOnly: true
                displayName:
                  type: string
            Tag:
              type: object
              required:
                - label
                - internalCode
              properties:
                label:
                  type: string
                internalCode:
                  type: string
                  writeOnly: true
            SecretValue:
              type: object
              required:
                - secret
              properties:
                secret:
                  type: string
                  writeOnly: true
                note:
                  type: string
        """;

    private const string InlineDirectionalSpecYaml = """
        openapi: "3.0.0"
        info:
          title: Inline Directional API
          version: "1.0.0"
        paths:
          /containers:
            post:
              operationId: createContainer
              requestBody:
                required: true
                content:
                  application/json:
                    schema:
                      $ref: '#/components/schemas/Container'
              responses:
                "201":
                  description: Created
                  content:
                    application/json:
                      schema:
                        $ref: '#/components/schemas/Container'
        components:
          schemas:
            Container:
              type: object
              required:
                - details
              properties:
                details:
                  type: object
                  required:
                    - serverNote
                  properties:
                    serverNote:
                      type: string
                      readOnly: true
                    display:
                      type: string
        """;

    private const string NeutralSpecYaml = """
        openapi: "3.0.0"
        info:
          title: Neutral API
          version: "1.0.0"
        paths:
          /products:
            post:
              operationId: createProduct
              requestBody:
                required: true
                content:
                  application/json:
                    schema:
                      $ref: '#/components/schemas/Product'
              responses:
                "201":
                  description: Created
                  content:
                    application/json:
                      schema:
                        $ref: '#/components/schemas/Product'
        components:
          schemas:
            Product:
              type: object
              required:
                - id
                - name
              properties:
                id:
                  type: string
                  format: uuid
                name:
                  type: string
        """;

    private static string GetRecordBlock(string source, string recordName)
    {
        var recordMarker = $"public sealed record {recordName}";
        var startIndex = source.IndexOf(recordMarker, StringComparison.Ordinal);
        Assert.That(startIndex, Is.GreaterThanOrEqualTo(0), $"Record '{recordName}' should exist.");

        var nextIndex = source.IndexOf("public sealed record ", startIndex + recordMarker.Length, StringComparison.Ordinal);
        if (nextIndex < 0)
            nextIndex = source.Length;

        return source.Substring(startIndex, nextIndex - startIndex);
    }
}
