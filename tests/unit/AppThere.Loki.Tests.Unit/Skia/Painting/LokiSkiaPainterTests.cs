// LAYER:   AppThere.Loki.Tests.Unit — Tests
// KIND:    Tests
// PURPOSE: Specification tests for LokiSkiaPainter. Verifies that each draw call
//          produces the expected pixel output on a 256×256 BitmapRenderSurface.
//          Pixel sampling is the authoritative assertion method — it is the only
//          reliable way to verify a painter's correctness without golden files.
//          Image tests use NSubstitute mocks for IImageStore.
// DEPENDS: LokiSkiaPainter, BitmapRenderSurface, IImageStore, SkiaTypeface,
//          PaintStyle, LinePaint, TextPaint, GlyphRun, LokiPath, ImageRef,
//          ImageFit, LokiColor, RectF, PointF, StorageException, NullLokiLogger
// USED BY: CI
// PHASE:   1

using System.Collections.Immutable;
using AppThere.Loki.Kernel.Color;
using AppThere.Loki.Kernel.Errors;
using AppThere.Loki.Kernel.Geometry;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.Skia.Fonts;
using AppThere.Loki.Skia.Images;
using AppThere.Loki.Skia.Painting;
using AppThere.Loki.Skia.Paths;
using AppThere.Loki.Skia.Scene;
using AppThere.Loki.Skia.Surfaces;
using AppThere.Loki.Tests.Unit.Skia;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SkiaSharp;

namespace AppThere.Loki.Tests.Unit.Skia.Painting;

public sealed class LokiSkiaPainterTests : IDisposable
{
    private readonly BitmapRenderSurface _surface;
    private readonly IImageStore         _imageStore;
    private readonly LokiSkiaPainter    _painter;

    static LokiSkiaPainterTests()
    {
        SkiaTestInitializer.EnsureSkiaSharpLoaded();
    }

    public LokiSkiaPainterTests()
    {
        _surface    = new BitmapRenderSurface(256, 256);
        _imageStore = Substitute.For<IImageStore>();
        _painter    = new LokiSkiaPainter(_surface.GetCanvas(), _imageStore, NullLokiLogger.Instance);
    }

    public void Dispose() => _surface.Dispose();

    // ── Helper ────────────────────────────────────────────────────────────────

