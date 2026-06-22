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

    /// <summary>
    /// A GET endpoint whose response DTO contains a top-level <c>enum</c> schema
    /// and an object schema whose property references it via <c>$ref</c>.
    /// </summary>
    public const string GetOrderWithEnumYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /orders/{orderId}:
            get:
              operationId: getOrder
              parameters:
                - name: orderId
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
                        $ref: '#/components/schemas/Order'
                "404":
                  description: Not found
        components:
          schemas:
            OrderStatus:
              type: string
              enum:
                - pending
                - active
                - cancelled
            Order:
              type: object
              required:
                - id
                - status
              properties:
                id:
                  type: string
                  format: uuid
                status:
                  $ref: '#/components/schemas/OrderStatus'
        """;

    /// <summary>
    /// A GET endpoint whose response DTO has a property with an inline <c>enum</c> schema
    /// (no top-level enum component schema).
    /// </summary>
    public const string GetProductWithInlineEnumYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /products/{productId}:
            get:
              operationId: getProduct
              parameters:
                - name: productId
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
                        $ref: '#/components/schemas/Product'
                "404":
                  description: Not found
        components:
          schemas:
            Product:
              type: object
              required:
                - id
                - category
              properties:
                id:
                  type: string
                  format: uuid
                category:
                  type: string
                  enum:
                    - electronics
                    - clothing
                    - food
        """;

    /// <summary>
    /// JSON variant of <see cref="GetOrderWithEnumYaml"/>.
    /// </summary>
    public const string GetOrderWithEnumJson = """
        {
          "openapi": "3.0.0",
          "info": {
            "title": "Test API",
            "version": "1.0.0"
          },
          "paths": {
            "/orders/{orderId}": {
              "get": {
                "operationId": "getOrder",
                "parameters": [
                  {
                    "name": "orderId",
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
                        "schema": { "$ref": "#/components/schemas/Order" }
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
              "OrderStatus": {
                "type": "string",
                "enum": ["pending", "active", "cancelled"]
              },
              "Order": {
                "type": "object",
                "required": ["id", "status"],
                "properties": {
                  "id": { "type": "string", "format": "uuid" },
                  "status": { "$ref": "#/components/schemas/OrderStatus" }
                }
              }
            }
          }
        }
        """;

    /// <summary>
    /// JSON variant of <see cref="GetProductWithInlineEnumYaml"/>.
    /// </summary>
    public const string GetProductWithInlineEnumJson = """
        {
          "openapi": "3.0.0",
          "info": {
            "title": "Test API",
            "version": "1.0.0"
          },
          "paths": {
            "/products/{productId}": {
              "get": {
                "operationId": "getProduct",
                "parameters": [
                  {
                    "name": "productId",
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
                        "schema": { "$ref": "#/components/schemas/Product" }
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
              "Product": {
                "type": "object",
                "required": ["id", "category"],
                "properties": {
                  "id": { "type": "string", "format": "uuid" },
                  "category": {
                    "type": "string",
                    "enum": ["electronics", "clothing", "food"]
                  }
                }
              }
            }
          }
        }
        """;

    /// <summary>
    /// A GET endpoint whose response DTO has a top-level <c>enum</c> schema whose values
    /// are bare integers (<c>0</c>, <c>1</c>, <c>2</c>) — not valid C# identifiers on their own.
    /// </summary>
    public const string GetOrderWithNumericEnumYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /orders/{orderId}:
            get:
              operationId: getOrder
              parameters:
                - name: orderId
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
                        $ref: '#/components/schemas/Order'
                "404":
                  description: Not found
        components:
          schemas:
            OrderPriority:
              type: integer
              enum:
                - 0
                - 1
                - 2
            Order:
              type: object
              required:
                - id
                - priority
              properties:
                id:
                  type: string
                  format: uuid
                priority:
                  $ref: '#/components/schemas/OrderPriority'
        """;

    /// <summary>
    /// A GET endpoint whose response DTO (<c>Order</c>) contains an inline object property
    /// (<c>address</c>) that should be emitted as a top-level <c>OrderAddress</c> record.
    /// </summary>
    public const string GetOrderWithNestedAddressYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /orders/{orderId}:
            get:
              operationId: getOrder
              parameters:
                - name: orderId
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
                        $ref: '#/components/schemas/Order'
                "404":
                  description: Not found
        components:
          schemas:
            Order:
              type: object
              required:
                - id
                - address
              properties:
                id:
                  type: string
                  format: uuid
                address:
                  type: object
                  required:
                    - street
                    - city
                  properties:
                    street:
                      type: string
                    city:
                      type: string
                note:
                  type: string
                  nullable: true
        """;

    /// <summary>
    /// A schema where an inline-object property itself has an inline-object property,
    /// verifying recursive nested record generation.
    /// </summary>
    public const string GetShipmentWithDeepNestingYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /shipments/{shipmentId}:
            get:
              operationId: getShipment
              parameters:
                - name: shipmentId
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
                        $ref: '#/components/schemas/Shipment'
                "404":
                  description: Not found
        components:
          schemas:
            Shipment:
              type: object
              required:
                - id
                - destination
              properties:
                id:
                  type: string
                  format: uuid
                destination:
                  type: object
                  required:
                    - address
                  properties:
                    address:
                      type: object
                      required:
                        - street
                        - city
                      properties:
                        street:
                          type: string
                        city:
                          type: string
        """;

    /// <summary>
    /// A schema whose properties use snake_case and kebab-case naming,
    /// verifying that generated C# identifiers are properly PascalCase.
    /// </summary>
    public const string GetInvoiceWithMixedCasePropertiesYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /invoices/{invoiceId}:
            get:
              operationId: getInvoice
              parameters:
                - name: invoiceId
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
                        $ref: '#/components/schemas/Invoice'
                "404":
                  description: Not found
        components:
          schemas:
            Invoice:
              type: object
              required:
                - invoice_id
                - billing_address
              properties:
                invoice_id:
                  type: string
                  format: uuid
                billing_address:
                  type: object
                  required:
                    - street_name
                  properties:
                    street_name:
                      type: string
                    zip_code:
                      type: string
                      nullable: true
                due_date:
                  type: string
                  format: date
                  nullable: true
        """;

    // ── OpenAPI 3.1 fixtures ──────────────────────────────────────────────
    //
    // These are 3.1 equivalents of the 3.0 fixtures above.  The key difference
    // is that nullable fields use the JSON Schema 2020-12 type-array syntax
    // ("type": ["string", "null"]) instead of the OpenAPI 3.0 "nullable: true"
    // keyword.  All other structure (paths, schemas, formats) is identical so
    // the generated C# output should be identical to the 3.0 counterpart.

    /// <summary>
    /// OpenAPI 3.1 equivalent of <see cref="GetClientYaml"/>.
    /// Nullable field <c>vatNumber</c> uses the 3.1 type-array syntax instead of
    /// <c>nullable: true</c>.
    /// </summary>
    public const string GetClientV31Yaml = """
        openapi: "3.1.0"
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
                  type:
                    - string
                    - "null"
        """;

    /// <summary>
    /// OpenAPI 3.1 equivalent of <see cref="GetClientJson"/>.
    /// Nullable field <c>vatNumber</c> uses the 3.1 type-array syntax instead of
    /// <c>"nullable": true</c>.
    /// </summary>
    public const string GetClientV31Json = """
        {
          "openapi": "3.1.0",
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
                  "vatNumber": { "type": ["string", "null"] }
                }
              }
            }
          }
        }
        """;

    /// <summary>
    /// OpenAPI 3.1 equivalent of <see cref="CreateClientYaml"/>.
    /// Nullable field <c>vatNumber</c> in <c>CreateClientRequest</c> uses the 3.1
    /// type-array syntax.
    /// </summary>
    public const string CreateClientV31Yaml = """
        openapi: "3.1.0"
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
                  type:
                    - string
                    - "null"
        """;

    /// <summary>
    /// An OpenAPI 3.1 YAML document with a rich schema that exercises multiple
    /// type-array combinations: nullable string, nullable integer, nullable uuid,
    /// non-nullable type array (no "null"), and a required field.
    /// </summary>
    public const string GetOrderV31Yaml = """
        openapi: "3.1.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /orders/{orderId}:
            get:
              operationId: getOrder
              parameters:
                - name: orderId
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
                        $ref: '#/components/schemas/Order'
                "404":
                  description: Not found
        components:
          schemas:
            Order:
              type: object
              required:
                - id
                - quantity
              properties:
                id:
                  type: string
                  format: uuid
                quantity:
                  type: integer
                notes:
                  type:
                    - string
                    - "null"
                discount:
                  type:
                    - integer
                    - "null"
                referralCode:
                  type:
                    - string
                    - "null"
                  format: uuid
                tag:
                  type:
                    - string
        """;

    /// <summary>
    /// JSON equivalent of <see cref="GetOrderV31Yaml"/>: an OpenAPI 3.1 spec
    /// exercising multiple type-array combinations.
    /// </summary>
    public const string GetOrderV31Json = """
        {
          "openapi": "3.1.0",
          "info": {
            "title": "Test API",
            "version": "1.0.0"
          },
          "paths": {
            "/orders/{orderId}": {
              "get": {
                "operationId": "getOrder",
                "parameters": [
                  {
                    "name": "orderId",
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
                        "schema": { "$ref": "#/components/schemas/Order" }
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
              "Order": {
                "type": "object",
                "required": ["id", "quantity"],
                "properties": {
                  "id": { "type": "string", "format": "uuid" },
                  "quantity": { "type": "integer" },
                  "notes": { "type": ["string", "null"] },
                  "discount": { "type": ["integer", "null"] },
                  "referralCode": { "type": ["string", "null"], "format": "uuid" },
                  "tag": { "type": ["string"] }
                }
              }
            }
          }
        }
        """;

    /// <summary>
    /// YAML fixture: a schema with a required <c>labels</c> property using
    /// <c>additionalProperties: { type: string }</c> (maps to <c>Dictionary&lt;string, string&gt;</c>)
    /// and an optional <c>metadata</c> property using
    /// <c>additionalProperties: { type: integer }</c> (maps to <c>Dictionary&lt;string, int&gt;</c>).
    /// </summary>
    public const string GetResourceWithAdditionalPropertiesYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /resources/{resourceId}:
            get:
              operationId: getResource
              parameters:
                - name: resourceId
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
                        $ref: '#/components/schemas/Resource'
                "404":
                  description: Not found
        components:
          schemas:
            Resource:
              type: object
              required:
                - id
                - labels
              properties:
                id:
                  type: string
                  format: uuid
                labels:
                  type: object
                  additionalProperties:
                    type: string
                metadata:
                  type: object
                  additionalProperties:
                    type: integer
        """;

    /// <summary>
    /// JSON equivalent of <see cref="GetResourceWithAdditionalPropertiesYaml"/>.
    /// </summary>
    public const string GetResourceWithAdditionalPropertiesJson = """
        {
          "openapi": "3.0.0",
          "info": { "title": "Test API", "version": "1.0.0" },
          "paths": {
            "/resources/{resourceId}": {
              "get": {
                "operationId": "getResource",
                "parameters": [
                  {
                    "name": "resourceId",
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
                        "schema": { "$ref": "#/components/schemas/Resource" }
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
              "Resource": {
                "type": "object",
                "required": ["id", "labels"],
                "properties": {
                  "id": { "type": "string", "format": "uuid" },
                  "labels": {
                    "type": "object",
                    "additionalProperties": { "type": "string" }
                  },
                  "metadata": {
                    "type": "object",
                    "additionalProperties": { "type": "integer" }
                  }
                }
              }
            }
          }
        }
        """;

    /// <summary>
    /// OpenAPI spec (YAML) with a component schema that has a property using
    /// <c>additionalProperties: { type: object, properties: {...} }</c>.
    /// The value type is an inline complex schema; the generator should emit a
    /// separate record named <c>ResourceTagsValue</c> and type the property as
    /// <c>Dictionary&lt;string, ResourceTagsValue&gt;</c>.
    /// </summary>
    public const string GetResourceWithInlineComplexAdditionalPropertiesYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /resources/{resourceId}:
            get:
              operationId: getResource
              parameters:
                - name: resourceId
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
                        $ref: '#/components/schemas/Resource'
                "404":
                  description: Not found
        components:
          schemas:
            Resource:
              type: object
              required:
                - id
              properties:
                id:
                  type: string
                  format: uuid
                tags:
                  type: object
                  additionalProperties:
                    type: object
                    properties:
                      label:
                        type: string
                      weight:
                        type: integer
        """;

    /// <summary>
    /// JSON equivalent of <see cref="GetResourceWithInlineComplexAdditionalPropertiesYaml"/>.
    /// </summary>
    public const string GetResourceWithInlineComplexAdditionalPropertiesJson = """
        {
          "openapi": "3.0.0",
          "info": { "title": "Test API", "version": "1.0.0" },
          "paths": {
            "/resources/{resourceId}": {
              "get": {
                "operationId": "getResource",
                "parameters": [
                  {
                    "name": "resourceId",
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
                        "schema": { "$ref": "#/components/schemas/Resource" }
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
              "Resource": {
                "type": "object",
                "required": ["id"],
                "properties": {
                  "id": { "type": "string", "format": "uuid" },
                  "tags": {
                    "type": "object",
                    "additionalProperties": {
                      "type": "object",
                      "properties": {
                        "label": { "type": "string" },
                        "weight": { "type": "integer" }
                      }
                    }
                  }
                }
              }
            }
          }
        }
        """;

    /// <summary>
    /// A list endpoint with a query parameter typed via <c>$ref</c> to a top-level enum schema.
    /// Used to verify that the generated <c>Parameters</c> record uses the fully-qualified
    /// contracts-namespace type for the enum property.
    /// </summary>
    public const string ListOrdersWithEnumQueryParamYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /orders:
            get:
              operationId: listOrders
              parameters:
                - name: status
                  in: query
                  required: false
                  schema:
                    $ref: '#/components/schemas/OrderStatus'
              responses:
                "200":
                  description: OK
                  content:
                    application/json:
                      schema:
                        type: array
                        items:
                          $ref: '#/components/schemas/Order'
        components:
          schemas:
            OrderStatus:
              type: string
              enum:
                - pending
                - active
                - cancelled
            Order:
              type: object
              required: [id]
              properties:
                id:
                  type: string
                  format: uuid
                status:
                  $ref: '#/components/schemas/OrderStatus'
        """;

    /// <summary>
    /// A POST endpoint whose inline request body contains a property typed via <c>$ref</c>
    /// to a top-level enum schema.  Used to verify that the generated inline <c>Request</c>
    /// record uses the fully-qualified contracts-namespace type for the enum property.
    /// </summary>
    public const string CreateOrderWithEnumRequestBodyYaml = """
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
                      required: [title]
                      properties:
                        title:
                          type: string
                        status:
                          $ref: '#/components/schemas/OrderStatus'
              responses:
                "201":
                  description: Created
        components:
          schemas:
            OrderStatus:
              type: string
              enum:
                - pending
                - active
                - cancelled
        """;

    // ── Component parameter fixtures ──────────────────────────────────────
    //
    // These fixtures use reusable parameters defined under components/parameters
    // and referenced via $ref inside operation parameter arrays.

    /// <summary>
    /// A GET endpoint that mixes a component parameter <c>$ref</c> (providerId, path)
    /// with an inline query parameter (page), demonstrating mixed inline+ref ordering.
    /// </summary>
    public const string GetLeadsWithComponentParametersYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /providers/{providerId}/leads:
            get:
              operationId: getLeads
              parameters:
                - $ref: '#/components/parameters/ProviderId'
                - name: page
                  in: query
                  required: false
                  schema:
                    type: integer
              responses:
                "200":
                  description: OK
        components:
          parameters:
            ProviderId:
              name: providerId
              in: path
              required: true
              schema:
                type: string
                format: uuid
        """;

    /// <summary>
    /// JSON variant of <see cref="GetLeadsWithComponentParametersYaml"/>.
    /// </summary>
    public const string GetLeadsWithComponentParametersJson = """
        {
          "openapi": "3.0.0",
          "info": {
            "title": "Test API",
            "version": "1.0.0"
          },
          "paths": {
            "/providers/{providerId}/leads": {
              "get": {
                "operationId": "getLeads",
                "parameters": [
                  { "$ref": "#/components/parameters/ProviderId" },
                  {
                    "name": "page",
                    "in": "query",
                    "required": false,
                    "schema": { "type": "integer" }
                  }
                ],
                "responses": {
                  "200": { "description": "OK" }
                }
              }
            }
          },
          "components": {
            "parameters": {
              "ProviderId": {
                "name": "providerId",
                "in": "path",
                "required": true,
                "schema": { "type": "string", "format": "uuid" }
              }
            }
          }
        }
        """;

    /// <summary>
    /// A GET endpoint whose parameters are all defined under <c>components/parameters</c>:
    /// a query parameter (<c>page</c>), a header parameter (<c>X-Correlation-Id</c>), and
    /// a cookie parameter (<c>session</c>).  Used to verify that all three non-path parameter
    /// locations resolve correctly and receive the correct binding attributes.
    /// </summary>
    public const string SearchWithAllNonPathComponentParametersYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /search:
            get:
              operationId: search
              parameters:
                - $ref: '#/components/parameters/PageQuery'
                - $ref: '#/components/parameters/CorrelationIdHeader'
                - $ref: '#/components/parameters/SessionCookie'
              responses:
                "200":
                  description: OK
        components:
          parameters:
            PageQuery:
              name: page
              in: query
              required: false
              schema:
                type: integer
            CorrelationIdHeader:
              name: X-Correlation-Id
              in: header
              required: false
              schema:
                type: string
            SessionCookie:
              name: session
              in: cookie
              required: false
              schema:
                type: string
        """;

    /// <summary>
    /// JSON variant of <see cref="SearchWithAllNonPathComponentParametersYaml"/>.
    /// </summary>
    public const string SearchWithAllNonPathComponentParametersJson = """
        {
          "openapi": "3.0.0",
          "info": {
            "title": "Test API",
            "version": "1.0.0"
          },
          "paths": {
            "/search": {
              "get": {
                "operationId": "search",
                "parameters": [
                  { "$ref": "#/components/parameters/PageQuery" },
                  { "$ref": "#/components/parameters/CorrelationIdHeader" },
                  { "$ref": "#/components/parameters/SessionCookie" }
                ],
                "responses": {
                  "200": { "description": "OK" }
                }
              }
            }
          },
          "components": {
            "parameters": {
              "PageQuery": {
                "name": "page",
                "in": "query",
                "required": false,
                "schema": { "type": "integer" }
              },
              "CorrelationIdHeader": {
                "name": "X-Correlation-Id",
                "in": "header",
                "required": false,
                "schema": { "type": "string" }
              },
              "SessionCookie": {
                "name": "session",
                "in": "cookie",
                "required": false,
                "schema": { "type": "string" }
              }
            }
          }
        }
        """;

    // ── Inline array-item schema fixtures ────────────────────────────────────
    //
    // These fixtures verify that inline object schemas used as the `items` of an
    // array property are collected and emitted as nested generated records.

    /// <summary>
    /// An operation whose inline 200 response contains an array property
    /// (<c>items</c>) whose item schema is itself an inline object.
    /// Expected nested records: <c>OkResponse</c>, <c>OkResponseItemsItem</c>.
    /// </summary>
    public const string GetRecipesWithInlineArrayItemYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /recipes:
            get:
              operationId: getRecipes
              responses:
                "200":
                  description: OK
                  content:
                    application/json:
                      schema:
                        type: object
                        required:
                          - items
                          - total
                        properties:
                          items:
                            type: array
                            items:
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
                          total:
                            type: integer
        """;

    /// <summary>
    /// An operation whose inline request body contains an array property
    /// (<c>entries</c>) whose item schema is an inline object.
    /// Expected nested records: <c>Request</c>, <c>RequestEntriesItem</c>.
    /// </summary>
    public const string BatchCreateWithInlineArrayItemYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /batch:
            post:
              operationId: batchCreate
              requestBody:
                required: true
                content:
                  application/json:
                    schema:
                      type: object
                      required:
                        - entries
                      properties:
                        entries:
                          type: array
                          items:
                            type: object
                            required:
                              - name
                            properties:
                              name:
                                type: string
              responses:
                "200":
                  description: OK
        """;

    /// <summary>
    /// An operation whose inline 200 response contains an array property whose
    /// item schema itself contains a nested inline object property (<c>metadata</c>).
    /// Expected: <c>OkResponse</c>, <c>OkResponseItemsItem</c>,
    /// <c>OkResponseItemsItemMetadata</c>.
    /// </summary>
    public const string GetItemsWithNestedInlineObjectInArrayItemYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /items:
            get:
              operationId: getItems
              responses:
                "200":
                  description: OK
                  content:
                    application/json:
                      schema:
                        type: object
                        required:
                          - items
                        properties:
                          items:
                            type: array
                            items:
                              type: object
                              required:
                                - id
                                - metadata
                              properties:
                                id:
                                  type: string
                                  format: uuid
                                metadata:
                                  type: object
                                  properties:
                                    source:
                                      type: string
        """;

    /// <summary>
    /// An operation whose inline 200 response contains an array property whose
    /// item schema itself contains a nested array property (<c>steps</c>) whose
    /// item schema is also an inline object.
    /// Expected: <c>OkResponse</c>, <c>OkResponseItemsItemStepsItem</c>,
    /// <c>OkResponseItemsItem</c>.
    /// </summary>
    public const string GetItemsWithNestedArrayInArrayItemYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /items:
            get:
              operationId: getItems
              responses:
                "200":
                  description: OK
                  content:
                    application/json:
                      schema:
                        type: object
                        required:
                          - items
                        properties:
                          items:
                            type: array
                            items:
                              type: object
                              required:
                                - id
                                - steps
                              properties:
                                id:
                                  type: string
                                  format: uuid
                                steps:
                                  type: array
                                  items:
                                    type: object
                                    required:
                                      - text
                                    properties:
                                      text:
                                        type: string
        """;

    /// <summary>
    /// A component schema (<c>Catalog</c>) with an array property (<c>entries</c>)
    /// whose item schema is an inline object.
    /// Expected records: <c>CatalogEntriesItem</c>, <c>Catalog</c>.
    /// </summary>
    public const string GetCatalogWithInlineArrayItemComponentYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /catalog:
            get:
              operationId: getCatalog
              responses:
                "200":
                  description: OK
                  content:
                    application/json:
                      schema:
                        $ref: '#/components/schemas/Catalog'
        components:
          schemas:
            Catalog:
              type: object
              required:
                - entries
              properties:
                entries:
                  type: array
                  items:
                    type: object
                    required:
                      - id
                      - title
                    properties:
                      id:
                        type: string
                        format: uuid
                      title:
                        type: string
        """;

    /// <summary>
    /// An inline 200 response with a <c>matrix</c> property that is an array-of-arrays whose
    /// inner item schema is an inline object.
    /// Expected: handler record <c>OkResponseMatrixItemItem</c>; property type <c>OkResponseMatrixItemItem[][]</c>.
    /// </summary>
    public const string GetMatrixWithNestedArrayItemYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /matrix:
            get:
              operationId: getMatrix
              responses:
                "200":
                  description: OK
                  content:
                    application/json:
                      schema:
                        type: object
                        required:
                          - matrix
                        properties:
                          matrix:
                            type: array
                            items:
                              type: array
                              items:
                                type: object
                                required:
                                  - value
                                properties:
                                  value:
                                    type: string
        """;

    /// <summary>
    /// A component schema (<c>Grid</c>) with a <c>cells</c> property that is an array-of-arrays
    /// whose inner item schema is an inline object.
    /// Expected records: <c>GridCellsItemItem</c>, <c>Grid</c>; property type <c>GridCellsItemItem[][]</c>.
    /// </summary>
    public const string GetGridWithNestedArrayItemComponentYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /grid:
            get:
              operationId: getGrid
              responses:
                "200":
                  description: OK
                  content:
                    application/json:
                      schema:
                        $ref: '#/components/schemas/Grid'
        components:
          schemas:
            Grid:
              type: object
              required:
                - cells
              properties:
                cells:
                  type: array
                  items:
                    type: array
                    items:
                      type: object
                      required:
                        - value
                      properties:
                        value:
                          type: string
        """;

    /// <summary>
    /// An operation-scoped inline response (<c>OkResponse</c>) with a <c>statuses</c> array
    /// property whose item schema is an inline enum.
    /// Handler-local enum emission is not yet supported, so the expected fallback is
    /// <c>string[]</c> (the safe primitive fallback for a <c>type: string</c> enum).
    /// </summary>
    public const string GetStatusesWithInlineEnumArrayItemYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /statuses:
            get:
              operationId: getStatuses
              responses:
                "200":
                  description: OK
                  content:
                    application/json:
                      schema:
                        type: object
                        required:
                          - statuses
                        properties:
                          statuses:
                            type: array
                            items:
                              type: string
                              enum:
                                - pending
                                - active
                                - closed
        """;

    /// <summary>
    /// An operation-scoped inline response (<c>OkResponse</c>) with a direct inline enum
    /// property. Handler-local enum emission is not yet supported, so the expected fallback is
    /// <c>string</c> (the safe primitive fallback for a <c>type: string</c> enum).
    /// </summary>
    public const string GetStatusSummaryWithInlineEnumPropertyYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /status-summary:
            get:
              operationId: getStatusSummary
              responses:
                "200":
                  description: OK
                  content:
                    application/json:
                      schema:
                        type: object
                        required:
                          - status
                        properties:
                          status:
                            type: string
                            enum:
                              - pending
                              - active
                              - closed
        """;

    /// <summary>
    /// A component schema (<c>Report</c>) with a <c>statuses</c> array property whose item
    /// schema is an inline enum.
    /// Expected: top-level enum <c>ReportStatusesItem</c>; property type <c>ReportStatusesItem[]</c>.
    /// </summary>
    public const string GetReportWithInlineEnumArrayItemComponentYaml = """
        openapi: "3.0.0"
        info:
          title: Test API
          version: "1.0.0"
        paths:
          /report:
            get:
              operationId: getReport
              responses:
                "200":
                  description: OK
                  content:
                    application/json:
                      schema:
                        $ref: '#/components/schemas/Report'
        components:
          schemas:
            Report:
              type: object
              required:
                - statuses
              properties:
                statuses:
                  type: array
                  items:
                    type: string
                    enum:
                      - pending
                      - active
                      - closed
        """;
}