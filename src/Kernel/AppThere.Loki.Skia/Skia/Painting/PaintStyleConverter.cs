// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Implementation
// PURPOSE: Static helpers that convert Loki paint types (LokiColor, PaintStyle,
//          LinePaint, TextPaint) to transient SKPaint/SKColor instances.
//          Every Create* method returns a new SKPaint; caller owns it and must
//          dispose it (use a 'using' statement). Never reuse returned instances.
//          Does NOT handle ImageFit or text layout — see LokiSkiaPainter.
// DEPENDS: LokiColor, PaintStyle, LinePaint, TextPaint, LineCap, LineJoin
// USED BY: LokiSkiaPainter
// PHASE:   1

using AppThere.Loki.Kernel.Color;
using AppThere.Loki.Skia.Scene;
using SkiaSharp;

namespace AppThere.Loki.Skia.Painting;

internal static class PaintStyleConverter
{
    internal static SKColor ToSkColor(LokiColor c)
        => new(c.R8, c.G8, c.B8, c.A8);

    /// <summary>Creates a fill SKPaint from a PaintStyle. Caller disposes.</summary>
    internal static SKPaint CreateFillPaint(PaintStyle style)
    {
        var paint = new SKPaint
        {
            IsAntialias = true,
            Style       = SKPaintStyle.Fill,
            Color       = ToSkColor(style.Color).WithAlpha((byte)(style.Opacity * style.Color.A * 255f)),
        };
        return paint;
    }

    /// <summary>Creates a stroke SKPaint from a PaintStyle. Caller disposes.</summary>
    internal static SKPaint CreateStrokePaint(PaintStyle style)
    {
        var paint = new SKPaint
        {
            IsAntialias = true,
            Style       = SKPaintStyle.Stroke,
            StrokeWidth = style.StrokeWidth,
            StrokeCap   = MapLineCap(style.Cap),
            StrokeJoin  = MapLineJoin(style.Join),
            Color       = ToSkColor(style.Color).WithAlpha((byte)(style.Opacity * style.Color.A * 255f)),
        };
        if (style.DashPattern is { Length: > 0 })
        {
            paint.PathEffect = SKPathEffect.CreateDash(style.DashPattern, 0f);
        }
        return paint;
    }

    /// <summary>Creates a stroke SKPaint from a LinePaint. Caller disposes.</summary>
    internal static SKPaint CreateLinePaint(LinePaint lp)
    {
        var paint = new SKPaint
        {
            IsAntialias = true,
            Style       = SKPaintStyle.Stroke,
            StrokeWidth = lp.Width,
            StrokeCap   = MapLineCap(lp.Cap),
            Color       = ToSkColor(lp.Color).WithAlpha((byte)(lp.Opacity * lp.Color.A * 255f)),
        };
        if (lp.DashPattern is { Length: > 0 })
        {
            paint.PathEffect = SKPathEffect.CreateDash(lp.DashPattern, 0f);
        }
        return paint;
    }

    /// <summary>Creates a fill SKPaint suitable for text rendering. Caller disposes.</summary>
    internal static SKPaint CreateTextPaint(TextPaint tp)
    {
        return new SKPaint
        {
            IsAntialias = true,
            Style       = SKPaintStyle.Fill,
            Color       = ToSkColor(tp.Color).WithAlpha((byte)(tp.Opacity * tp.Color.A * 255f)),
        };
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private static SKStrokeCap MapLineCap(LineCap cap) => cap switch
    {
        LineCap.Round  => SKStrokeCap.Round,
        LineCap.Square => SKStrokeCap.Square,
        _              => SKStrokeCap.Butt,
    };

    private static SKStrokeJoin MapLineJoin(LineJoin join) => join switch
    {
        LineJoin.Round => SKStrokeJoin.Round,
        LineJoin.Bevel => SKStrokeJoin.Bevel,
        _              => SKStrokeJoin.Miter,
    };
}
