namespace MinimalOpenAPI.Generator.Tests;

/// <summary>Shared OpenAPI YAML fixtures for generator tests.</summary>
internal static class OpenApiFixtures
{
    public const string GetClientYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /tenants/{tenantId}/clients/{clientId}:
            get:
              operationId: getClient
              summary: Get a specific client
              description: Returns the client with the specified identifier.
              tags:
                - clients
              parameters:
                - name: tenantId
                  in: path
                  required: true
                  schema:
                    type: string
                    format: uuid
                - name: clientId
                  in: path
                  required: true
                  schema:
                    type: string
                    format: uuid
                - name: includeDeleted
                  in: query
                  required: false
                  schema:
                    type: boolean
              responses:
                "200":
                  description: OK
                  content:
                    application/json:
                      schema:
                        $ref: '#/components/schemas/Client'
                "404":
                  description: Not found
        components:
          schemas:
            Client:
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
                vatNumber:
                  type: string
                  nullable: true
        """;

    public const string CreateClientYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /tenants/{tenantId}/clients:
            post:
              operationId: createClient
              parameters:
                - name: tenantId
                  in: path
                  required: true
                  schema:
                    type: string
                    format: uuid
              requestBody:
                required: true
                content:
                  application/json:
                    schema:
                      $ref: '#/components/schemas/CreateClientRequest'
              responses:
                "201":
                  description: Created
                  content:
                    application/json:
                      schema:
                        $ref: '#/components/schemas/Client'
                "400":
                  description: Bad Request
        components:
          schemas:
            Client:
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
            CreateClientRequest:
              type: object
              required:
                - name
              properties:
                name:
                  type: string
                vatNumber:
                  type: string
                  nullable: true
        """;
}
