using System.Text.Json;
using System.Text.Json.Serialization;

public union Primitive(int, bool);

[JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
public union Animal(Dog, Cat);

public sealed record Dog(
    string Name,
    [property: JsonRequired] string Breed);

public sealed record Cat(
    string Name,
    [property: JsonRequired] int Lives);

[JsonUnion(TypeClassifier = typeof(EventClassifier))]
public union Event(Created, Deleted);

public sealed record Created(string Kind, int Id);

public sealed record Deleted(string Kind, int Id);

public sealed class EventClassifier : JsonTypeClassifierFactory<Event>
{
    public override JsonTypeClassifier CreateJsonClassifier(
        JsonTypeClassifierContext context,
        JsonSerializerOptions options)
        => static (ref Utf8JsonReader reader) =>
        {
            var input = reader;
            if (input.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected an object with a 'kind' discriminator.");

            Type? caseType = null;
            while (input.Read() && input.TokenType == JsonTokenType.PropertyName)
            {
                var isDiscriminator = input.ValueTextEquals("kind"u8);
                input.Read();

                if (!isDiscriminator)
                {
                    input.Skip();
                    continue;
                }

                if (caseType is not null || input.TokenType != JsonTokenType.String)
                    throw new JsonException("Expected one string 'kind' discriminator.");

                caseType = input.GetString() switch
                {
                    "created" => typeof(Created),
                    "deleted" => typeof(Deleted),
                    _ => throw new JsonException("Unknown 'kind' discriminator.")
                };
            }

            return caseType ?? throw new JsonException("Missing 'kind' discriminator.");
        };
}

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(Primitive))]
[JsonSerializable(typeof(Animal))]
[JsonSerializable(typeof(Event))]
internal partial class UnionJsonContext : JsonSerializerContext;