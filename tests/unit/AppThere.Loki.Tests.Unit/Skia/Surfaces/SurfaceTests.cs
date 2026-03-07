// LAYER:   AppThere.Loki.Tests.Unit — Tests
// KIND:    Tests
// PURPOSE: Specification tests for BitmapRenderSurface, PdfRenderSurface, and
//          HeadlessSurfaceFactory. Covers construction, dimension validation,
//          pixel format, flush/close lifecycle, idempotent dispose, and factory
//          return types. No binary files — PDFs are written to MemoryStream.
// DEPENDS: BitmapRenderSurface, PdfRenderSurface, HeadlessSurfaceFactory,
//          IRenderSurface, IRenderSurfaceFactory, PdfMetadata, NullLokiLogger
// USED BY: CI
// PHASE:   1
// ADR:     ADR-002

using AppThere.Loki.Kernel.Geometry;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.Skia.Surfaces;
using AppThere.Loki.Tests.Unit.Skia;
using FluentAssertions;
using SkiaSharp;

namespace AppThere.Loki.Tests.Unit.Skia.Surfaces;

public sealed class SurfaceTests
{
    private static readonly SizeF    ValidPixelSize = new(512f, 512f);
    private static readonly SizeF    ValidPageSize  = new(595f, 842f); // A4 in points
    private static readonly PdfMetadata DefaultMeta = new("Test", "Tester", "Loki");

    static SurfaceTests()
    {
        SkiaTestInitializer.EnsureSkiaSharpLoaded();
    }

    // ── BitmapRenderSurface ───────────────────────────────────────────────────

    [Fact]
    public void Create_ValidSize_DoesNotThrow()
    {
        var act = () => { using var s = new BitmapRenderSurface(64, 64); };

        act.Should().NotThrow();
    }

    [Fact]
    public void Create_ZeroWidth_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new BitmapRenderSurface(0, 64);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_ZeroHeight_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new BitmapRenderSurface(64, 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GetBitmap_AfterCreate_HasCorrectDimensions()
    {
        using var surface = new BitmapRenderSurface(128, 256);

        var bitmap = surface.GetBitmap();

        bitmap.Width.Should().Be(128);
        bitmap.Height.Should().Be(256);
    }

    [Fact]
    public void GetBitmap_AfterCreate_IsRgba8888()
    {
        using var surface = new BitmapRenderSurface(64, 64);

        var bitmap = surface.GetBitmap();

        bitmap.ColorType.Should().Be(SKColorType.Rgba8888);
    }

    [Fact]
    public void Flush_AfterCreate_DoesNotThrow()
    {
        using var surface = new BitmapRenderSurface(64, 64);

        var act = () => surface.Flush();

        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var surface = new BitmapRenderSurface(64, 64);

        surface.Dispose();
        var act = () => surface.Dispose();

        act.Should().NotThrow();
    }

    // ── PdfRenderSurface ──────────────────────────────────────────────────────

    [Fact]
    public void PdfCreate_ValidArgs_DoesNotThrow()
    {
        using var stream = new MemoryStream();
        var act = () =>
        {
            using var s = new PdfRenderSurface(stream, ValidPageSize, DefaultMeta);
            s.Close();
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void PdfCanvas_AfterCreate_IsNotNull()
    {
        using var stream  = new MemoryStream();
        using var surface = new PdfRenderSurface(stream, ValidPageSize, DefaultMeta);

        surface.Canvas.Should().NotBeNull();

        surface.Close();
    }

    [Fact]
    public void PdfClose_CalledTwice_DoesNotThrow()
    {
        using var stream  = new MemoryStream();
        using var surface = new PdfRenderSurface(stream, ValidPageSize, DefaultMeta);

        surface.Close();
        var act = () => surface.Close();

        act.Should().NotThrow();
    }

    [Fact]
    public void PdfClose_WritesToStream()
    {
        using var stream  = new MemoryStream();
        using var surface = new PdfRenderSurface(stream, ValidPageSize, DefaultMeta);

        surface.Close();

        stream.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void PdfDispose_AfterClose_DoesNotThrow()
    {
        using var stream  = new MemoryStream();
        var surface = new PdfRenderSurface(stream, ValidPageSize, DefaultMeta);

        surface.Close();
        var act = () => surface.Dispose();

        act.Should().NotThrow();
    }

    // ── HeadlessSurfaceFactory ────────────────────────────────────────────────

    [Fact]
    public void IsGpuAvailable_AlwaysFalse()
    {
        var factory = new HeadlessSurfaceFactory(NullLokiLogger.Instance);

        factory.IsGpuAvailable.Should().BeFalse();
    }

    [Fact]
    public void PreferredBackend_AlwaysCpu()
    {
        var factory = new HeadlessSurfaceFactory(NullLokiLogger.Instance);

        factory.PreferredBackend.Should().Be(RenderBackend.Cpu);
    }

    [Fact]
    public void CreateTileSurface_ValidSize_ReturnsBitmapRenderSurface()
    {
        var factory = new HeadlessSurfaceFactory(NullLokiLogger.Instance);

        using var surface = factory.CreateTileSurface(ValidPixelSize);

        surface.Should().BeOfType<BitmapRenderSurface>();
    }

    [Fact]
    public void CreateHeadlessBitmapSurface_ValidSize_ReturnsBitmapRenderSurface()
    {
        var factory = new HeadlessSurfaceFactory(NullLokiLogger.Instance);

        using var surface = factory.CreateHeadlessBitmapSurface(ValidPixelSize);

        surface.Should().BeOfType<BitmapRenderSurface>();
    }

    [Fact]
    public void CreatePdfSurface_ValidArgs_ReturnsPdfRenderSurface()
    {
        var factory = new HeadlessSurfaceFactory(NullLokiLogger.Instance);
        using var stream = new MemoryStream();

        using var surface = factory.CreatePdfSurface(stream, ValidPageSize, DefaultMeta);

        surface.Should().BeOfType<PdfRenderSurface>();
        ((PdfRenderSurface)surface).Close();
    }
}
