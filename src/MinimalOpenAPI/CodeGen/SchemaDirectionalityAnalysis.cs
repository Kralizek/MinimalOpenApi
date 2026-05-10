using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using MinimalOpenAPI.Abstractions.Models;

namespace MinimalOpenAPI.Generator.CodeGen;

internal enum ReadWriteSchemaHandling
{
    Ignore,
    Auto,
    Split
}

internal enum SchemaGenerationScope
{
    Neutral,
    Request,
    Response
}

internal sealed class SchemaDirectionalityAnalysis
{
    private readonly ReadWriteSchemaHandling _handling;
    private readonly IReadOnlyDictionary<string, OpenApiSchema> _schemas;
    private readonly HashSet<string> _directionalSchemas;
    private readonly HashSet<string> _requestScopedSchemas;
    private readonly HashSet<string> _responseScopedSchemas;

    private SchemaDirectionalityAnalysis(
        ReadWriteSchemaHandling handling,
        IReadOnlyDictionary<string, OpenApiSchema> schemas,
        HashSet<string> directionalSchemas,
        HashSet<string> requestScopedSchemas,
        HashSet<string> responseScopedSchemas)
    {
        _handling = handling;
        _schemas = schemas;
        _directionalSchemas = directionalSchemas;
        _requestScopedSchemas = requestScopedSchemas;
        _responseScopedSchemas = responseScopedSchemas;
    }

    public ReadWriteSchemaHandling Handling => _handling;

    public static SchemaDirectionalityAnalysis Create(
        IReadOnlyDictionary<string, OpenApiSchema> schemas,
        IReadOnlyList<OpenApiOperation> operations,
        ReadWriteSchemaHandling handling)
    {
        var directionalSchemas = handling == ReadWriteSchemaHandling.Ignore
            ? new HashSet<string>(StringComparer.Ordinal)
            : ComputeDirectionalSchemas(schemas);

        var requestScopedSchemas = new HashSet<string>(StringComparer.Ordinal);
        var responseScopedSchemas = new HashSet<string>(StringComparer.Ordinal);

        if (handling != ReadWriteSchemaHandling.Ignore)
        {
            foreach (var operation in operations)
            {
                TraverseOperationSchemaGraph(
                    operation.RequestBody?.Schema,
                    SchemaGenerationScope.Request,
                    handling,
                    schemas,
                    directionalSchemas,
                    requestScopedSchemas,
                    responseScopedSchemas);

                foreach (var response in operation.Responses)
                {
                    TraverseOperationSchemaGraph(
                        response.Schema,
                        SchemaGenerationScope.Response,
                        handling,
                        schemas,
                        directionalSchemas,
                        requestScopedSchemas,
                        responseScopedSchemas);
                }
            }
        }

        return new SchemaDirectionalityAnalysis(
            handling,
            schemas,
            directionalSchemas,
            requestScopedSchemas,
            responseScopedSchemas);
    }

    public bool ShouldFilterProperties(SchemaGenerationScope scope)
        => _handling != ReadWriteSchemaHandling.Ignore && scope != SchemaGenerationScope.Neutral;

    public bool ShouldIncludeProperty(OpenApiSchema propertySchema, SchemaGenerationScope scope)
    {
        if (!ShouldFilterProperties(scope))
            return true;

        return scope switch
        {
            SchemaGenerationScope.Request => !propertySchema.ReadOnly,
            SchemaGenerationScope.Response => !propertySchema.WriteOnly,
            _ => true
        };
    }

    public bool ShouldGenerateScope(string schemaName, SchemaGenerationScope scope) => scope switch
    {
        SchemaGenerationScope.Request => _requestScopedSchemas.Contains(schemaName) && IsScopeEligible(schemaName),
        SchemaGenerationScope.Response => _responseScopedSchemas.Contains(schemaName) && IsScopeEligible(schemaName),
        _ => true
    };

    public string ResolveSchemaReference(string referenceName, SchemaGenerationScope scope)
    {
        if (scope == SchemaGenerationScope.Request
            && _requestScopedSchemas.Contains(referenceName)
            && IsScopeEligible(referenceName))
            return ScopedSchemaName(referenceName, SchemaGenerationScope.Request);

        if (scope == SchemaGenerationScope.Response
            && _responseScopedSchemas.Contains(referenceName)
            && IsScopeEligible(referenceName))
            return ScopedSchemaName(referenceName, SchemaGenerationScope.Response);

        return referenceName;
    }

    private bool IsScopeEligible(string schemaName)
        => _schemas.TryGetValue(schemaName, out var schema) && schema.Enum is null;

    public static string ScopedSchemaName(string schemaName, SchemaGenerationScope scope) => scope switch
    {
        SchemaGenerationScope.Request => $"{schemaName}Request",
        SchemaGenerationScope.Response => $"{schemaName}Response",
        _ => schemaName
    };

    private static HashSet<string> ComputeDirectionalSchemas(IReadOnlyDictionary<string, OpenApiSchema> schemas)
    {
        var directionalSchemas = new HashSet<string>(StringComparer.Ordinal);
        var memo = new Dictionary<string, bool>(StringComparer.Ordinal);
        var visiting = new HashSet<string>(StringComparer.Ordinal);

        foreach (var schemaName in schemas.Keys)
        {
            if (NeedsDirectionality(schemaName, schemas, memo, visiting))
                directionalSchemas.Add(schemaName);
        }

        return directionalSchemas;
    }

