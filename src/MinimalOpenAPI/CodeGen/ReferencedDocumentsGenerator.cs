using System.Text;

using Microsoft.CodeAnalysis;

namespace MinimalOpenAPI.Generator.CodeGen;

/// <summary>
/// Registers the OpenAPI documents and handler implementations declared in referenced assemblies
/// (for example contract class libraries) from the consuming project.
/// <para>
/// Every generated document marks its assembly with <c>[assembly: MinimalOpenApiDocument(typeof(...))]</c>,
/// and every project implementing handlers or endpoint configurations of referenced documents marks
/// its assembly with <c>[assembly: MinimalOpenApiImplementations(typeof(...))]</c>. This generator reads
/// those attributes (and the <c>MinimalOpenApiContracts</c> ones) from the compilation's metadata
/// references and emits one module initializer that calls each referenced entry point's
/// <c>Register()</c> directly. A library's own module initializer only runs once one of its members is
/// accessed, which may happen after <c>AddMinimalOpenApi()</c>; the consuming project's initializer
/// always runs when that project's code first runs (for an application, before <c>Main</c>), and the
/// direct calls load each library. Discovery happens at compile time, so no runtime assembly
/// scanning is involved.
/// </para>
/// </summary>
internal static class ReferencedDocumentsGenerator
{
    private const string RuntimeAssemblyName = "MinimalOpenAPI";
    private const string DocumentAttributeName = "MinimalOpenAPI.MinimalOpenApiDocumentAttribute";
    private const string ImplementationsAttributeName = "MinimalOpenAPI.MinimalOpenApiImplementationsAttribute";
    private const string ContractsAttributeName = "MinimalOpenAPI.MinimalOpenApiContractsAttribute";

    /// <summary>Hint name of the generated source.</summary>
    public const string HintName = "MinimalOpenApi/MinimalOpenApi.ReferencedDocuments.g.cs";

    /// <summary>
    /// Collects the MinimalOpenAPI metadata declared by referenced assemblies. The value is
    /// equatable so dependent outputs are regenerated only when that metadata changes.
    /// </summary>
    public static ReferencedMetadata Collect(Compilation compilation, CancellationToken cancellationToken)
    {
        var documents = new SortedSet<string>(StringComparer.Ordinal);
        var documentAssemblies = new SortedSet<string>(StringComparer.Ordinal);
        var implementations = new SortedSet<string>(StringComparer.Ordinal);
        var unimplementedHandlers = new SortedSet<ReferencedType>();
        var implementedConfigurations = new SortedSet<ReferencedType>();
        var implementedHandlersElsewhere = new SortedSet<ReferencedImplementation>();
        var implementedConfigurationsElsewhere = new SortedSet<ReferencedImplementation>();
        var contracts = new SortedSet<ReferencedContracts>();

        foreach (var reference in compilation.References)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assembly
                || assembly.Name == RuntimeAssemblyName
                || !ReferencesRuntime(assembly))
                continue;

