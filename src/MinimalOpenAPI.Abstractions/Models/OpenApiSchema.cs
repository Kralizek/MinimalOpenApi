namespace MinimalOpenAPI.Abstractions.Models;

/// <summary>Describes the shape of a value in an OpenAPI document, mapping to a JSON Schema subset.</summary>
public sealed class OpenApiSchema
{
    /// <summary>The JSON Schema primitive type (e.g. <c>string</c>, <c>integer</c>, <c>array</c>, <c>object</c>).</summary>
    public string? Type { get; init; }

    /// <summary>An optional format hint that refines the primitive type (e.g. <c>uuid</c>, <c>date-time</c>, <c>int64</c>).</summary>
    public string? Format { get; init; }

    /// <summary>Whether the value is allowed to be <see langword="null"/>.</summary>
    public bool Nullable { get; init; }

    /// <summary>
    /// The name of a component schema referenced via <c>$ref</c> (e.g. <c>TodoItem</c>).
    /// When set, all other fields are ignored.
    /// </summary>
    public string? Reference { get; init; }

    /// <summary>For <c>array</c>-typed schemas, describes the element type.</summary>
    public OpenApiSchema? Items { get; init; }

    /// <summary>Named properties of an <c>object</c>-typed schema, keyed by property name.</summary>
    public Dictionary<string, OpenApiSchema> Properties { get; init; } = new Dictionary<string, OpenApiSchema>(StringComparer.Ordinal);

    /// <summary>The set of property names that are required on this schema.</summary>
    public List<string> Required { get; init; } = new List<string>();

    /// <summary>
    /// The set of allowed values for this schema (OpenAPI <c>enum</c> keyword).
    /// When non-<see langword="null"/>, the schema represents an enumeration.
    /// </summary>
    public List<string>? Enum { get; init; }

    /// <summary>Minimum allowed length for a <c>string</c>-typed schema (<c>minLength</c> keyword).</summary>
    public int? MinLength { get; init; }

    /// <summary>Maximum allowed length for a <c>string</c>-typed schema (<c>maxLength</c> keyword).</summary>
    public int? MaxLength { get; init; }

    /// <summary>Regular-expression pattern for a <c>string</c>-typed schema (<c>pattern</c> keyword).</summary>
    public string? Pattern { get; init; }

    /// <summary>Inclusive lower bound for an <c>integer</c> or <c>number</c>-typed schema (<c>minimum</c> keyword).</summary>
    public double? Minimum { get; init; }

    /// <summary>Inclusive upper bound for an <c>integer</c> or <c>number</c>-typed schema (<c>maximum</c> keyword).</summary>
    public double? Maximum { get; init; }

    /// <summary>Minimum number of items for an <c>array</c>-typed schema (<c>minItems</c> keyword).</summary>
    public int? MinItems { get; init; }

    /// <summary>Maximum number of items for an <c>array</c>-typed schema (<c>maxItems</c> keyword).</summary>
    public int? MaxItems { get; init; }

    /// <summary>
    /// Schema for the values of a free-form map (<c>additionalProperties</c> keyword, object form).
    /// When non-<see langword="null"/> and <see cref="Properties"/> is empty, the schema
    /// represents a <c>Dictionary&lt;string, T&gt;</c> where <c>T</c> is mapped from this schema.
    /// </summary>
    public OpenApiSchema? AdditionalProperties { get; init; }

    /// <summary>
    /// Whether the schema permits any additional properties (boolean <c>additionalProperties: true</c>).
    /// When <see langword="true"/> and <see cref="Properties"/> is non-empty, generated records include
    /// a <c>[JsonExtensionData] Dictionary&lt;string, JsonElement&gt;?</c> property to capture
    /// extra key-value pairs at runtime.
    /// When <see langword="true"/> and <see cref="Properties"/> is empty, the schema maps to
    /// <c>Dictionary&lt;string, object&gt;</c> at the use site (no record is generated for the
    /// component).
    /// </summary>
    public bool AdditionalPropertiesAllowed { get; init; }
}