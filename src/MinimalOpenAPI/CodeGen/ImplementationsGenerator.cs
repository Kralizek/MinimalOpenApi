using System.Text;

using Microsoft.CodeAnalysis;

using MinimalOpenAPI.Generator.Diagnostics;

namespace MinimalOpenAPI.Generator.CodeGen;

/// <summary>A generated handler or endpoint configuration base class declared in a referenced assembly.</summary>
internal sealed record ReferencedBase(string BaseTypeName, bool IsConfiguration, string DeclaringAssemblyName);

/// <summary>A class in this compilation deriving from a <see cref="ReferencedBase"/>.</summary>
internal sealed record ReferencedBaseImplementation(string ClassName, ReferencedBase Base);

/// <summary>
/// Registers the classes of this project that implement handlers or endpoint configurations of
/// documents declared in referenced assemblies, and reports MOA001–MOA003 across assemblies.
/// <para>
/// The handler and configuration bases of a document need not be implemented where the document is
/// declared: any project referencing that assembly may derive from them. The generator emits a public
/// entry point whose <c>Register()</c> first registers the referenced documents and then replaces their
/// default (throwing) handler registrations with this project's implementations, plus an assembly
/// attribute listing the implemented bases so that referencing projects can call the entry point and
/// check for missing or duplicate implementations.
/// </para>
/// </summary>
internal static class ImplementationsGenerator
{
    /// <summary>Hint name of the generated source.</summary>
    public const string HintName = "MinimalOpenApi/MinimalOpenApi.Implementations.g.cs";

    /// <summary>Name of the generated entry-point class, declared in the project's root namespace.</summary>
    public const string EntryPointClassName = "MinimalOpenApiImplementations";

    public static void Execute(
        SourceProductionContext spc,
        IReadOnlyList<ReferencedBaseImplementation> implementations,
        ReferencedMetadata referenced,
        string rootNamespace,
        bool isApplication)
    {
        var handlers = Check(
            spc,
            implementations.Where(i => !i.Base.IsConfiguration).ToList(),
            referenced.ImplementedHandlersElsewhere,
            declaredUnimplemented: referenced.UnimplementedHandlers,
            declaredImplemented: null,
            referenced.DocumentAssemblies,
            DiagnosticDescriptors.DuplicateHandlerImplementation,
            isApplication);

        var configurations = Check(
            spc,
            implementations.Where(i => i.Base.IsConfiguration).ToList(),
            referenced.ImplementedConfigurationsElsewhere,
            declaredUnimplemented: null,
            declaredImplemented: referenced.ImplementedConfigurations,
            referenced.DocumentAssemblies,
            DiagnosticDescriptors.DuplicateEndpointConfigurationImplementation,
            isApplication);

        if (handlers.Count == 0 && configurations.Count == 0)
            return;

        spc.AddSource(HintName, Generate(handlers, configurations, referenced, rootNamespace));
    }