    private static bool NeedsDirectionality(
        string schemaName,
        IReadOnlyDictionary<string, OpenApiSchema> schemas,
        Dictionary<string, bool> memo,
        HashSet<string> visiting)
    {
        if (!schemas.TryGetValue(schemaName, out var schema))
            return false;

        if (memo.TryGetValue(schemaName, out var cached))
            return cached;

        if (!visiting.Add(schemaName))
            return false;

        var inlineVisiting = new HashSet<OpenApiSchema>(ReferenceEqualityComparer.Instance);
        var needsDirectionality = NeedsDirectionality(schema, schemas, memo, visiting, inlineVisiting);

        visiting.Remove(schemaName);
        memo[schemaName] = needsDirectionality;
        return needsDirectionality;
    }

    private static bool NeedsDirectionality(
        OpenApiSchema schema,
        IReadOnlyDictionary<string, OpenApiSchema> schemas,
        Dictionary<string, bool> memo,
        HashSet<string> visitingRefs,
        HashSet<OpenApiSchema> visitingInline)
    {
        if (!visitingInline.Add(schema))
            return false;

        if (schema.Properties.Any(p => p.Value.ReadOnly || p.Value.WriteOnly))
            return true;

        foreach (var child in EnumerateChildSchemas(schema))
        {
            if (child.Reference is { } reference)
            {
                if (NeedsDirectionality(reference, schemas, memo, visitingRefs))
                    return true;
                continue;
            }

            if (NeedsDirectionality(child, schemas, memo, visitingRefs, visitingInline))
                return true;
        }

        return false;
    }

    private static void TraverseOperationSchemaGraph(
        OpenApiSchema? schema,
        SchemaGenerationScope scope,
        ReadWriteSchemaHandling handling,
        IReadOnlyDictionary<string, OpenApiSchema> schemas,
        HashSet<string> directionalSchemas,
        HashSet<string> requestScopedSchemas,
        HashSet<string> responseScopedSchemas)
    {
        var visitedRefs = new HashSet<(string Name, SchemaGenerationScope Scope)>();
        var visitedInline = new HashSet<OpenApiSchema>(ReferenceEqualityComparer.Instance);
        TraverseSchema(
            schema,
            scope,
            handling,
            schemas,
            directionalSchemas,
            requestScopedSchemas,
            responseScopedSchemas,
            visitedRefs,
            visitedInline);
    }

    private static void TraverseSchema(
        OpenApiSchema? schema,
        SchemaGenerationScope scope,
        ReadWriteSchemaHandling handling,
        IReadOnlyDictionary<string, OpenApiSchema> schemas,
        HashSet<string> directionalSchemas,
        HashSet<string> requestScopedSchemas,
        HashSet<string> responseScopedSchemas,
        HashSet<(string Name, SchemaGenerationScope Scope)> visitedRefs,
        HashSet<OpenApiSchema> visitedInline)
    {
        if (schema is null)
            return;

        if (schema.Reference is { } reference)
        {
            if (!visitedRefs.Add((reference, scope)))
                return;

            if (scope == SchemaGenerationScope.Request
                && (handling == ReadWriteSchemaHandling.Split || directionalSchemas.Contains(reference)))
            {
                requestScopedSchemas.Add(reference);
            }

            if (scope == SchemaGenerationScope.Response
                && (handling == ReadWriteSchemaHandling.Split || directionalSchemas.Contains(reference)))
            {
                responseScopedSchemas.Add(reference);
            }

            if (schemas.TryGetValue(reference, out var targetSchema))
            {
                TraverseSchema(
                    targetSchema,
                    scope,
                    handling,
                    schemas,
                    directionalSchemas,
                    requestScopedSchemas,
                    responseScopedSchemas,
                    visitedRefs,
                    visitedInline);
            }

            return;
        }

        if (!visitedInline.Add(schema))
            return;

        foreach (var child in EnumerateChildSchemas(schema))
        {
            TraverseSchema(
                child,
                scope,
                handling,
                schemas,
                directionalSchemas,
                requestScopedSchemas,
                responseScopedSchemas,
                visitedRefs,
                visitedInline);
        }
    }

    private static IEnumerable<OpenApiSchema> EnumerateChildSchemas(OpenApiSchema schema)
    {
        foreach (var property in schema.Properties.Values)
            yield return property;

        if (schema.Items is not null)
            yield return schema.Items;

        if (schema.AdditionalProperties is not null)
            yield return schema.AdditionalProperties;

        foreach (var allOfSchema in schema.AllOf)
            yield return allOfSchema;
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<OpenApiSchema>
    {
        public static readonly ReferenceEqualityComparer Instance = new();

        public bool Equals(OpenApiSchema? x, OpenApiSchema? y) => ReferenceEquals(x, y);

        public int GetHashCode(OpenApiSchema obj) => RuntimeHelpers.GetHashCode(obj);
    }
}