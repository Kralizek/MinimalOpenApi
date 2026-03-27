namespace MinimalOpenAPI.Generator.Parser;

/// <summary>
/// A simple YAML tree node representing a scalar, mapping, or sequence.
/// </summary>
internal sealed class YamlNode
{
    public string? Scalar { get; private set; }
    public Dictionary<string, YamlNode> Mapping { get; } = new(StringComparer.Ordinal);
    public List<YamlNode> Sequence { get; } = new List<YamlNode>();

    public bool IsScalar => Scalar is not null;
    public bool IsMapping => Mapping.Count > 0;
    public bool IsSequence => Sequence.Count > 0;

    private YamlNode() { }

    public YamlNode(string scalar) { Scalar = scalar; }

    internal static YamlNode Empty() => new YamlNode();

    // ── Accessors ──────────────────────────────────────────────────────────

    public string? GetString(params string[] path)
    {
        var node = Navigate(path);
        return node?.Scalar;
    }

    public bool GetBool(params string[] path)
    {
        var val = GetString(path);
        return val is "true" or "True" or "TRUE";
    }

    public YamlNode? GetNode(params string[] path) => Navigate(path);

    private YamlNode? Navigate(string[] path)
    {
        YamlNode current = this;
        foreach (var key in path)
        {
            if (!current.Mapping.TryGetValue(key, out var next)) return null;
            current = next;
        }
        return current;
    }

    // ── Parser ─────────────────────────────────────────────────────────────

    public static YamlNode Parse(string yaml)
    {
        var lines = Preprocess(yaml);
        var root = new YamlNode();
        var pos = 0;
        ParseMapping(lines, ref pos, 0, root);
        return root;
    }

    // Remove comments and normalize CRLF
    private static List<(int Indent, string Content)> Preprocess(string yaml)
    {
        var result = new List<(int, string)>();
        foreach (var raw in yaml.Replace("\r\n", "\n").Split('\n'))
        {
            // Strip inline comments (be careful with quoted strings)
            var line = StripComment(raw);
            if (string.IsNullOrWhiteSpace(line)) continue;
            var indent = CountLeadingSpaces(line);
            result.Add((indent, line.TrimEnd()));
        }
        return result;
    }

