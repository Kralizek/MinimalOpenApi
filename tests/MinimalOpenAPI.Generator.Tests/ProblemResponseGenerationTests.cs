namespace MinimalOpenAPI.Generator.Tests;

[TestFixture]
public class ProblemResponseGenerationTests
{
    [Test]
    public void ApplicationProblemJsonResponses_GenerateStatusSpecificProblemWrappers_WithStronglyTypedPayloads()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "",
            additionalFiles: [("openapi.yaml", ProblemResponsesYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetBookEndpointBase.g.cs");

        Assert.That(source, Does.Contain("public virtual"));
        Assert.That(source, Does.Contain("HandleAsync("));
        Assert.That(source, Does.Contain("Results<"));
        Assert.That(source, Does.Contain("Ok<global::TestProject.Openapi.Contracts.Book>"));
        Assert.That(source, Does.Contain("BadRequestProblem"));
        Assert.That(source, Does.Contain("NotFoundProblem"));
        Assert.That(source, Does.Contain("ConflictProblem"));
        Assert.That(source, Does.Not.Contain("HttpResults.BadRequest<"));
        Assert.That(source, Does.Not.Contain("HttpResults.NotFound<"));
        Assert.That(source, Does.Not.Contain("HttpResults.Conflict<"));

        Assert.That(source, Does.Contain("public sealed class BadRequestProblem"));
        Assert.That(source, Does.Contain("IValueHttpResult<global::Microsoft.AspNetCore.Mvc.ProblemDetails>"));
        Assert.That(source, Does.Contain("Value.Status ??= global::Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest;"));

        Assert.That(source, Does.Contain("public sealed record NotFoundResponse"));
        Assert.That(source, Does.Contain("public sealed class NotFoundProblem"));
        Assert.That(source, Does.Contain("IValueHttpResult<NotFoundResponse>"));

        Assert.That(source, Does.Contain("public sealed class ConflictProblem"));
        Assert.That(source, Does.Contain("IValueHttpResult<global::TestProject.Openapi.Contracts.BookLockedProblem>"));

        Assert.That(source, Does.Contain("httpContext.Response.StatusCode = global::Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest;"));
        Assert.That(source, Does.Contain("httpContext.Response.StatusCode = global::Microsoft.AspNetCore.Http.StatusCodes.Status404NotFound;"));
        Assert.That(source, Does.Contain("httpContext.Response.StatusCode = global::Microsoft.AspNetCore.Http.StatusCodes.Status409Conflict;"));
        Assert.That(source, Does.Contain("global::Microsoft.AspNetCore.Http.HttpResponseJsonExtensions.WriteAsJsonAsync("));
        Assert.That(source, Does.Not.Contain("httpContext.Response.WriteAsJsonAsync("));
        Assert.That(source, Does.Contain("contentType: \"application/problem+json\""));
    }

    [Test]
    public void ApplicationProblemJsonResponses_GenerateProblemMetadataPayloadTypeAndContentType()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "",
            additionalFiles: [("openapi.yaml", ProblemResponsesYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");

        Assert.That(source, Does.Contain(".Produces<global::TestProject.Openapi.Contracts.Book>(global::Microsoft.AspNetCore.Http.StatusCodes.Status200OK)"));
        Assert.That(source, Does.Contain(".Produces<global::Microsoft.AspNetCore.Mvc.ProblemDetails>(global::Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest, \"application/problem+json\")"));
        Assert.That(source, Does.Contain(".Produces<global::TestProject.Openapi.Endpoints.GetBookEndpointBase.NotFoundResponse>(global::Microsoft.AspNetCore.Http.StatusCodes.Status404NotFound, \"application/problem+json\")"));
        Assert.That(source, Does.Contain(".Produces<global::TestProject.Openapi.Contracts.BookLockedProblem>(global::Microsoft.AspNetCore.Http.StatusCodes.Status409Conflict, \"application/problem+json\")"));
    }

    [Test]
    public void UnknownProblemStatus_UsesStatusCodeFallbackName()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "",
            additionalFiles: [("openapi.yaml", UnknownProblemStatusYaml)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "RedirectHintEndpointBase.g.cs");

        Assert.That(source, Does.Contain("Status302Problem"));
        Assert.That(source, Does.Not.Contain("RedirectProblem"));
        Assert.That(source, Does.Contain("public int? StatusCode => global::Microsoft.AspNetCore.Http.StatusCodes.Status302Found;"));
        Assert.That(source, Does.Contain("contentType: \"application/problem+json\""));
    }

    [Test]
    public void ApplicationJsonResponses_RemainUnchanged_AndDoNotGenerateProblemWrappers()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "",
            additionalFiles: [("openapi.yaml", JsonOnlyResponsesYaml)]);

