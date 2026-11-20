using MinimalOpenAPI.Abstractions.Models;
using MinimalOpenAPI.Parser.Json;
using MinimalOpenAPI.Parser.Yaml;

namespace MinimalOpenAPI.Generator.Tests;

[TestFixture]
public sealed class SchemaCompositionParserTests
{
    [Test]
    public async Task Json_preserves_ordered_compositions_discriminator_and_empty_keywords()
    {
        var document = await new JsonOpenApiParser().ParseAsync(JsonDocument);

        AssertCompositions(document.Schemas);
    }

    [Test]
    public async Task Yaml_preserves_ordered_compositions_discriminator_and_empty_keywords()
    {
        var document = await new YamlOpenApiParser().ParseAsync(YamlDocument);

        AssertCompositions(document.Schemas);
    }

    private static void AssertCompositions(IReadOnlyDictionary<string, OpenApiSchema> schemas)
    {
        var pet = schemas["Pet"];
        var oneOf = pet.OneOf!;
        Assert.That(oneOf, Has.Count.EqualTo(2));
        Assert.That(oneOf[0].Reference, Is.EqualTo("Cat"));
        Assert.That(oneOf[0].RawReference, Is.EqualTo("#/components/schemas/Cat"));
        Assert.That(oneOf[1].Reference, Is.EqualTo("Dog"));
        Assert.That(pet.AnyOf, Is.Null);
        var discriminator = pet.Discriminator!;
        Assert.That(discriminator.PropertyName, Is.EqualTo("kind"));
        Assert.That(discriminator.Mapping["kitty"], Is.EqualTo("#/components/schemas/Cat"));
        Assert.That(discriminator.Mapping["puppy"], Is.EqualTo("#/components/schemas/Dog"));

        var value = schemas["Value"];
        Assert.That(value.OneOf, Is.Null);
        var anyOf = value.AnyOf!;
        Assert.That(anyOf, Has.Count.EqualTo(2));
        Assert.That(anyOf[0].Type, Is.EqualTo("string"));
        Assert.That(anyOf[1].Type, Is.EqualTo("integer"));

        var pets = schemas["Wrapper"].Properties["pets"];
        var petAlternatives = pets.Items!.AnyOf!;
        Assert.That(petAlternatives, Has.Count.EqualTo(2));
        Assert.That(petAlternatives[0].Reference, Is.EqualTo("Cat"));
        Assert.That(petAlternatives[1].Type, Is.EqualTo("boolean"));

        var nested = schemas["Composed"].AllOf.Single().Properties["pet"];
        Assert.That(nested.OneOf, Has.Count.EqualTo(2));
        Assert.That(schemas["Empty"].OneOf, Is.Empty);
        Assert.That(schemas["Plain"].OneOf, Is.Null);
        Assert.That(schemas["Plain"].AnyOf, Is.Null);
    }

    private const string JsonDocument = """
        {
          "openapi": "3.1.0",
          "components": {
            "schemas": {
              "Cat": {"type": "object"},
              "Dog": {"type": "object"},
              "Pet": {
                "oneOf": [
                  {"$ref": "#/components/schemas/Cat"},
                  {"$ref": "#/components/schemas/Dog"}
                ],
                "discriminator": {
                  "propertyName": "kind",
                  "mapping": {
                    "kitty": "#/components/schemas/Cat",
                    "puppy": "#/components/schemas/Dog"
                  }
                }
              },
              "Value": {
                "anyOf": [
                  {"type": "string"},
                  {"type": "integer"}
                ]
              },
              "Wrapper": {
                "type": "object",
                "properties": {
                  "pets": {
                    "type": "array",
                    "items": {
                      "anyOf": [
                        {"$ref": "#/components/schemas/Cat"},
                        {"type": "boolean"}
                      ]
                    }
                  }
                }
              },
              "Composed": {
                "allOf": [
                  {
                    "type": "object",
                    "properties": {
                      "pet": {
                        "oneOf": [
                          {"$ref": "#/components/schemas/Cat"},
                          {"$ref": "#/components/schemas/Dog"}
                        ]
                      }
                    }
                  }
                ]
              },
              "Empty": {"oneOf": []},
              "Plain": {"type": "string"}
            }
          }
        }
        """;

    private const string YamlDocument = """
        openapi: 3.1.0
        components:
          schemas:
            Cat:
              type: object
            Dog:
              type: object
            Pet:
              oneOf:
                - $ref: '#/components/schemas/Cat'
                - $ref: '#/components/schemas/Dog'
              discriminator:
                propertyName: kind
                mapping:
                  kitty: '#/components/schemas/Cat'
                  puppy: '#/components/schemas/Dog'
            Value:
              anyOf:
                - type: string
                - type: integer
            Wrapper:
              type: object
              properties:
                pets:
                  type: array
                  items:
                    anyOf:
                      - $ref: '#/components/schemas/Cat'
                      - type: boolean
            Composed:
              allOf:
                - type: object
                  properties:
                    pet:
                      oneOf:
                        - $ref: '#/components/schemas/Cat'
                        - $ref: '#/components/schemas/Dog'
            Empty:
              oneOf: []
            Plain:
              type: string
        """;
}