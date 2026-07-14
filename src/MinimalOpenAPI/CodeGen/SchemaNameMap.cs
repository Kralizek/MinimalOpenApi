using System.Collections.Generic;
using System.Linq;

namespace MinimalOpenAPI.Generator.CodeGen;

/// <summary>
/// A pre-computed, document-level mapping from original OpenAPI component schema names to
/// their normalised C# type identifiers.
/// </summary>
/// <remarks>
/// Build this once per <c>components/schemas</c> dictionary before source emission so that
/// name collisions can be detected and reported as diagnostics, and all generators use the
/// same normalised names throughout code generation.
/// </remarks>
internal sealed class SchemaNameMap
{
    // Maps original OpenAPI schema name → normalised C# type name (empty string = failed to normalise).
    private readonly Dictionary<string, string> _map;

    private SchemaNameMap(Dictionary<string, string> map,
        IReadOnlyList<SchemaNameCollision> collisions,
        IReadOnlyList<string> unnormalisableNames)
    {
        _map = map;
        Collisions = collisions;
        UnnormalisableNames = unnormalisableNames;
    }

    /// <summary>
    /// Builds a <see cref="SchemaNameMap"/> from the keys of a
    /// <c>components/schemas</c> dictionary.
    /// </summary>
    public static SchemaNameMap Build(IEnumerable<string> schemaNames)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        var normalisedToOriginals = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var unnormalisableNames = new List<string>();

        foreach (var name in schemaNames)
        {
            var normalised = TypeMapper.NormalizeSchemaTypeName(name);
            if (normalised is null)
            {
                // Cannot produce a valid C# identifier for this name.
                map[name] = string.Empty;
                unnormalisableNames.Add(name);
                continue;
            }

            map[name] = normalised;

            if (!normalisedToOriginals.TryGetValue(normalised, out var originals))
            {
                originals = new List<string>();
                normalisedToOriginals[normalised] = originals;
            }

            originals.Add(name);
        }

        var collisions = normalisedToOriginals
            .Where(kvp => kvp.Value.Count > 1)
            .Select(kvp => new SchemaNameCollision(kvp.Value.ToArray(), kvp.Key))
            .ToList();

        return new SchemaNameMap(map, collisions, unnormalisableNames);
    }

    /// <summary>
    /// Returns the normalised C# type name for <paramref name="originalName"/>.
    /// Falls back to the original name when no mapping was registered (e.g. inline names
    /// composed inside the generators that are already valid C# identifiers).
    /// </summary>
    public string GetTypeName(string originalName)
    {
        if (_map.TryGetValue(originalName, out var normalised) && !string.IsNullOrEmpty(normalised))
            return normalised;

        // Fallback: return as-is.  This covers names that were passed in without being
        // registered (e.g. inline derived names already composed in the generators).
        return originalName;
    }

    /// <summary>
    /// Groups of original schema names whose normalised form is the same C# identifier.
    /// Emit <c>MOA012</c> for each entry.
    /// </summary>
    public IReadOnlyList<SchemaNameCollision> Collisions { get; }

    /// <summary>
    /// Original schema names that cannot be normalised to any valid C# identifier.
    /// Emit <c>MOA013</c> for each entry.
    /// </summary>
    public IReadOnlyList<string> UnnormalisableNames { get; }

    /// <summary>Returns <see langword="true"/> when at least one collision was detected.</summary>
    public bool HasCollisions => Collisions.Count > 0;

    /// <summary>Returns <see langword="true"/> when at least one name could not be normalised.</summary>
    public bool HasUnnormalisableNames => UnnormalisableNames.Count > 0;
}

/// <summary>
/// Records a set of original OpenAPI schema names that all normalise to the same C# type name.
/// </summary>
internal sealed record SchemaNameCollision(
    IReadOnlyList<string> OriginalNames,
    string NormalisedTypeName);