    private static string StripComment(string line)
    {
        bool inSingle = false, inDouble = false;
        for (var i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\'' && !inDouble) inSingle = !inSingle;
            else if (c == '"' && !inSingle) inDouble = !inDouble;
            else if (c == '#' && !inSingle && !inDouble)
            {
                return line.Substring(0, i);
            }
        }
        return line;
    }

    private static int CountLeadingSpaces(string line)
    {
        var count = 0;
        foreach (var c in line)
        {
            if (c == ' ') count++;
            else break;
        }
        return count;
    }

    private static void ParseMapping(
        List<(int Indent, string Content)> lines,
        ref int pos,
        int expectedIndent,
        YamlNode node)
    {
        while (pos < lines.Count)
        {
            var (indent, content) = lines[pos];

            // Stop if we've de-dented past our level
            if (indent < expectedIndent) break;

            // Skip lines that are not at our level (shouldn't happen normally)
            if (indent > expectedIndent) { pos++; continue; }

            var text = content.TrimStart();

            // Sequence item at this level — stop (caller handles sequences)
            if (text.StartsWith("- ") || text == "-") break;

            // Key: value
            var colonIdx = FindMappingColon(text);
            if (colonIdx < 0) { pos++; continue; }

            var key = UnquoteScalar(text.Substring(0, colonIdx).Trim());
            var rest = text.Substring(colonIdx + 1).Trim();

            pos++;

            if (string.IsNullOrEmpty(rest))
            {
                // Value is on the next lines
                var child = new YamlNode();
                if (pos < lines.Count && lines[pos].Indent > expectedIndent)
                {
                    var childIndent = lines[pos].Indent;
                    var childText = lines[pos].Content.TrimStart();

                    if (childText.StartsWith("- ") || childText == "-")
                    {
                        ParseSequence(lines, ref pos, childIndent, child);
                    }
                    else
                    {
                        ParseMapping(lines, ref pos, childIndent, child);
                    }
                }
                node.Mapping[key] = child;
            }
            else if (rest.StartsWith("|") || rest.StartsWith(">"))
            {
                // Block scalar — skip until de-dent
                while (pos < lines.Count && lines[pos].Indent > expectedIndent) pos++;
                node.Mapping[key] = new YamlNode(string.Empty);
            }
            else
            {
                node.Mapping[key] = new YamlNode(UnquoteScalar(rest));
            }
        }
    }

    private static void ParseSequence(
        List<(int Indent, string Content)> lines,
        ref int pos,
        int expectedIndent,
        YamlNode node)
    {
        while (pos < lines.Count)
        {
            var (indent, content) = lines[pos];
            if (indent < expectedIndent) break;
            if (indent > expectedIndent) { pos++; continue; }

            var text = content.TrimStart();

            if (!text.StartsWith("- ") && text != "-") break;

            var itemText = text.Substring(2).Trim(); // skip "- "
            pos++;

            var itemNode = new YamlNode();

            if (string.IsNullOrEmpty(itemText))
            {
                // Multi-line item
                if (pos < lines.Count && lines[pos].Indent > expectedIndent)
                {
                    ParseMapping(lines, ref pos, lines[pos].Indent, itemNode);
                }
            }
            else
            {
                // Could be inline mapping key: value, or a scalar
                var colonIdx = FindMappingColon(itemText);
                if (colonIdx >= 0)
                {
                    // Inline first key-value, then more at increased indent
                    var key = itemText.Substring(0, colonIdx).Trim();
                    var val = itemText.Substring(colonIdx + 1).Trim();
                    if (string.IsNullOrEmpty(val))
                    {
                        // Value on next lines
                        if (pos < lines.Count && lines[pos].Indent > expectedIndent)
                        {
                            var childIndent = lines[pos].Indent;
                            var childText = lines[pos].Content.TrimStart();
                            if (childText.StartsWith("- ") || childText == "-")
                                ParseSequence(lines, ref pos, childIndent, itemNode);
                            else
                                ParseMapping(lines, ref pos, childIndent, itemNode);
                        }
                    }
                    else
                    {
                        itemNode.Mapping[key] = new YamlNode(UnquoteScalar(val));
                    }
                    // More keys at indent + 2
                    if (pos < lines.Count && lines[pos].Indent > expectedIndent)
                    {
                        ParseMapping(lines, ref pos, lines[pos].Indent, itemNode);
                    }
                }
                else
                {
                    itemNode = new YamlNode(UnquoteScalar(itemText));
                    // Additional child keys
                    if (pos < lines.Count && lines[pos].Indent > expectedIndent)
                    {
                        ParseMapping(lines, ref pos, lines[pos].Indent, itemNode);
                    }
                }
            }

            node.Sequence.Add(itemNode);
        }
    }

    /// <summary>Find the colon that separates a YAML key from its value.</summary>
    private static int FindMappingColon(string text)
    {
        bool inSingle = false, inDouble = false;
        for (var i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '\'' && !inDouble) inSingle = !inSingle;
            else if (c == '"' && !inSingle) inDouble = !inDouble;
            else if (c == ':' && !inSingle && !inDouble)
            {
                // Must be followed by space or end-of-string
                if (i + 1 >= text.Length || text[i + 1] == ' ')
                    return i;
            }
        }
        return -1;
    }

    private static string UnquoteScalar(string value)
    {
        value = value.Trim();
        if (value.Length >= 2)
        {
            if ((value[0] == '\'' && value[value.Length - 1] == '\'') ||
                (value[0] == '"' && value[value.Length - 1] == '"'))
            {
                return value.Substring(1, value.Length - 2);
            }
        }
        return value;
    }
}
