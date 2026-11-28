#if NET10_0_OR_GREATER
using System.ComponentModel;

namespace MinimalOpenAPI;

/// <summary>
/// Identifies the generated entry point that registers the handlers and endpoint configurations this
/// assembly implements for OpenAPI documents declared in referenced assemblies.
/// <para>
/// The MinimalOpenAPI source generator applies this attribute when a project contains classes that
/// derive from handler or endpoint configuration base classes generated in a referenced assembly.
/// Projects referencing this assembly read it at build time, call its entry point before the
/// application starts and use the listed base classes for the MOA001–MOA003 checks.
/// </para>
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public sealed class MinimalOpenApiImplementationsAttribute : Attribute
{
    /// <summary>Creates the attribute for the given generated entry-point type.</summary>
    /// <param name="entryPoint">
    /// The generated static class exposing a public static parameterless <c>Register()</c> method.
    /// </param>
    public MinimalOpenApiImplementationsAttribute(Type entryPoint) => EntryPoint = entryPoint;

    /// <summary>The generated static class exposing <c>Register()</c>.</summary>
    public Type EntryPoint { get; }

    /// <summary>The referenced handler base classes implemented in this assembly.</summary>
    public Type[] Handlers { get; set; } = [];

    /// <summary>The referenced endpoint configuration base classes implemented in this assembly.</summary>
    public Type[] Configurations { get; set; } = [];
}
#endif