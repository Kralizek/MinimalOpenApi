#if NET10_0_OR_GREATER
using System.ComponentModel;

namespace MinimalOpenAPI;

/// <summary>
/// Identifies the contract types (DTOs) generated in this assembly for an OpenAPI document.
/// <para>
/// The MinimalOpenAPI source generator applies this attribute once per document whose
/// <c>&lt;OpenApi&gt;</c> item generates <c>Contracts</c>. A project that generates only
/// <c>Endpoints</c> for the same document reads it at build time to locate the contract types and
/// to verify, through <see cref="DocumentHash"/>, that both were generated from the same document.
/// </para>
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed class MinimalOpenApiContractsAttribute : Attribute
{
    /// <summary>Creates the attribute.</summary>
    /// <param name="specName">The document's spec name (the <c>Namespace</c> item metadata or the name derived from the file).</param>
    /// <param name="contractsNamespace">The namespace containing the generated contract types.</param>
    /// <param name="documentHash">A hash of the document content and of the settings that shape the contract types.</param>
    public MinimalOpenApiContractsAttribute(string specName, string contractsNamespace, string documentHash)
    {
        SpecName = specName;
        ContractsNamespace = contractsNamespace;
        DocumentHash = documentHash;
    }

    /// <summary>The document's spec name.</summary>
    public string SpecName { get; }

    /// <summary>The namespace containing the generated contract types.</summary>
    public string ContractsNamespace { get; }

    /// <summary>A hash of the document content and of the settings that shape the contract types.</summary>
    public string DocumentHash { get; }
}
#endif