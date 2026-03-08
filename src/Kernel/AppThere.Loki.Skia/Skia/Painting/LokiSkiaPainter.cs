// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Implementation
// PURPOSE: Implements ILokiPainter by delegating to SKCanvas. Does not own the
//          canvas — does not dispose it. All SKPaint instances are created
//          transiently per draw call and disposed immediately (using statement).
//          Conversion between Loki types and Skia types is delegated to
//          PaintStyleConverter. Group opacity is tracked via a stack.
// DEPENDS: ILokiPainter, SKCanvas, IImageStore, ILokiLogger, PaintStyleConverter,
//          LokiPath, GlyphRun, SkiaTypeface, PaintStyle, LinePaint, TextPaint,
//          ImageRef, ImageFit, LokiColor, RectF, PointF, StorageException
// USED BY: TileRenderer, lokiprint CLI, LokiSkiaPainterTests
// PHASE:   1
// ADR:     ADR-002, ADR-003, ADR-004

using AppThere.Loki.Kernel.Color;
using AppThere.Loki.Kernel.Errors;
using AppThere.Loki.Kernel.Geometry;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.Skia.Fonts;
using AppThere.Loki.Skia.Images;
using AppThere.Loki.Skia.Paths;
using AppThere.Loki.Skia.Scene;
using SkiaSharp;

namespace AppThere.Loki.Skia.Painting;

public sealed class LokiSkiaPainter : ILokiPainter
{
    private readonly SKCanvas    _canvas;
    private readonly IImageStore _imageStore;
    private readonly ILokiLogger _logger;
    private readonly Stack<(RectF Bounds, float Opacity)> _groupStack = new();

    public LokiSkiaPainter(SKCanvas canvas, IImageStore imageStore, ILokiLogger logger)
    {
        _canvas     = canvas     ?? throw new ArgumentNullException(nameof(canvas));
        _imageStore = imageStore ?? throw new ArgumentNullException(nameof(imageStore));
        _logger     = logger     ?? throw new ArgumentNullException(nameof(logger));
    }

    // ── State ─────────────────────────────────────────────────────────────────

    public void Save()    => _canvas.Save();
    public void Restore() => _canvas.Restore();

    public void ClipRect(RectF rect) =>
        _canvas.ClipRect(ToSkRect(rect), SKClipOperation.Intersect);

    public void SetTransform(float sx, float sy, float tx, float ty) =>
        _canvas.SetMatrix(SKMatrix.CreateScaleTranslation(sx, sy, tx, ty));

    public void Clear(LokiColor color) =>
        _canvas.Clear(PaintStyleConverter.ToSkColor(color));

    // ── Geometry ──────────────────────────────────────────────────────────────

    public void DrawRect(RectF bounds, PaintStyle fill, PaintStyle? stroke = null)
    {
        var skRect = ToSkRect(bounds);
        using (var paint = PaintStyleConverter.CreateFillPaint(fill))
            _canvas.DrawRect(skRect, paint);
        if (stroke is not null)
        {
            using var paint = PaintStyleConverter.CreateStrokePaint(stroke);
            _canvas.DrawRect(skRect, paint);
        }
    }

    public void DrawRoundRect(RectF bounds, float rx, float ry, PaintStyle fill, PaintStyle? stroke = null)
    {
        var skRect = ToSkRect(bounds);
        using (var paint = PaintStyleConverter.CreateFillPaint(fill))
            _canvas.DrawRoundRect(skRect, rx, ry, paint);
        if (stroke is not null)
        {
            using var paint = PaintStyleConverter.CreateStrokePaint(stroke);
            _canvas.DrawRoundRect(skRect, rx, ry, paint);
        }
    }

    public void DrawLine(PointF a, PointF b, LinePaint paint)
    {
        using var skPaint = PaintStyleConverter.CreateLinePaint(paint);
        _canvas.DrawLine(a.X, a.Y, b.X, b.Y, skPaint);
    }

    public void DrawPath(LokiPath path, PaintStyle fill, PaintStyle? stroke = null)
    {
        var skPath = path.ToSkiaPath();
        using (var paint = PaintStyleConverter.CreateFillPaint(fill))
            _canvas.DrawPath(skPath, paint);
        if (stroke is not null)
        {
            using var paint = PaintStyleConverter.CreateStrokePaint(stroke);
            _canvas.DrawPath(skPath, paint);
        }
    }

    // ── Images ────────────────────────────────────────────────────────────────

    public void DrawImage(RectF destBounds, ImageRef image, float opacity = 1f, ImageFit fit = ImageFit.Contain)
    {
        SKBitmap bitmap;
        try
        {
            bitmap = _imageStore.Decode(image);
        }
        catch (StorageException ex)
        {
            _logger.Warn("Image '{0}' not found — drawing placeholder. {1}",
                image.ContentHash.Length >= 8 ? image.ContentHash[..8] : image.ContentHash,
                ex.Message);
            using var placeholder = new SKPaint { Style = SKPaintStyle.Fill, Color = new SKColor(255, 0, 255) };
            _canvas.DrawRect(ToSkRect(destBounds), placeholder);
            return;
        }

        var (src, dst) = ComputeImageRects(bitmap, destBounds, fit);
        using var imgPaint = new SKPaint { IsAntialias = true };
        imgPaint.Color = SKColors.White.WithAlpha((byte)(opacity * 255f));

        _canvas.Save();
        _canvas.ClipRect(ToSkRect(destBounds));
        _canvas.DrawBitmap(bitmap, src, dst, imgPaint);
        _canvas.Restore();
    }

    // ── Text ──────────────────────────────────────────────────────────────────