    /// <summary>
    /// Reports duplicate (and, for handlers, missing) implementations and returns this project's
    /// implementation per base, ordered by base name.
    /// </summary>
    private static List<(string BaseTypeName, string ClassName)> Check(
        SourceProductionContext spc,
        List<ReferencedBaseImplementation> own,
        IReadOnlyList<ReferencedImplementation> elsewhere,
        IReadOnlyList<ReferencedType>? declaredUnimplemented,
        IReadOnlyList<ReferencedType>? declaredImplemented,
        IReadOnlyList<string> documentAssemblies,
        DiagnosticDescriptor duplicateDescriptor,
        bool isApplication)
    {
        var ownByBase = own
            .GroupBy(i => i.Base.BaseTypeName, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.OrderBy(i => i.ClassName, StringComparer.Ordinal).ToList(), StringComparer.Ordinal);
        var elsewhereByBase = elsewhere
            .GroupBy(i => i.BaseTypeName, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Select(i => i.ImplementingAssemblyName).ToList(), StringComparer.Ordinal);
        var unimplemented = (declaredUnimplemented ?? [])
            .GroupBy(t => t.TypeName, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().AssemblyName, StringComparer.Ordinal);
        var implementedWhereDeclared = (declaredImplemented ?? [])
            .GroupBy(t => t.TypeName, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().AssemblyName, StringComparer.Ordinal);

        var bases = new SortedSet<string>(ownByBase.Keys, StringComparer.Ordinal);
        bases.UnionWith(elsewhereByBase.Keys);
        bases.UnionWith(unimplemented.Keys);
        bases.UnionWith(implementedWhereDeclared.Keys);

        var result = new List<(string BaseTypeName, string ClassName)>();
        foreach (var baseName in bases)
        {
            var sources = new List<string>();
            var ownClasses = ownByBase.TryGetValue(baseName, out var o) ? o : [];
            sources.AddRange(ownClasses.Select(c => c.ClassName));
            if (elsewhereByBase.TryGetValue(baseName, out var assemblies))
                sources.AddRange(assemblies.Select(a => $"assembly '{a}'"));

            // The declaring assembly implements the base itself: for configurations it says so; for
            // handlers it lists only the ones it does not implement.
            if (implementedWhereDeclared.TryGetValue(baseName, out var declaringConfiguration))
            {
                sources.Add($"assembly '{declaringConfiguration}'");
            }
            else if (declaredUnimplemented is not null && ownClasses.Count > 0 && !unimplemented.ContainsKey(baseName))
            {
                var declaring = ownClasses[0].Base.DeclaringAssemblyName;
                if (documentAssemblies.Contains(declaring, StringComparer.Ordinal))
                    sources.Add($"assembly '{declaring}'");
            }

            if (sources.Count > 1 && (ownClasses.Count > 0 || isApplication))
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    duplicateDescriptor,
                    Location.None,
                    baseName,
                    string.Join(", ", sources)));
            }
            else if (sources.Count == 0 && isApplication && unimplemented.TryGetValue(baseName, out var declaringAssembly))
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.MissingHandlerImplementation,
                    Location.None,
                    baseName,
                    $" in referenced assembly '{declaringAssembly}' or in any project referencing it"));
            }

            if (ownClasses.Count > 0)
                result.Add((baseName, ownClasses[0].ClassName));
        }

        return result;
    }

    private static string Generate(
        List<(string BaseTypeName, string ClassName)> handlers,
        List<(string BaseTypeName, string ClassName)> configurations,
        ReferencedMetadata referenced,
        string rootNamespace)
    {
        const string ext = "global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions";
        const string di = "global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions";

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.Append($"[assembly: global::MinimalOpenAPI.MinimalOpenApiImplementations(typeof(global::{rootNamespace}.{EntryPointClassName})");
        AppendTypes(sb, "Handlers", handlers);
        AppendTypes(sb, "Configurations", configurations);
        sb.AppendLine(")]");
        sb.AppendLine();
        sb.AppendLine($"namespace {rootNamespace};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Registers this project's implementations of handlers and endpoint configurations generated for");
        sb.AppendLine("/// OpenAPI documents declared in referenced assemblies.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
        TypeMapper.AppendGeneratedAttributes(sb);
        sb.AppendLine($"public static class {EntryPointClassName}");
        sb.AppendLine("{");
        sb.AppendLine("    private static int _registered;");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Registers the referenced documents, then replaces their default handler registrations with this");
        sb.AppendLine("    /// project's implementations. Calling it more than once has no further effect.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static void Register()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (global::System.Threading.Interlocked.Exchange(ref _registered, 1) != 0)");
        sb.AppendLine("            return;");
        sb.AppendLine();
        foreach (var document in referenced.DocumentEntryPoints)
            sb.AppendLine($"        {document}.Register();");
        sb.AppendLine("        global::MinimalOpenAPI.ServiceCollectionExtensions.RegisterGeneratedServices(static services =>");
        sb.AppendLine("        {");
        foreach (var (baseName, className) in handlers)
        {
            sb.AppendLine($"            {ext}.RemoveAll<global::{baseName}>(services);");
            sb.AppendLine($"            {di}.AddScoped<global::{baseName}, global::{className}>(services);");
        }
        foreach (var (baseName, className) in configurations)
        {
            sb.AppendLine($"            {ext}.RemoveAll<global::{baseName}>(services);");
            sb.AppendLine($"            {di}.AddSingleton<global::{baseName}, global::{className}>(services);");
        }
        sb.AppendLine("        });");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("/// <summary>Registers this project's implementations when this assembly is loaded.</summary>");
        TypeMapper.AppendGeneratedAttributes(sb);
        sb.AppendLine("internal static class MinimalOpenApiImplementationsModuleInitializer");
        sb.AppendLine("{");
        sb.AppendLine("    [global::System.Runtime.CompilerServices.ModuleInitializer]");
        sb.AppendLine($"    internal static void Register() => global::{rootNamespace}.{EntryPointClassName}.Register();");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void AppendTypes(StringBuilder sb, string property, List<(string BaseTypeName, string ClassName)> types)
    {
        if (types.Count == 0)
            return;
        sb.Append($", {property} = new global::System.Type[] {{ ");
        sb.Append(string.Join(", ", types.Select(t => $"typeof(global::{t.BaseTypeName})")));
        sb.Append(" }");
    }
}