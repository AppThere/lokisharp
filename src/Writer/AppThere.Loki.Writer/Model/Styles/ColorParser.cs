// LAYER:   AppThere.Loki.Writer — Document Model
// KIND:    Implementation
// PURPOSE: Parses ODF hex colour strings ("RRGGBB", 6 digits, no #) into
//          LokiColor values. Returns null on null input, empty string, or any
//          parse failure. Does NOT handle ARGB or short-form hex.
// DEPENDS: LokiColor
// USED BY: StyleResolver
// PHASE:   3
// ADR:     ADR-007

using AppThere.Loki.Kernel.Color;

namespace AppThere.Loki.Writer.Model.Styles;

internal static class ColorParser
{
    /// <summary>
    /// Parses a 6-character hex RGB string (no '#') into a LokiColor.
    /// Returns null on null input, empty string, wrong length, or invalid hex.
    /// </summary>
    public static LokiColor? ParseHex(string? hex)
    {
        if (string.IsNullOrEmpty(hex))
            return null;

        if (hex.Length != 6)
            return null;

        if (!TryParseByte(hex, 0, out var r) ||
            !TryParseByte(hex, 2, out var g) ||
            !TryParseByte(hex, 4, out var b))
            return null;

        return LokiColor.FromArgb32(255, r, g, b);
    }

    private static bool TryParseByte(string hex, int start, out byte value)
    {
        var span = hex.AsSpan(start, 2);
        if (byte.TryParse(span, System.Globalization.NumberStyles.HexNumber,
                null, out value))
            return true;
        value = 0;
        return false;
    }
}
