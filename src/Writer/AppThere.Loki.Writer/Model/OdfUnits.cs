// LAYER:   AppThere.Loki.Writer — Document Model
// KIND:    Implementation
// PURPOSE: Converts ODF measurement strings (e.g. "2.54cm", "12pt", "1.5em")
//          into points (1pt = 1/72 inch). Returns 0f on unrecognised unit or
//          parse failure — never throws. Does NOT handle layout or cascade.
// DEPENDS: (none)
// USED BY: OdfImporter (property parsing), StyleResolver
// PHASE:   3
// ADR:     ADR-009

namespace AppThere.Loki.Writer.Model;

public static class OdfUnits
{
    private const float CmToPoints  = 28.3465f;
    private const float MmToPoints  = 2.83465f;
    private const float InToPoints  = 72f;
    private const float PcToPoints  = 12f;

    /// <summary>
    /// Converts an ODF measurement string to points.
    /// Supported units: pt, cm, mm, in, pc, em, %.
    /// Returns 0f on unrecognised unit or parse failure.
    /// </summary>
    public static float ToPoints(string value, float inheritedFontSizePts)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0f;

        var s = value.AsSpan().Trim();

        if (s.EndsWith("pt",  StringComparison.OrdinalIgnoreCase))
            return ParseNumber(s[..^2]) ?? 0f;

        if (s.EndsWith("cm",  StringComparison.OrdinalIgnoreCase))
            return (ParseNumber(s[..^2]) ?? 0f) * CmToPoints;

        if (s.EndsWith("mm",  StringComparison.OrdinalIgnoreCase))
            return (ParseNumber(s[..^2]) ?? 0f) * MmToPoints;

        if (s.EndsWith("in",  StringComparison.OrdinalIgnoreCase))
            return (ParseNumber(s[..^2]) ?? 0f) * InToPoints;

        if (s.EndsWith("pc",  StringComparison.OrdinalIgnoreCase))
            return (ParseNumber(s[..^2]) ?? 0f) * PcToPoints;

        if (s.EndsWith("em",  StringComparison.OrdinalIgnoreCase))
            return (ParseNumber(s[..^2]) ?? 0f) * inheritedFontSizePts;

        if (s.EndsWith("%",   StringComparison.OrdinalIgnoreCase))
            return (ParseNumber(s[..^1]) ?? 0f) / 100f * inheritedFontSizePts;

        return 0f;
    }

    private static float? ParseNumber(ReadOnlySpan<char> text)
    {
        var trimmed = text.Trim();
        if (float.TryParse(trimmed, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var result))
            return result;
        return null;
    }
}