    private LokiColor SamplePixel(BitmapRenderSurface surface, int x, int y)
    {
        var bmp   = surface.GetBitmap();
        var color = bmp.GetPixel(x, y);
        return LokiColor.FromArgb32(color.Alpha, color.Red, color.Green, color.Blue);
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Clear_Red_FillsEntireBitmap()
    {
        _painter.Clear(LokiColor.Red);

        var tl = SamplePixel(_surface, 0,   0);
        var br = SamplePixel(_surface, 255, 255);
        var cx = SamplePixel(_surface, 128, 128);

        tl.R8.Should().BeGreaterThan(200);
        tl.G8.Should().BeLessThan(50);
        br.R8.Should().BeGreaterThan(200);
        cx.R8.Should().BeGreaterThan(200);
    }

    [Fact]
    public void DrawRect_FilledBlue_CentrePixelIsBlue()
    {
        _painter.Clear(LokiColor.White);
        _painter.DrawRect(new RectF(50f, 50f, 156f, 156f), PaintStyle.Fill(LokiColor.Blue));

        var center = SamplePixel(_surface, 128, 128);

        center.B8.Should().BeGreaterThan(200);
        center.R8.Should().BeLessThan(50);
        center.G8.Should().BeLessThan(50);
    }

    [Fact]
    public void DrawRect_FilledBlue_PixelOutsideRectIsNotBlue()
    {
        _painter.Clear(LokiColor.White);
        _painter.DrawRect(new RectF(50f, 50f, 100f, 100f), PaintStyle.Fill(LokiColor.Blue));

        var outside = SamplePixel(_surface, 10, 10);

        outside.R8.Should().BeGreaterThan(200);
        outside.G8.Should().BeGreaterThan(200);
        outside.B8.Should().BeGreaterThan(200);
    }

    [Fact]
    public void DrawRoundRect_Filled_CentrePixelIsExpectedColor()
    {
        _painter.Clear(LokiColor.White);
        _painter.DrawRoundRect(new RectF(50f, 50f, 156f, 156f), 10f, 10f,
            PaintStyle.Fill(LokiColor.Green));

        var center = SamplePixel(_surface, 128, 128);

        center.G8.Should().BeGreaterThan(100);
        center.R8.Should().BeLessThan(50);
        center.B8.Should().BeLessThan(50);
    }

    [Fact]
    public void DrawLine_Horizontal_PixelsAlongLineAreExpectedColor()
    {
        _painter.Clear(LokiColor.White);
        _painter.DrawLine(new PointF(10f, 128f), new PointF(246f, 128f),
            new LinePaint(LokiColor.Red, Width: 4f));

        var mid = SamplePixel(_surface, 128, 128);

        mid.R8.Should().BeGreaterThan(200);
        mid.G8.Should().BeLessThan(50);
        mid.B8.Should().BeLessThan(50);
    }

    [Fact]
    public void DrawPath_FilledTriangle_CentrePixelIsExpectedColor()
    {
        _painter.Clear(LokiColor.White);

        var path = LokiPath.CreateBuilder()
            .MoveTo(new PointF(128f, 40f))
            .LineTo(new PointF(220f, 200f))
            .LineTo(new PointF(36f,  200f))
            .Close()
            .Build();

        _painter.DrawPath(path, PaintStyle.Fill(LokiColor.Red));

        var center = SamplePixel(_surface, 128, 150);

        center.R8.Should().BeGreaterThan(200);
        center.G8.Should().BeLessThan(50);
        center.B8.Should().BeLessThan(50);
    }

    [Fact]
    public void DrawImage_FitFill_CentrePixelMatchesImageColor()
    {
        _painter.Clear(LokiColor.White);

        using var blueBitmap = new SKBitmap(4, 4, SKColorType.Rgba8888, SKAlphaType.Premul);
        blueBitmap.Erase(SKColors.Blue);

        var imageRef = new ImageRef("aabbccdd", 4, 4, "image/png");
        _imageStore.Decode(Arg.Any<ImageRef>()).Returns(blueBitmap);

        _painter.DrawImage(new RectF(50f, 50f, 156f, 156f), imageRef, 1f, ImageFit.Fill);

        var center = SamplePixel(_surface, 128, 128);

        center.B8.Should().BeGreaterThan(200);
        center.R8.Should().BeLessThan(50);
    }

    [Fact]
    public void DrawImage_UnregisteredRef_DrawsMagentaPlaceholder()
    {
        _painter.Clear(LokiColor.White);

        var imageRef = new ImageRef("deadbeef", 4, 4, "image/png");
        _imageStore.Decode(Arg.Any<ImageRef>()).Throws(new StorageException("not found"));

        var act = () => _painter.DrawImage(new RectF(50f, 50f, 156f, 156f), imageRef);

        act.Should().NotThrow();

        var center = SamplePixel(_surface, 128, 128);
        center.R8.Should().BeGreaterThan(200); // magenta has full red
        center.B8.Should().BeGreaterThan(200); // magenta has full blue
        center.G8.Should().BeLessThan(50);     // magenta has no green
    }

    [Fact]
    public void DrawGlyphRun_SingleRun_DoesNotThrow()
    {
        _painter.Clear(LokiColor.White);
        var run = BuildTestGlyphRun("Hi");

        var act = () => _painter.DrawGlyphRun(new PointF(50f, 150f), run, new TextPaint(LokiColor.Black));

        act.Should().NotThrow();
    }

    [Fact]
    public void DrawGlyphRun_SingleRun_ProducesNonBlankPixels()
    {
        _painter.Clear(LokiColor.White);
        var run = BuildTestGlyphRun("Hello");

        _painter.DrawGlyphRun(new PointF(20f, 150f), run, new TextPaint(LokiColor.Black));

        _surface.Flush();

        var foundNonWhite = false;
        for (var x = 10; x < 230 && !foundNonWhite; x += 2)
        {
            for (var y = 110; y < 165 && !foundNonWhite; y += 2)
            {
                var px = SamplePixel(_surface, x, y);
                if (px.R8 < 240 || px.G8 < 240 || px.B8 < 240)
                    foundNonWhite = true;
            }
        }

        foundNonWhite.Should().BeTrue("glyph run should produce non-blank pixels");
    }

    [Fact]
    public void Save_Restore_ClipIsRestored()
    {
        _painter.Clear(LokiColor.White);

        _painter.Save();
        _painter.ClipRect(new RectF(0f, 0f, 50f, 256f));
        _painter.Clear(LokiColor.Red);
        _painter.Restore();

        // After restore, clip is gone — can draw blue at x=60+
        _painter.DrawRect(new RectF(60f, 60f, 130f, 130f), PaintStyle.Fill(LokiColor.Blue));

        var bluePixel = SamplePixel(_surface, 100, 100);
        bluePixel.B8.Should().BeGreaterThan(200);
        bluePixel.R8.Should().BeLessThan(50);
    }

    [Fact]
    public void BeginGroup_EndGroup_DoesNotThrow()
    {
        var act = () =>
        {
            _painter.BeginGroup(new RectF(0f, 0f, 256f, 256f), 0.5f);
            _painter.DrawRect(new RectF(50f, 50f, 100f, 100f), PaintStyle.Fill(LokiColor.Blue));
            _painter.EndGroup();
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void BeginGroup_WithOpacity_ReducesPixelAlpha()
    {
        _painter.Clear(LokiColor.White);

        _painter.BeginGroup(new RectF(0f, 0f, 256f, 256f), 0.5f);
        _painter.DrawRect(new RectF(0f, 0f, 256f, 256f), PaintStyle.Fill(LokiColor.Blue));
        _painter.EndGroup();

        _surface.Flush();

        var center = SamplePixel(_surface, 128, 128);

        // 50% opacity blue over white → R and G are not pure 0 (some white bleed through)
        center.B8.Should().BeGreaterThan(100);
        center.R8.Should().BeGreaterThan(50); // not pure blue; white background bleeds through
    }

    [Fact]
    public void EndGroup_WithoutBeginGroup_ThrowsInvalidOperationException()
    {
        var act = () => _painter.EndGroup();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void DrawShadow_ProducesPixelsOutsideContentBounds()
    {
        _painter.Clear(LokiColor.White);

        // Content at (30, 60, 60, 60) → right edge at x=90
        // Offset (40, 0) → shadow rect at (70, 60, 60, 60) → right edge at x=130
        _painter.DrawShadow(
            new RectF(30f, 60f, 60f, 60f),
            new PointF(40f, 0f),
            blurRadius: 4f,
            LokiColor.Black);

        _surface.Flush();

        // x=110 is outside content bounds (>90) but inside shadow rect (<130)
        var shadowPixel = SamplePixel(_surface, 110, 90);

        // Shadow pixel should be darker than pure white
        var isNotWhite = shadowPixel.R8 < 250 || shadowPixel.G8 < 250 || shadowPixel.B8 < 250;
        isNotWhite.Should().BeTrue("shadow should produce pixels outside the content bounds");
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static GlyphRun BuildTestGlyphRun(string text)
    {
        var skTypeface = SKTypeface.Default;
        var typeface   = new SkiaTypeface(skTypeface, isBundled: false, ownsTypeface: false, isVariable: false);

        using var skFont   = new SKFont(skTypeface, 24f);
        var       glyphIds = new ushort[skFont.CountGlyphs(text)];
        skFont.GetGlyphs(text.AsSpan(), glyphIds.AsSpan());
        var       advances = new float[glyphIds.Length];
        skFont.GetGlyphWidths(glyphIds.AsSpan(), advances.AsSpan(),
                              Span<SKRect>.Empty, null);

        return new GlyphRun(
            typeface,
            24f,
            ImmutableArray.Create(glyphIds),
            ImmutableArray.Create(advances),
            null,
            null);
    }
}
