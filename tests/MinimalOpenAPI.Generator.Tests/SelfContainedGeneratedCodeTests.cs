namespace MinimalOpenAPI.Generator.Tests;

/// <summary>
/// Compiles the full generated output (DTOs, handler bases, endpoint configuration bases, DI
/// registration and endpoint mapping) without implicit or global usings, under namespace layouts
/// whose segments collide with generated namespaces. Regression coverage for generated code that
/// relied on partially qualified names (#125) or on Web SDK implicit usings (#126).
/// </summary>
[TestFixture]
public class SelfContainedGeneratedCodeTests
{
    private const string Contract = """
        openapi: "3.0.0"
        info:
          title: Pets
          version: "1"
        paths:
          /pets/{petId}:
            get:
              operationId: getPet
              parameters:
                - { name: petId, in: path, required: true, schema: { type: string, format: uuid } }
                - { name: include, in: query, schema: { type: string } }
                - { name: X-Tenant, in: header, schema: { type: string } }
              responses:
                "200":
                  description: The pet.
                  content:
                    application/json:
                      schema: { $ref: '#/components/schemas/Pet' }
                "404":
                  description: Not found.
                  content:
                    application/problem+json: {}
          /pets:
            post:
              operationId: createPet
              requestBody:
                required: true
                content:
                  application/json:
                    schema: { $ref: '#/components/schemas/Pet' }
              responses:
                "201":
                  description: Created.
                  content:
                    application/json:
                      schema:
                        type: object
                        properties:
                          id: { type: string, format: uuid }
          /pets/{petId}/photo:
            post:
              operationId: uploadPhoto
              parameters:
                - { name: petId, in: path, required: true, schema: { type: string, format: uuid } }
              requestBody:
                required: true
                content:
                  multipart/form-data:
                    schema:
                      type: object
                      required: [file]
                      properties:
                        file: { type: string, format: binary }
              responses:
                "204":
                  description: Uploaded.
        components:
          schemas:
            Pet:
              type: object
              required: [name, kind]
              properties:
                name: { type: string, maxLength: 50 }
                kind: { $ref: '#/components/schemas/PetKind' }
            PetKind:
              type: string
              enum: [cat, dog]
        """;

    private static string UserSource(string handlerNamespace, string endpointsNamespace) => $$"""
        namespace {{handlerNamespace}}
        {
            public sealed class GetPetHandler : global::{{endpointsNamespace}}.GetPetEndpointBase { }
            public sealed class CreatePetHandler : global::{{endpointsNamespace}}.CreatePetEndpointBase { }
            public sealed class UploadPhotoHandler : global::{{endpointsNamespace}}.UploadPhotoEndpointBase { }

            public sealed class UploadPhotoConfiguration : global::{{endpointsNamespace}}.UploadPhotoEndpointConfigurationBase
            {
                public override void Configure(global::Microsoft.AspNetCore.Builder.RouteHandlerBuilder endpoint) { }
            }
        }
        """;

    [TestCase("TestProject", "Api", "TestProject.Handlers", TestName = "Default layout")]
    [TestCase("Petstore", "Petstore", "Petstore.Handlers", TestName = "Root namespace equals OpenApi Namespace")]
    [TestCase("Contracts", "Api", "Contracts.Handlers", TestName = "Root namespace Contracts")]
    [TestCase("Endpoints", "Api", "Endpoints.Handlers", TestName = "Root namespace Endpoints")]
    [TestCase("TestProject", "Api", "Api.Handlers", TestName = "Handler namespace collides with generated namespace")]
    public void GeneratedSources_CompileWithoutImplicitUsings(string rootNamespace, string specName, string handlerNamespace)
    {
        var endpointsNamespace = $"{rootNamespace}.{specName}.Endpoints";
        var (result, outputCompilation) = GeneratorTestHelper.RunGenerator(
            UserSource(handlerNamespace, endpointsNamespace),
            [("openapi.yaml", Contract)],
            rootNamespace: rootNamespace,
            specNameOverride: specName);

        Assert.That(result.Diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error), Is.Empty);

        var registration = GeneratorTestHelper.GetGeneratedSource(result, "DependencyInjection.g.cs");
        Assert.That(registration, Does.Contain($"global::{handlerNamespace}.GetPetHandler"),
            "the concrete handler must be registered through its discovered implementation");

        var errors = GeneratorTestHelper.GetCompilationErrors(outputCompilation);
        Assert.That(errors, Is.Empty, string.Join(Environment.NewLine, errors));
    }
}