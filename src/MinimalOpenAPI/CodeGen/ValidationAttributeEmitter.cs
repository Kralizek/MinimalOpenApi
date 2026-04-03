using MinimalOpenAPI.Abstractions.Models;

namespace MinimalOpenAPI.Generator.CodeGen;

/// <summary>
/// Converts OpenAPI constraint keywords (<c>minLength</c>, <c>maxLength</c>,
/// <c>pattern</c>, <c>minimum</c>, <c>maximum</c>, <c>minItems</c>, <c>maxItems</c>)
/// to <c>System.ComponentModel.DataAnnotations</c> attribute strings for use in
/// generated DTO and <c>Parameters</c> record properties.
/// </summary>
/// <remarks>
/// ASP.NET Core Minimal APIs do not run <c>DataAnnotations</c> validation
/// automatically. The generated attributes provide OpenAPI endpoint metadata
/// and IDE-level hints; runtime enforcement requires the consumer to enable
/// filter-based validation (e.g. <c>app.UseDataAnnotationsValidation()</c>).
/// </remarks>
internal static class ValidationAttributeEmitter
{
    /// <summary>
    /// Returns attribute strings for all applicable validation constraints
    /// on <paramref name="schema"/>.
    /// </summary>
    /// <param name="schema">The OpenAPI schema whose constraints are to be emitted.</param>
    /// <param name="indent">Optional indentation prefix prepended to each attribute string.</param>
    public static IEnumerable<string> GetAttributes(OpenApiSchema schema, string indent = "")
    {
        var type = schema.Type?.ToLowerInvariant();

        // ── String constraints ────────────────────────────────────────────
        if (type == "string")
        {
            if (schema.MinLength.HasValue && schema.MaxLength.HasValue)
            {
                yield return $"{indent}[global::System.ComponentModel.DataAnnotations.StringLength({schema.MaxLength.Value}, MinimumLength = {schema.MinLength.Value})]";
            }
            else if (schema.MinLength.HasValue)
            {
                yield return $"{indent}[global::System.ComponentModel.DataAnnotations.MinLength({schema.MinLength.Value})]";
            }
            else if (schema.MaxLength.HasValue)
            {
                yield return $"{indent}[global::System.ComponentModel.DataAnnotations.MaxLength({schema.MaxLength.Value})]";
            }

            if (schema.Pattern is not null)
            {
                yield return $"{indent}[global::System.ComponentModel.DataAnnotations.RegularExpression({EscapeStringLiteral(schema.Pattern)})]";
            }
        }

        // ── Numeric constraints ───────────────────────────────────────────
        if (type is "integer" or "number")
        {
            if (schema.Minimum.HasValue || schema.Maximum.HasValue)
            {
                if (type == "integer")
                {
                    var min = schema.Minimum.HasValue
                        ? ((long)schema.Minimum.Value).ToString(System.Globalization.CultureInfo.InvariantCulture)
                        : "int.MinValue";
                    var max = schema.Maximum.HasValue
                        ? ((long)schema.Maximum.Value).ToString(System.Globalization.CultureInfo.InvariantCulture)
                        : "int.MaxValue";
                    yield return $"{indent}[global::System.ComponentModel.DataAnnotations.Range({min}, {max})]";
                }
                else
                {
                    var min = schema.Minimum.HasValue ? FormatDouble(schema.Minimum.Value) : "double.MinValue";
                    var max = schema.Maximum.HasValue ? FormatDouble(schema.Maximum.Value) : "double.MaxValue";
                    yield return $"{indent}[global::System.ComponentModel.DataAnnotations.Range({min}, {max})]";
                }
            }
        }

        // ── Array constraints ─────────────────────────────────────────────
        if (type == "array")
        {
            if (schema.MinItems.HasValue)
                yield return $"{indent}[global::System.ComponentModel.DataAnnotations.MinLength({schema.MinItems.Value})]";
            if (schema.MaxItems.HasValue)
                yield return $"{indent}[global::System.ComponentModel.DataAnnotations.MaxLength({schema.MaxItems.Value})]";
        }
    }

    private static string FormatDouble(double value)
    {
        if (double.IsInfinity(value) || double.IsNaN(value))
            return "0.0";
        if (value == Math.Floor(value))
            return ((long)value).ToString(System.Globalization.CultureInfo.InvariantCulture) + ".0";
        return value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string EscapeStringLiteral(string pattern)
    {
        // Use a verbatim string literal when the pattern contains no double-quote
        // characters, to preserve back-slashes (common in regex patterns).
        if (!pattern.Contains('"'))
            return $"@\"{pattern}\"";

        // Fall back to a regular string literal with explicit escaping.
        return "\"" + pattern.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}