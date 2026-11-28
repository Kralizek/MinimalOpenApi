#if NET10_0_OR_GREATER
using System.ComponentModel;

namespace MinimalOpenAPI;

/// <summary>
/// Marks a generated handler base class, so projects that reference the declaring assembly can
/// discover and register classes deriving from it.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class MinimalOpenApiHandlerBaseAttribute : Attribute;

/// <summary>
/// Marks a generated endpoint configuration base class, so projects that reference the declaring
/// assembly can discover and register classes deriving from it.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class MinimalOpenApiEndpointConfigurationBaseAttribute : Attribute;
#endif