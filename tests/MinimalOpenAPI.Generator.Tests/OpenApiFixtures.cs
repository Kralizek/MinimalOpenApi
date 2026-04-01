namespace MinimalOpenAPI.Generator.Tests;

/// <summary>Shared OpenAPI YAML and JSON fixtures for generator tests.</summary>
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

    /// <summary>
    /// A POST endpoint where both the request body and the 200 response schema are defined
    /// inline (no <c>$ref</c> to a named component schema).
    /// </summary>
    public const string CreateOrderWithInlineSchemasYaml = """
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
                      type: object
                      required:
                        - productId
                        - quantity
                      properties:
                        productId:
                          type: string
                          format: uuid
                        quantity:
                          type: integer
              responses:
                "201":
                  description: Created
                  content:
                    application/json:
                      schema:
                        type: object
                        required:
                          - orderId
                        properties:
                          orderId:
                            type: string
                            format: uuid
                "400":
                  description: Bad Request
        """;

    public const string GetClientJson = """
        {
          "openapi": "3.0.0",
          "info": {
            "title": "Test API",
            "version": "1.0.0"
          },
          "paths": {
            "/tenants/{tenantId}/clients/{clientId}": {
              "get": {
                "operationId": "getClient",
                "summary": "Get a specific client",
                "description": "Returns the client with the specified identifier.",
                "tags": ["clients"],
                "parameters": [
                  {
                    "name": "tenantId",
                    "in": "path",
                    "required": true,
                    "schema": { "type": "string", "format": "uuid" }
                  },
                  {
                    "name": "clientId",
                    "in": "path",
                    "required": true,
                    "schema": { "type": "string", "format": "uuid" }
                  },
                  {
                    "name": "includeDeleted",
                    "in": "query",
                    "required": false,
                    "schema": { "type": "boolean" }
                  }
                ],
                "responses": {
                  "200": {
                    "description": "OK",
                    "content": {
                      "application/json": {
                        "schema": { "$ref": "#/components/schemas/Client" }
                      }
                    }
                  },
                  "404": { "description": "Not found" }
                }
              }
            }
          },
          "components": {
            "schemas": {
              "Client": {
                "type": "object",
                "required": ["id", "name"],
                "properties": {
                  "id": { "type": "string", "format": "uuid" },
                  "name": { "type": "string" },
                  "vatNumber": { "type": "string", "nullable": true }
                }
              }
            }
          }
        }
        """;

    public const string CreateClientJson = """
        {
          "openapi": "3.0.0",
          "info": {
            "title": "Test API",
            "version": "1.0.0"
          },
          "paths": {
            "/tenants/{tenantId}/clients": {
              "post": {
                "operationId": "createClient",
                "parameters": [
                  {
                    "name": "tenantId",
                    "in": "path",
                    "required": true,
                    "schema": { "type": "string", "format": "uuid" }
                  }
                ],
                "requestBody": {
                  "required": true,
                  "content": {
                    "application/json": {
                      "schema": { "$ref": "#/components/schemas/CreateClientRequest" }
                    }
                  }
                },
                "responses": {
                  "201": {
                    "description": "Created",
                    "content": {
                      "application/json": {
                        "schema": { "$ref": "#/components/schemas/Client" }
                      }
                    }
                  },
                  "400": { "description": "Bad Request" }
                }
              }
            }
          },
          "components": {
            "schemas": {
              "Client": {
                "type": "object",
                "required": ["id", "name"],
                "properties": {
                  "id": { "type": "string", "format": "uuid" },
                  "name": { "type": "string" }
                }
              },
              "CreateClientRequest": {
                "type": "object",
                "required": ["name"],
                "properties": {
                  "name": { "type": "string" },
                  "vatNumber": { "type": "string", "nullable": true }
                }
              }
            }
          }
        }
        """;

    /// <summary>
    /// A GET endpoint whose response DTO contains a <c>format: date</c> field,
    /// which should map to <c>global::System.DateOnly</c>.
    /// </summary>
    public const string GetEventYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /events/{eventId}:
            get:
              operationId: getEvent
              parameters:
                - name: eventId
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
                        $ref: '#/components/schemas/Event'
                "404":
                  description: Not found
        components:
          schemas:
            Event:
              type: object
              required:
                - id
                - date
              properties:
                id:
                  type: string
                  format: uuid
                date:
                  type: string
                  format: date
                notes:
                  type: string
                  format: date
                  nullable: true
        """;

    /// <summary>
    /// A GET endpoint whose response DTO contains a <c>format: date</c> field,
    /// which should map to <c>global::System.DateOnly</c> (JSON variant).
    /// </summary>
    public const string GetEventJson = """
        {
          "openapi": "3.0.0",
          "info": {
            "title": "Test API",
            "version": "1.0.0"
          },
          "paths": {
            "/events/{eventId}": {
              "get": {
                "operationId": "getEvent",
                "parameters": [
                  {
                    "name": "eventId",
                    "in": "path",
                    "required": true,
                    "schema": { "type": "string", "format": "uuid" }
                  }
                ],
                "responses": {
                  "200": {
                    "description": "OK",
                    "content": {
                      "application/json": {
                        "schema": { "$ref": "#/components/schemas/Event" }
                      }
                    }
                  },
                  "404": { "description": "Not found" }
                }
              }
            }
          },
          "components": {
            "schemas": {
              "Event": {
                "type": "object",
                "required": ["id", "date"],
                "properties": {
                  "id": { "type": "string", "format": "uuid" },
                  "date": { "type": "string", "format": "date" },
                  "notes": { "type": "string", "format": "date", "nullable": true }
                }
              }
            }
          }
        }
        """;

    /// <summary>
    /// A POST endpoint where both the request body and the 200 response schema are defined
    /// inline (no <c>$ref</c> to a named component schema).
    /// </summary>
    public const string CreateOrderWithInlineSchemasJson = """
        {
          "openapi": "3.0.0",
          "info": {
            "title": "Test API",
            "version": "1.0.0"
          },
          "paths": {
            "/orders": {
              "post": {
                "operationId": "createOrder",
                "requestBody": {
                  "required": true,
                  "content": {
                    "application/json": {
                      "schema": {
                        "type": "object",
                        "required": ["productId", "quantity"],
                        "properties": {
                          "productId": { "type": "string", "format": "uuid" },
                          "quantity": { "type": "integer" }
                        }
                      }
                    }
                  }
                },
                "responses": {
                  "201": {
                    "description": "Created",
                    "content": {
                      "application/json": {
                        "schema": {
                          "type": "object",
                          "required": ["orderId"],
                          "properties": {
                            "orderId": { "type": "string", "format": "uuid" }
                          }
                        }
                      }
                    }
                  },
                  "400": { "description": "Bad Request" }
                }
              }
            }
          }
        }
        """;
}