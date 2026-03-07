// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Implementation
// PURPOSE: Wraps SKTypeface to implement the platform-neutral ILokiTypeface.
//          Holds the underlying Skia typeface handle. Does NOT expose SKTypeface
//          to any caller above this layer. Disposal is owner-controlled via ownsTypeface.
// DEPENDS: ILokiTypeface, SKTypeface, FontWeight, FontSlant
// USED BY: SkiaFontManager
// PHASE:   1
// ADR:     ADR-001

using SkiaSharp;
using AppThere.Loki.Kernel.Fonts;

namespace AppThere.Loki.Skia.Fonts;

internal sealed class SkiaTypeface : ILokiTypeface
{
    private bool _disposed;
    private readonly bool _ownsTypeface;

    internal SKTypeface Inner { get; }

    public string     FamilyName { get; }
    public FontWeight Weight     { get; }
    public FontSlant  Slant      { get; }
    public bool       IsBundled  { get; }
    public bool       IsVariable { get; }

    internal SkiaTypeface(
        SKTypeface  typeface,
        bool        isBundled,
        bool        ownsTypeface,
        bool        isVariable,
        FontWeight? reportedWeight = null)
    {
        Inner         = typeface ?? throw new ArgumentNullException(nameof(typeface));
        _ownsTypeface = ownsTypeface;
        FamilyName    = typeface.FamilyName;
        Weight        = reportedWeight ?? MapWeight(typeface.FontStyle.Weight);
        Slant         = typeface.FontStyle.Slant switch
        {
            SKFontStyleSlant.Italic  => FontSlant.Italic,
            SKFontStyleSlant.Oblique => FontSlant.Oblique,
            _                        => FontSlant.Upright,
        };
        IsBundled  = isBundled;
        IsVariable = isVariable;
    }

    public bool ContainsGlyph(int codepoint)
    {
        if (_disposed) return false;
        return Inner.ContainsGlyph(codepoint);
    }

    public void Dispose()
    {
        if (_disposed || !_ownsTypeface) return;
        _disposed = true;
        Inner.Dispose();
    }

    private static FontWeight MapWeight(int w) => w switch
    {
        <= 150 => FontWeight.Thin,
        <= 250 => FontWeight.ExtraLight,
        <= 350 => FontWeight.Light,
        <= 450 => FontWeight.Regular,
        <= 550 => FontWeight.Medium,
        <= 650 => FontWeight.SemiBold,
        <= 750 => FontWeight.Bold,
        <= 850 => FontWeight.ExtraBold,
        _      => FontWeight.Black,
    };
}
