// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Interface
// PURPOSE: Platform-neutral handle to a resolved typeface (font + style).
//          Wraps SKTypeface without exposing it to callers above this layer.
//          Callers use ILokiTypeface everywhere; only SkiaFontManager holds SKTypeface.
// DEPENDS: FontFamilyInfo
// USED BY: IFontManager, LokiTextShaper, GlyphRun
// PHASE:   1

using AppThere.Loki.Kernel.Fonts;

namespace AppThere.Loki.Skia.Fonts;

public interface ILokiTypeface : IDisposable
{
    string     FamilyName  { get; }
    FontWeight Weight      { get; }
    FontSlant  Slant       { get; }
    bool       IsBundled   { get; }
    bool       IsVariable  { get; }

    /// <summary>Returns true if this typeface contains a glyph for the given Unicode codepoint.</summary>
    bool ContainsGlyph(int codepoint);
}
