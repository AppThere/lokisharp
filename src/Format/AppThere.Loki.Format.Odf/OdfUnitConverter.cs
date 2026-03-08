// LAYER:   AppThere.Loki.Format.Odf — ODF Import
// KIND:    Implementation
// PURPOSE: Static helper that converts ODF measurement strings (e.g. "8.5in",
//          "21cm", "595pt", "2.83mm") into logical points (1pt = 1/72 inch).
//          Logs and returns 0 on parse failure.
//          Supported units: in, cm, mm, pt, pc (pica = 12pt).
// DEPENDS: —
// USED BY: OdfDocumentParser (page layout, margin, font-size, spacing)
// PHASE:   3
// ADR:     ADR-009

namespace AppThere.Loki.Format.Odf;

internal static class OdfUnitConverter
{
    // ── Public entry point ────────────────────────────────────────────────────

    /// <summary>
    /// Convert an ODF measurement string to logical points.
    /// Returns 0 on failure.
    /// </summary>
    public static float ToPoints(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0f;

        value = value.Trim();

        // Split into numeric part and unit suffix
        var unitStart = 0;
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsLetter(c) || c == '%')
            {
                unitStart = i;
                break;
            }
        }

        if (unitStart == 0 && value.Length > 0 && !char.IsLetter(value[0]))
        {
            // No unit — treat as points
            return float.TryParse(value,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var v0) ? v0 : 0f;
        }

        var numStr  = value[..unitStart].Trim();
        var unitStr = value[unitStart..].Trim().ToLowerInvariant();

        if (!float.TryParse(numStr,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out var num))
            return 0f;

        return unitStr switch
        {
            "pt"  => num,
            "in"  => num * 72f,
            "cm"  => num * 28.3465f,
            "mm"  => num * 2.83465f,
            "pc"  => num * 12f,
            "px"  => num * 0.75f,  // 96dpi: 1px = 0.75pt
            _     => 0f,
        };
    }

    /// <summary>
    /// Parse a font-size string which may be "12pt", "14pt", or just "12".
    /// Returns defaultPts on failure.
    /// </summary>
    public static float FontSizePts(string? value, float defaultPts = 12f)
    {
        var result = ToPoints(value);
        return result > 0f ? result : defaultPts;
    }
}
