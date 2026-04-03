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
}