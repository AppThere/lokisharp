// LAYER:   AppThere.Loki.Writer — Document Model
// KIND:    Records (computed style values)
// PURPOSE: Fully-resolved style structs stored on each model node.
//          Computed by StyleResolver at import time — the layout engine
//          reads these values only, never the StyleRegistry.
//          All measurements are in points (1pt = 1/72 inch).
// DEPENDS: FontDescriptor, LokiColor, Thickness
// USED BY: ParagraphNode, RunNode, LayoutEngine, PaintScene construction
// PHASE:   3
// ADR:     ADR-007

using AppThere.Loki.Kernel.Color;
using AppThere.Loki.Kernel.Fonts;
using AppThere.Loki.Kernel.Geometry;

namespace AppThere.Loki.Writer.Model.Styles;

public sealed record ParagraphStyle(
    FontDescriptor  Font,
    float           FontSizePts,
    LokiColor       Color,
    TextAlignment   Alignment,
    Thickness       MarginPts,          // before/after/start/end
    Thickness       PaddingPts,
    float           LineHeightPts,      // computed: FontSizePts × leading factor
    float           FirstLineIndentPts,
    float           HangingIndentPts,
    string?         ListStyleId,        // null = not a list item
    int             ListLevel,          // 0-based; 0 when not a list item
    float           SpaceBeforePts,     // paragraph spacing before
    float           SpaceAfterPts)      // paragraph spacing after
{
    public static ParagraphStyle Default => new(
        Font:               FontDescriptor.Default,
        FontSizePts:        12f,
        Color:              LokiColor.Black,
        Alignment:          TextAlignment.Left,
        MarginPts:          Thickness.Zero,
        PaddingPts:         Thickness.Zero,
        LineHeightPts:      14.4f,   // 12pt × 1.2 leading
        FirstLineIndentPts: 0f,
        HangingIndentPts:   0f,
        ListStyleId:        null,
        ListLevel:          0,
        SpaceBeforePts:     0f,
        SpaceAfterPts:      0f);
}

public sealed record CharacterStyle(
    FontDescriptor  Font,
    float           FontSizePts,
    LokiColor       Color,
    LokiColor?      BackgroundColor,
    bool            Bold,
    bool            Italic,
    bool            Underline,
    bool            Strikethrough,
    TextBaseline    Baseline)
{
    public static CharacterStyle Default => new(
        Font:            FontDescriptor.Default,
        FontSizePts:     12f,
        Color:           LokiColor.Black,
        BackgroundColor: null,
        Bold:            false,
        Italic:          false,
        Underline:       false,
        Strikethrough:   false,
        Baseline:        TextBaseline.Normal);
}

public enum TextAlignment  { Left, Right, Centre, Justify }
public enum TextBaseline   { Normal, Superscript, Subscript }
