#if NET10_0_OR_GREATER
using System.ComponentModel;

namespace MinimalOpenAPI;

/// <summary>
/// Identifies the generated entry point of an OpenAPI document declared in this assembly.
/// <para>
/// The MinimalOpenAPI source generator applies this attribute once per <c>&lt;OpenApi&gt;</c> document.
/// When the generator runs in a project that references this assembly (for example a web host
/// referencing a contracts class library), it reads these attributes at build time and registers
/// every referenced document before the application starts, so <c>AddMinimalOpenApi()</c>,
/// <c>MapMinimalOpenApiEndpoints()</c> and <c>MapOpenApiSchemas()</c> include it without any
/// runtime assembly scanning.
/// </para>
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed class MinimalOpenApiDocumentAttribute : Attribute
{
    /// <summary>Creates the attribute for the given generated entry-point type.</summary>
    /// <param name="entryPoint">
    /// The generated static class exposing a public static parameterless <c>Register()</c> method.
    /// </param>
    public MinimalOpenApiDocumentAttribute(Type entryPoint) => EntryPoint = entryPoint;

    /// <summary>The generated static class exposing <c>Register()</c>.</summary>
    public Type EntryPoint { get; }

    /// <summary>
    /// The generated handler base classes of this document that have no implementation in this
    /// assembly. A class library leaves them to the application that references it, which reports
    /// MOA001 for each one at build time.
    /// </summary>
    public Type[] UnimplementedHandlers { get; set; } = [];

    /// <summary>
    /// The generated endpoint configuration base classes of this document that are implemented in
    /// this assembly, so referencing projects can report a second implementation (MOA003).
    /// </summary>
    public Type[] ImplementedConfigurations { get; set; } = [];
}
#endif