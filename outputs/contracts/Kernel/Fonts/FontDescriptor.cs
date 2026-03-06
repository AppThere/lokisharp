// LAYER:   AppThere.Loki.Kernel — Kernel
// KIND:    Record
// PURPOSE: Immutable specification of a requested font. This is the input type
//          to IFontManager.TryGetTypeface. It describes what is wanted, not what
//          was resolved. The font manager resolves to an actual typeface.
// DEPENDS: FontWeight, FontSlant, FontStretch
// USED BY: IFontManager, GlyphRun, TextPaint
// PHASE:   1
// ADR:     ADR-001

namespace AppThere.Loki.Kernel.Fonts;

public sealed record FontDescriptor(
    string      FamilyName,
    FontWeight  Weight        = FontWeight.Regular,
    FontSlant   Slant         = FontSlant.Upright,
    FontStretch Stretch       = FontStretch.Normal,
    float       SizeInPoints  = 12f,
    /// <summary>
    /// Optional explicit variable font axis overrides.
    /// When set, these values override the Weight/Slant/Stretch fields for
    /// the corresponding OpenType axes (wght, ital, slnt, wdth).
    /// Pass null to use the derived axis values. See ADR-001 §Variable Axis Mapping.
    /// </summary>
    IReadOnlyDictionary<string, float>? VariableAxes = null)
{
    public static readonly FontDescriptor Default =
        new("Inter", FontWeight.Regular, FontSlant.Upright, FontStretch.Normal, 12f);
}
