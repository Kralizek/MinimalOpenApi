using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace MinimalOpenAPI.Generator.Tests;

/// <summary>A test implementation of IAdditionalText.</summary>
internal sealed class TestAdditionalText : AdditionalText
{
    private readonly string _content;

    public TestAdditionalText(string path, string content)
    {
        Path = path;
        _content = content;
    }

    public override string Path { get; }

    public override SourceText? GetText(CancellationToken cancellationToken = default)
        => SourceText.From(_content, Encoding.UTF8);
}

/// <summary>A test implementation of AnalyzerConfigOptionsProvider.</summary>
internal sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
{
    private readonly AdditionalText[] _additionalTexts;
    private readonly string _rootNamespace;
    private readonly string _metadataKey;
    private readonly string _namespaceMetadataKey;
    private readonly string _schemaIdMetadataKey;
    private readonly string _publishAsMetadataKey;
    private readonly string _displayNameMetadataKey;
    private readonly string _displayVersionMetadataKey;
    private readonly string _readWriteSchemaHandlingMetadataKey;
    private readonly string _namespaceKey;
    private readonly string? _specNameOverride;
    private readonly IReadOnlyDictionary<string, string> _specNameOverridesByFilePath;
    private readonly string? _schemaIdOverride;
    private readonly string? _publishAs;
    private readonly string? _displayName;
    private readonly string? _displayVersion;
    private readonly string? _readWriteSchemaHandling;

    public TestAnalyzerConfigOptionsProvider(
        AdditionalText[] additionalTexts,
        string rootNamespace,
        string metadataKey,
        string namespaceMetadataKey,
        string schemaIdMetadataKey,
        string publishAsMetadataKey,
        string displayNameMetadataKey,
        string displayVersionMetadataKey,
        string readWriteSchemaHandlingMetadataKey,
        string namespaceKey,
        string? specNameOverride = null,
        IReadOnlyDictionary<string, string>? specNameOverridesByFilePath = null,
        string? schemaId = null,
        string? publishAs = null,
        string? displayName = null,
        string? displayVersion = null,
        string? readWriteSchemaHandling = null)
    {
        _additionalTexts = additionalTexts;
        _rootNamespace = rootNamespace;
        _metadataKey = metadataKey;
        _namespaceMetadataKey = namespaceMetadataKey;
        _schemaIdMetadataKey = schemaIdMetadataKey;
        _publishAsMetadataKey = publishAsMetadataKey;
        _displayNameMetadataKey = displayNameMetadataKey;
        _displayVersionMetadataKey = displayVersionMetadataKey;
        _readWriteSchemaHandlingMetadataKey = readWriteSchemaHandlingMetadataKey;
        _namespaceKey = namespaceKey;
        _specNameOverride = specNameOverride;
        _specNameOverridesByFilePath = specNameOverridesByFilePath is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(specNameOverridesByFilePath, StringComparer.OrdinalIgnoreCase);
        _schemaIdOverride = schemaId;
        _publishAs = publishAs;
        _displayName = displayName;
        _displayVersion = displayVersion;
        _readWriteSchemaHandling = readWriteSchemaHandling;
    }

    public override AnalyzerConfigOptions GlobalOptions
        => new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [_namespaceKey] = _rootNamespace
        });

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        => new TestAnalyzerConfigOptions(new Dictionary<string, string>());

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
    {
        var isOpenApi = _additionalTexts.Any(t => t.Path == textFile.Path);

        var options = new Dictionary<string, string>
        {
            [_metadataKey] = isOpenApi ? "true" : "false"
        };

        if (isOpenApi && _specNameOverridesByFilePath.TryGetValue(textFile.Path, out var namespaceOverride))
            options[_namespaceMetadataKey] = namespaceOverride;
        else if (isOpenApi && _specNameOverride is not null)
            options[_namespaceMetadataKey] = _specNameOverride;

        if (isOpenApi && _schemaIdOverride is not null)
            options[_schemaIdMetadataKey] = _schemaIdOverride;

        if (isOpenApi && _publishAs is not null)
            options[_publishAsMetadataKey] = _publishAs;

        if (isOpenApi && _displayName is not null)
            options[_displayNameMetadataKey] = _displayName;

        if (isOpenApi && _displayVersion is not null)
            options[_displayVersionMetadataKey] = _displayVersion;

        if (isOpenApi && _readWriteSchemaHandling is not null)
            options[_readWriteSchemaHandlingMetadataKey] = _readWriteSchemaHandling;

        return new TestAnalyzerConfigOptions(options);
    }
}

/// <summary>A test implementation of AnalyzerConfigOptions.</summary>
internal sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
{
    private readonly Dictionary<string, string> _options;

    public TestAnalyzerConfigOptions(Dictionary<string, string> options)
        => _options = options;

    public override bool TryGetValue(string key, out string value)
    {
        if (_options.TryGetValue(key, out var v))
        {
            value = v!;
            return true;
        }
        value = string.Empty;
        return false;
    }
}