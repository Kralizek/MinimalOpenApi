using MinimalOpenAPI.Abstractions.Models;

namespace MinimalOpenAPI.Generator.CodeGen;

internal static class SchemaReferenceValidator
{
    public static IReadOnlyList<string> FindUnresolvedReferences(OpenApiDocument document)
    {
        var unresolved = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<OpenApiSchema>();

        foreach (var schema in document.Schemas.Values)
            Visit(schema, document.Schemas, unresolved, visited);

        foreach (var operation in document.Operations)
        {
            foreach (var parameter in operation.Parameters)
                Visit(parameter.Schema, document.Schemas, unresolved, visited);

            if (operation.RequestBody?.Schema is { } requestSchema)
                Visit(requestSchema, document.Schemas, unresolved, visited);

            foreach (var response in operation.Responses)
            {
                if (response.Schema is { } responseSchema)
                    Visit(responseSchema, document.Schemas, unresolved, visited);
            }
        }

        return unresolved.OrderBy(static reference => reference, StringComparer.Ordinal).ToArray();
    }

    private static void Visit(
        OpenApiSchema schema,
        IReadOnlyDictionary<string, OpenApiSchema> components,
        HashSet<string> unresolved,
        HashSet<OpenApiSchema> visited)
    {
        if (!visited.Add(schema))
            return;

        if (!string.IsNullOrWhiteSpace(schema.Reference))
        {
            if (!components.TryGetValue(schema.Reference!, out var referenced))
            {
                unresolved.Add(schema.Reference!);
                return;
            }

            Visit(referenced, components, unresolved, visited);
        }

        foreach (var property in schema.Properties.Values)
            Visit(property, components, unresolved, visited);

        foreach (var part in schema.AllOf)
            Visit(part, components, unresolved, visited);

        if (schema.Items is { } items)
            Visit(items, components, unresolved, visited);

        if (schema.AdditionalProperties is { } additionalProperties)
            Visit(additionalProperties, components, unresolved, visited);
    }
}
