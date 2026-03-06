// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Record
// PURPOSE: Immutable fill or stroke style for geometry nodes.
//          A null PaintStyle means "do not draw this pass" (e.g. no stroke).
//          Does not model text paint — see TextPaint.
// DEPENDS: LokiColor, Thickness
// USED BY: RectNode, RoundRectNode, PathNode, ILokiPainter
// PHASE:   1

using AppThere.Loki.Kernel.Color;
using AppThere.Loki.Kernel.Geometry;

namespace AppThere.Loki.Skia.Scene;

public sealed record PaintStyle(
    LokiColor    Color,
    float        Opacity    = 1f,
    float        StrokeWidth = 0f,  // 0 = fill; > 0 = stroke width in logical points
    LineCap      Cap         = LineCap.Butt,
    LineJoin     Join        = LineJoin.Miter,
    float[]?     DashPattern  = null)
{
    public static readonly PaintStyle Black = new(LokiColor.Black);
    public static readonly PaintStyle White = new(LokiColor.White);
    public static PaintStyle Fill(LokiColor color, float opacity = 1f) => new(color, opacity);
    public static PaintStyle Stroke(LokiColor color, float width, float opacity = 1f) =>
        new(color, opacity, width);
}

public sealed record LinePaint(
    LokiColor  Color,
    float      Width      = 1f,
    float      Opacity    = 1f,
    LineCap    Cap        = LineCap.Butt,
    float[]?   DashPattern = null);

public sealed record TextPaint(
    LokiColor Color,
    float     Opacity          = 1f,
    LokiColor? BackgroundColor = null,  // highlight / background fill behind text
    float     LetterSpacing    = 0f);   // additional tracking in logical points

public enum LineCap  { Butt, Round, Square }
public enum LineJoin { Miter, Round, Bevel }
public enum ImageFit { Fill, Contain, Cover, None }
