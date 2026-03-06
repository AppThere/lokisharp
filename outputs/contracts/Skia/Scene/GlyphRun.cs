// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Record
// PURPOSE: A single shaped text run: one typeface, one size, contiguous glyphs.
//          Produced by LokiTextShaper; consumed by GlyphRunNode and ILokiPainter.
//          GlyphIds and Advances are parallel arrays of the same length.
//          Offsets is optional per-glyph adjustment (e.g. kerning, mark attachment).
// DEPENDS: ILokiTypeface
// USED BY: GlyphRunNode, ILokiPainter.DrawGlyphRun, LokiTextShaper
// PHASE:   1

using System.Collections.Immutable;
using AppThere.Loki.Skia.Fonts;

namespace AppThere.Loki.Skia.Scene;

public sealed record GlyphRun(
    ILokiTypeface         Typeface,
    float                 SizeInPoints,
    ImmutableArray<ushort> GlyphIds,    // OpenType glyph IDs
    ImmutableArray<float>  Advances,    // x-advance per glyph in logical points
    ImmutableArray<float>? OffsetX,    // null = all zeros
    ImmutableArray<float>? OffsetY,    // null = all zeros
    bool                   IsRtl       = false)
{
    public float TotalAdvance =>
        Advances.IsDefault ? 0f : Advances.Sum();
}
