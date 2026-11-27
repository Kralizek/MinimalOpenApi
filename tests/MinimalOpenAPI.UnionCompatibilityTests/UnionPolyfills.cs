#if NET10_0
namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
internal sealed class UnionAttribute : Attribute;

internal interface IUnion
{
    object? Value { get; }
}
#endif