            foreach (var attribute in assembly.GetAttributes())
            {
                switch (attribute.AttributeClass?.ToDisplayString())
                {
                    case DocumentAttributeName when TryGetEntryPoint(attribute, out var entryPoint):
                        documents.Add(entryPoint);
                        documentAssemblies.Add(assembly.Name);
                        foreach (var type in GetTypes(attribute, "UnimplementedHandlers"))
                            unimplementedHandlers.Add(new ReferencedType(type.ToDisplayString(), assembly.Name));
                        foreach (var type in GetTypes(attribute, "ImplementedConfigurations"))
                            implementedConfigurations.Add(new ReferencedType(type.ToDisplayString(), assembly.Name));
                        break;

                    case ImplementationsAttributeName when TryGetEntryPoint(attribute, out var entryPoint):
                        implementations.Add(entryPoint);
                        foreach (var type in GetTypes(attribute, "Handlers"))
                            implementedHandlersElsewhere.Add(new ReferencedImplementation(type.ToDisplayString(), assembly.Name));
                        foreach (var type in GetTypes(attribute, "Configurations"))
                            implementedConfigurationsElsewhere.Add(new ReferencedImplementation(type.ToDisplayString(), assembly.Name));
                        break;

                    case ContractsAttributeName
                        when attribute.ConstructorArguments.Length == 3
                            && attribute.ConstructorArguments[0].Value is string specName
                            && attribute.ConstructorArguments[1].Value is string contractsNamespace
                            && attribute.ConstructorArguments[2].Value is string documentHash:
                        contracts.Add(new ReferencedContracts(specName, contractsNamespace, documentHash, assembly.Name));
                        break;
                }
            }
        }

        return new ReferencedMetadata(
            documents.ToArray(),
            documentAssemblies.ToArray(),
            implementations.ToArray(),
            unimplementedHandlers.ToArray(),
            implementedConfigurations.ToArray(),
            implementedHandlersElsewhere.ToArray(),
            implementedConfigurationsElsewhere.ToArray(),
            contracts.ToArray());
    }

    /// <summary>Generates the module initializer, or <see langword="null"/> when nothing is referenced.</summary>
    public static string? Generate(ReferencedMetadata referenced, string rootNamespace)
    {
        if (referenced.DocumentEntryPoints.Count == 0 && referenced.ImplementationEntryPoints.Count == 0)
            return null;

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine($"namespace {rootNamespace};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Registers the MinimalOpenAPI documents and handler implementations declared in referenced");
        sb.AppendLine("/// assemblies, so that <c>AddMinimalOpenApi()</c> and <c>MapMinimalOpenApiEndpoints()</c> include them.");
        sb.AppendLine("/// </summary>");
        TypeMapper.AppendGeneratedAttributes(sb);
        sb.AppendLine("internal static class MinimalOpenApiReferencedDocuments");
        sb.AppendLine("{");
        sb.AppendLine("    [global::System.Runtime.CompilerServices.ModuleInitializer]");
        sb.AppendLine("    internal static void Register()");
        sb.AppendLine("    {");
        // Documents first, so the implementations registered next replace their default handlers.
        foreach (var name in referenced.DocumentEntryPoints)
            sb.AppendLine($"        {name}.Register();");
        foreach (var name in referenced.ImplementationEntryPoints)
            sb.AppendLine($"        {name}.Register();");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static bool TryGetEntryPoint(AttributeData attribute, out string entryPoint)
    {
        if (attribute.ConstructorArguments.Length == 1
            && attribute.ConstructorArguments[0].Value is INamedTypeSymbol type
            && type.DeclaredAccessibility == Accessibility.Public)
        {
            entryPoint = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return true;
        }

        entryPoint = string.Empty;
        return false;
    }

    private static IEnumerable<INamedTypeSymbol> GetTypes(AttributeData attribute, string propertyName)
    {
        foreach (var named in attribute.NamedArguments)
        {
            if (named.Key != propertyName || named.Value.Kind != TypedConstantKind.Array)
                continue;

            foreach (var value in named.Value.Values)
            {
                if (value.Value is INamedTypeSymbol type)
                    yield return type;
            }
        }
    }

    private static bool ReferencesRuntime(IAssemblySymbol assembly)
    {
        foreach (var module in assembly.Modules)
        {
            foreach (var referenced in module.ReferencedAssemblies)
            {
                if (referenced.Name == RuntimeAssemblyName)
                    return true;
            }
        }

        return false;
    }
}