    public void DrawGlyphRun(PointF origin, GlyphRun run, TextPaint paint)
    {
        if (run.GlyphIds.IsDefaultOrEmpty) return;

        var skTypeface = ((SkiaTypeface)run.Typeface).Inner;
        using var font = new SKFont(skTypeface, run.SizeInPoints) { Subpixel = true };

        if (paint.BackgroundColor.HasValue)
        {
            var bgRect = ComputeGlyphRunBounds(origin, run, font);
            using var bgPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = PaintStyleConverter.ToSkColor(paint.BackgroundColor.Value),
            };
            _canvas.DrawRect(bgRect, bgPaint);
        }

        var count     = run.GlyphIds.Length;
        var positions = BuildGlyphPositions(run, count);

        using var blobBuilder = new SKTextBlobBuilder();
        var buffer = blobBuilder.AllocatePositionedRun(font, count);
        for (var i = 0; i < count; i++) buffer.Glyphs[i] = run.GlyphIds[i];
        for (var i = 0; i < count; i++) buffer.Points[i] = positions[i];

        using var blob = blobBuilder.Build();
        if (blob is null) return;

        using var skPaint = PaintStyleConverter.CreateTextPaint(paint);
        _canvas.DrawTextBlob(blob, origin.X, origin.Y, skPaint);
    }

    // ── Groups / effects ──────────────────────────────────────────────────────

    public void BeginGroup(RectF bounds, float opacity = 1f, RectF? clip = null)
    {
        _groupStack.Push((bounds, opacity));
        using var layerPaint = new SKPaint
        {
            Color = new SKColor(255, 255, 255, (byte)(opacity * 255f)),
        };
        if (clip.HasValue)
            _canvas.SaveLayer(ToSkRect(clip.Value), layerPaint);
        else
            _canvas.SaveLayer(layerPaint);
    }

    public void EndGroup()
    {
        if (_groupStack.Count == 0)
            throw new InvalidOperationException("EndGroup called without a matching BeginGroup.");
        _groupStack.Pop();
        _canvas.Restore();
    }

    public void DrawShadow(RectF contentBounds, PointF offset, float blurRadius, LokiColor color)
    {
        using var blurFilter = SKImageFilter.CreateBlur(blurRadius, blurRadius);
        using var shadowPaint = new SKPaint
        {
            IsAntialias = true,
            Style       = SKPaintStyle.Fill,
            Color       = PaintStyleConverter.ToSkColor(color),
            ImageFilter = blurFilter,
        };
        _canvas.Save();
        _canvas.Translate(offset.X, offset.Y);
        _canvas.DrawRect(ToSkRect(contentBounds), shadowPaint);
        _canvas.Restore();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static SKRect ToSkRect(RectF r) =>
        new(r.Left, r.Top, r.Right, r.Bottom);

    private static SKPoint[] BuildGlyphPositions(GlyphRun run, int count)
    {
        var positions = new SKPoint[count];
        float x = 0f;
        for (var i = 0; i < count; i++)
        {
            float ox = run.OffsetX.HasValue ? run.OffsetX.Value[i] : 0f;
            float oy = run.OffsetY.HasValue ? run.OffsetY.Value[i] : 0f;
            positions[i] = new SKPoint(x + ox, oy);
            x += run.Advances[i];
        }
        return positions;
    }

    private static SKRect ComputeGlyphRunBounds(PointF origin, GlyphRun run, SKFont font)
    {
        var metrics      = font.Metrics;
        float totalWidth = run.TotalAdvance;
        return new SKRect(
            origin.X,
            origin.Y + metrics.Ascent,
            origin.X + totalWidth,
            origin.Y + metrics.Descent);
    }

    private static (SKRect Src, SKRect Dst) ComputeImageRects(SKBitmap bmp, RectF dest, ImageFit fit)
    {
        float imgW = bmp.Width;
        float imgH = bmp.Height;
        float dstW = dest.Width;
        float dstH = dest.Height;

        var fullSrc = new SKRect(0, 0, imgW, imgH);
        var fullDst = ToSkRect(dest);

        if (imgW <= 0 || imgH <= 0 || dstW <= 0 || dstH <= 0)
            return (fullSrc, fullDst);

        return fit switch
        {
            ImageFit.Fill    => (fullSrc, fullDst),
            ImageFit.Contain => ContainRects(imgW, imgH, dest),
            ImageFit.Cover   => CoverRects(imgW, imgH, dest),
            ImageFit.None    => NoneRects(imgW, imgH, dest),
            _                => (fullSrc, fullDst),
        };
    }

    private static (SKRect Src, SKRect Dst) ContainRects(float iw, float ih, RectF dest)
    {
        float scale = Math.Min(dest.Width / iw, dest.Height / ih);
        float fw    = iw * scale;
        float fh    = ih * scale;
        float ox    = dest.X + (dest.Width  - fw) / 2f;
        float oy    = dest.Y + (dest.Height - fh) / 2f;
        return (new SKRect(0, 0, iw, ih), new SKRect(ox, oy, ox + fw, oy + fh));
    }

    private static (SKRect Src, SKRect Dst) CoverRects(float iw, float ih, RectF dest)
    {
        float scale = Math.Max(dest.Width / iw, dest.Height / ih);
        float fw    = dest.Width  / scale;
        float fh    = dest.Height / scale;
        float sx    = (iw - fw) / 2f;
        float sy    = (ih - fh) / 2f;
        return (new SKRect(sx, sy, sx + fw, sy + fh), ToSkRect(dest));
    }

    private static (SKRect Src, SKRect Dst) NoneRects(float iw, float ih, RectF dest)
    {
        float cw = Math.Min(iw, dest.Width);
        float ch = Math.Min(ih, dest.Height);
        return (new SKRect(0, 0, cw, ch),
                new SKRect(dest.X, dest.Y, dest.X + cw, dest.Y + ch));
    }
}
