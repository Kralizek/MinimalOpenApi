using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System.Text;

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
    private readonly string _namespaceKey;

    public TestAnalyzerConfigOptionsProvider(
        AdditionalText[] additionalTexts,
        string rootNamespace,
        string metadataKey,
        string namespaceKey)
    {
        _additionalTexts = additionalTexts;
        _rootNamespace = rootNamespace;
        _metadataKey = metadataKey;
        _namespaceKey = namespaceKey;
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
        var isOpenApi = _additionalTexts.Any(t =>
            t.Path == textFile.Path);

        return new TestAnalyzerConfigOptions(new Dictionary<string, string>
        {
            [_metadataKey] = isOpenApi ? "true" : "false"
        });
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