/// <summary>Equatable, ordered MinimalOpenAPI metadata read from referenced assemblies.</summary>
internal sealed class ReferencedMetadata(
    IReadOnlyList<string> documentEntryPoints,
    IReadOnlyList<string> documentAssemblies,
    IReadOnlyList<string> implementationEntryPoints,
    IReadOnlyList<ReferencedType> unimplementedHandlers,
    IReadOnlyList<ReferencedType> implementedConfigurations,
    IReadOnlyList<ReferencedImplementation> implementedHandlersElsewhere,
    IReadOnlyList<ReferencedImplementation> implementedConfigurationsElsewhere,
    IReadOnlyList<ReferencedContracts> contracts) : IEquatable<ReferencedMetadata>
{
    /// <summary>Fully qualified (<c>global::</c>) names of the referenced document entry points.</summary>
    public IReadOnlyList<string> DocumentEntryPoints { get; } = documentEntryPoints;

    /// <summary>Names of the referenced assemblies that declare at least one document.</summary>
    public IReadOnlyList<string> DocumentAssemblies { get; } = documentAssemblies;

    /// <summary>Fully qualified (<c>global::</c>) names of the referenced implementation entry points.</summary>
    public IReadOnlyList<string> ImplementationEntryPoints { get; } = implementationEntryPoints;

    /// <summary>Handler bases that the declaring assembly does not implement.</summary>
    public IReadOnlyList<ReferencedType> UnimplementedHandlers { get; } = unimplementedHandlers;

    /// <summary>Endpoint configuration bases that the declaring assembly implements.</summary>
    public IReadOnlyList<ReferencedType> ImplementedConfigurations { get; } = implementedConfigurations;

    /// <summary>Handler bases implemented by other referenced assemblies (not the declaring one).</summary>
    public IReadOnlyList<ReferencedImplementation> ImplementedHandlersElsewhere { get; } = implementedHandlersElsewhere;

    /// <summary>Endpoint configuration bases implemented by other referenced assemblies.</summary>
    public IReadOnlyList<ReferencedImplementation> ImplementedConfigurationsElsewhere { get; } = implementedConfigurationsElsewhere;

    /// <summary>Contract types generated in referenced assemblies.</summary>
    public IReadOnlyList<ReferencedContracts> Contracts { get; } = contracts;

    public bool Equals(ReferencedMetadata? other)
        => other is not null
            && DocumentEntryPoints.SequenceEqual(other.DocumentEntryPoints, StringComparer.Ordinal)
            && DocumentAssemblies.SequenceEqual(other.DocumentAssemblies, StringComparer.Ordinal)
            && ImplementationEntryPoints.SequenceEqual(other.ImplementationEntryPoints, StringComparer.Ordinal)
            && UnimplementedHandlers.SequenceEqual(other.UnimplementedHandlers)
            && ImplementedConfigurations.SequenceEqual(other.ImplementedConfigurations)
            && ImplementedHandlersElsewhere.SequenceEqual(other.ImplementedHandlersElsewhere)
            && ImplementedConfigurationsElsewhere.SequenceEqual(other.ImplementedConfigurationsElsewhere)
            && Contracts.SequenceEqual(other.Contracts);

    public override bool Equals(object? obj) => Equals(obj as ReferencedMetadata);

    public override int GetHashCode()
    {
        var hash = 17;
        foreach (var name in DocumentEntryPoints)
            hash = unchecked((hash * 31) + StringComparer.Ordinal.GetHashCode(name));
        foreach (var name in ImplementationEntryPoints)
            hash = unchecked((hash * 31) + StringComparer.Ordinal.GetHashCode(name));
        hash = unchecked((hash * 31) + UnimplementedHandlers.Count);
        hash = unchecked((hash * 31) + ImplementedConfigurations.Count);
        hash = unchecked((hash * 31) + ImplementedHandlersElsewhere.Count);
        hash = unchecked((hash * 31) + ImplementedConfigurationsElsewhere.Count);
        hash = unchecked((hash * 31) + Contracts.Count);
        return hash;
    }
}

/// <summary>A generated base class (display name) and the assembly whose document declares it.</summary>
internal sealed record ReferencedType(string TypeName, string AssemblyName) : IComparable<ReferencedType>
{
    public int CompareTo(ReferencedType? other)
    {
        if (other is null)
            return 1;
        var result = string.CompareOrdinal(TypeName, other.TypeName);
        return result != 0 ? result : string.CompareOrdinal(AssemblyName, other.AssemblyName);
    }
}

/// <summary>A generated base class (display name) implemented in another referenced assembly.</summary>
internal sealed record ReferencedImplementation(string BaseTypeName, string ImplementingAssemblyName) : IComparable<ReferencedImplementation>
{
    public int CompareTo(ReferencedImplementation? other)
    {
        if (other is null)
            return 1;
        var result = string.CompareOrdinal(BaseTypeName, other.BaseTypeName);
        return result != 0 ? result : string.CompareOrdinal(ImplementingAssemblyName, other.ImplementingAssemblyName);
    }
}

/// <summary>Contract types generated for a document in a referenced assembly.</summary>
internal sealed record ReferencedContracts(string SpecName, string ContractsNamespace, string DocumentHash, string AssemblyName)
    : IComparable<ReferencedContracts>
{
    public int CompareTo(ReferencedContracts? other)
    {
        if (other is null)
            return 1;
        var result = string.CompareOrdinal(SpecName, other.SpecName);
        if (result == 0)
            result = string.CompareOrdinal(AssemblyName, other.AssemblyName);
        if (result == 0)
            result = string.CompareOrdinal(ContractsNamespace, other.ContractsNamespace);
        return result != 0 ? result : string.CompareOrdinal(DocumentHash, other.DocumentHash);
    }
}