        var handlerBaseSource = GeneratorTestHelper.GetGeneratedSource(result, "GetOrderEndpointBase.g.cs");
        var endpointMappingSource = GeneratorTestHelper.GetGeneratedSource(result, "EndpointMapping.g.cs");

        Assert.That(handlerBaseSource, Does.Contain("HttpResults.BadRequest<global::TestProject.Openapi.Contracts.OrderValidationError>"));
        Assert.That(handlerBaseSource, Does.Not.Contain("BadRequestProblem"));
        Assert.That(endpointMappingSource, Does.Contain(".Produces<global::TestProject.Openapi.Contracts.OrderValidationError>(global::Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest)"));
        Assert.That(endpointMappingSource, Does.Not.Contain("application/problem+json"));
    }

    [Test]
    public void JsonParser_PreservesProblemContentType_ForProblemWrapperGeneration()
    {
        var (result, _) = GeneratorTestHelper.RunGenerator(
            userSource: "",
            additionalFiles: [("openapi.json", ProblemResponsesJson)]);

        var source = GeneratorTestHelper.GetGeneratedSource(result, "GetBookEndpointBase.g.cs");

        Assert.That(source, Does.Contain("BadRequestProblem"));
        Assert.That(source, Does.Contain("IValueHttpResult<global::Microsoft.AspNetCore.Mvc.ProblemDetails>"));
    }

    private const string ProblemResponsesYaml = """
        openapi: "3.0.0"
        info:
          title: Test
          version: "1.0"
        paths:
          /books/{bookId}:
            get:
              operationId: getBook
              parameters:
                - name: bookId
                  in: path
                  required: true
                  schema:
                    type: string
                    format: uuid
              responses:
                "200":
                  description: Book found
                  content:
                    application/json:
                      schema:
                        $ref: "#/components/schemas/Book"
                "400":
                  description: Invalid request
                  content:
                    application/problem+json: {}
                "404":
                  description: Book not found
                  content:
                    application/problem+json:
                      schema:
                        type: object
                        required: [title, status, missingBookId]
                        properties:
                          title:
                            type: string
                          status:
                            type: integer
                            format: int32
                          detail:
                            type: string
                            nullable: true
                          missingBookId:
                            type: string
                            format: uuid
                "409":
                  description: Book is locked
                  content:
                    application/problem+json:
                      schema:
                        $ref: "#/components/schemas/BookLockedProblem"
        components:
          schemas:
            Book:
              type: object
              required: [id, title]
              properties:
                id:
                  type: string
                  format: uuid
                title:
                  type: string
            BookLockedProblem:
              type: object
              required: [title, status, lockOwner]
              properties:
                title:
                  type: string
                status:
                  type: integer
                  format: int32
                detail:
                  type: string
                  nullable: true
                lockOwner:
                  type: string
        """;

    private const string UnknownProblemStatusYaml = """
        openapi: "3.0.0"
        info:
          title: Test
          version: "1.0"
        paths:
          /redirect-hint:
            get:
              operationId: redirectHint
              responses:
                "302":
                  description: Redirect represented as problem response
                  content:
                    application/problem+json: {}
        """;

    private const string JsonOnlyResponsesYaml = """
        openapi: "3.0.0"
        info:
          title: Test
          version: "1.0"
        paths:
          /orders/{id}:
            get:
              operationId: getOrder
              parameters:
                - name: id
                  in: path
                  required: true
                  schema:
                    type: string
                    format: uuid
              responses:
                "200":
                  description: Ok
                  content:
                    application/json:
                      schema:
                        $ref: "#/components/schemas/Order"
                "400":
                  description: Validation error
                  content:
                    application/json:
                      schema:
                        $ref: "#/components/schemas/OrderValidationError"
        components:
          schemas:
            Order:
              type: object
              required: [id]
              properties:
                id:
                  type: string
                  format: uuid
            OrderValidationError:
              type: object
              required: [message]
              properties:
                message:
                  type: string
        """;

    private const string ProblemResponsesJson = """
        {
          "openapi": "3.0.0",
          "info": {
            "title": "Test",
            "version": "1.0"
          },
          "paths": {
            "/books/{bookId}": {
              "get": {
                "operationId": "getBook",
                "parameters": [
                  {
                    "name": "bookId",
                    "in": "path",
                    "required": true,
                    "schema": {
                      "type": "string",
                      "format": "uuid"
                    }
                  }
                ],
                "responses": {
                  "400": {
                    "description": "Invalid request",
                    "content": {
                      "application/problem+json": {}
                    }
                  }
                }
              }
            }
          }
        }
        """;
}