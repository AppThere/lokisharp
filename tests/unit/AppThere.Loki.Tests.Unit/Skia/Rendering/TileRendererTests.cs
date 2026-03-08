// LAYER:   AppThere.Loki.Tests.Unit — Tests
// KIND:    Tests
// PURPOSE: Specification tests for TileRenderer. Verifies tile coordinate mapping,
//          zoom transforms, band/node culling, node dispatch for all node types,
//          and error propagation. Uses HeadlessSurfaceFactory (real) and pixel-
//          sampling to assert rendering correctness. IImageStore is mocked with
//          NSubstitute only for the failure test.
// DEPENDS: TileRenderer, ITileRenderer, HeadlessSurfaceFactory, TileRequest,
//          PaintScene, PaintBand, PaintNode, RectNode, GroupNode, ShadowNode,
//          ImageNode, LokiSkiaPainter, TileRenderException, PdfRenderException,
//          PdfMetadata, NullLokiLogger, LokiColor, RectF, PointF, ImmutableArray
// USED BY: CI
// PHASE:   1
// ADR:     ADR-002, ADR-003, ADR-004

using System.Collections.Immutable;
using AppThere.Loki.Kernel.Color;
using AppThere.Loki.Kernel.Geometry;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.Skia.Errors;
using AppThere.Loki.Skia.Images;
using AppThere.Loki.Skia.Rendering;
using AppThere.Loki.Skia.Scene;
using AppThere.Loki.Skia.Scene.Nodes;
using AppThere.Loki.Skia.Surfaces;
using AppThere.Loki.Tests.Unit.Skia;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SkiaSharp;

namespace AppThere.Loki.Tests.Unit.Skia.Rendering;

public sealed class TileRendererTests
{
    private readonly HeadlessSurfaceFactory _factory;
    private readonly IImageStore            _imageStore;
    private readonly TileRenderer           _renderer;

    static TileRendererTests()
    {
        SkiaTestInitializer.EnsureSkiaSharpLoaded();
    }

    public TileRendererTests()
    {
        _factory    = new HeadlessSurfaceFactory(NullLokiLogger.Instance);
        _imageStore = Substitute.For<IImageStore>();
        _renderer   = new TileRenderer(_factory, _imageStore, NullLokiLogger.Instance);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>A4 scene with a single red rect covering the entire part.</summary>
    private static PaintScene RedRectScene()
    {
        var node = new RectNode(new RectF(0f, 0f, 595f, 842f), PaintStyle.Fill(LokiColor.Red));
        var band = new PaintBand(0f, 842f, ImmutableArray.Create<PaintNode>(node), 1L);
        return PaintScene.CreateBuilder(0).WithSize(595f, 842f).AddBand(band).Build();
    }

    private static PaintScene EmptyScene() =>
        PaintScene.CreateBuilder(0).WithSize(595f, 842f).Build();

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RenderTileAsync_RedRectScene_Tile00_CentrePixelIsRed()
    {
        var request = TileRequest.ForHeadless(0, 1f, 0, 0);

        using var bitmap = await _renderer.RenderTileAsync(RedRectScene(), request);

        var px = bitmap.GetPixel(256, 256);
        px.Red.Should().BeGreaterThan(200);
        px.Green.Should().BeLessThan(50);
        px.Blue.Should().BeLessThan(50);
    }

    [Fact]
    public async Task RenderTileAsync_RedRectScene_Tile00_ReturnsBitmapOfCorrectSize()
    {
        var request = TileRequest.ForHeadless(0, 1f, 0, 0);

        using var bitmap = await _renderer.RenderTileAsync(RedRectScene(), request);

        bitmap.Width.Should().Be(request.PixelSize);
        bitmap.Height.Should().Be(request.PixelSize);
    }

    [Fact]
    public async Task RenderTileAsync_EmptyScene_Tile00_ReturnsWhiteBitmap()
    {
        var request = TileRequest.ForHeadless(0, 1f, 0, 0);

        using var bitmap = await _renderer.RenderTileAsync(EmptyScene(), request);

        var px = bitmap.GetPixel(256, 256);
        px.Red.Should().BeGreaterThan(200);
        px.Green.Should().BeGreaterThan(200);
        px.Blue.Should().BeGreaterThan(200);
    }

    [Fact]
    public async Task RenderTileAsync_ZoomLevel2_Tile00_CentrePixelIsRed()
    {
        // At zoom=2, tile (0,0) covers logical (0,0,256,256). Red rect is 595×842.
        var request = TileRequest.ForHeadless(0, 2f, 0, 0);

        using var bitmap = await _renderer.RenderTileAsync(RedRectScene(), request);

        var px = bitmap.GetPixel(256, 256);
        px.Red.Should().BeGreaterThan(200);
        px.Green.Should().BeLessThan(50);
        px.Blue.Should().BeLessThan(50);
    }

    [Fact]
    public async Task RenderTileAsync_NodeOutsideTileBounds_NotRendered()
    {
        // Blue rect in tile (1,0) logical space — x > 512 at zoom=1.
        var blueNode = new RectNode(new RectF(560f, 100f, 100f, 100f), PaintStyle.Fill(LokiColor.Blue));
        var band     = new PaintBand(0f, 842f, ImmutableArray.Create<PaintNode>(blueNode), 1L);
        var scene    = PaintScene.CreateBuilder(0).WithSize(595f, 842f).AddBand(band).Build();

        // Render tile (0,0): covers logical x=[0,512), y=[0,512)
        var request = TileRequest.ForHeadless(0, 1f, 0, 0);
        using var bitmap = await _renderer.RenderTileAsync(scene, request);

        // Centre should be white — the blue rect was culled
        var px = bitmap.GetPixel(256, 256);
        px.Red.Should().BeGreaterThan(200);
        px.Green.Should().BeGreaterThan(200);
    }

    [Fact]
    public async Task RenderTileAsync_ShadowNode_DoesNotThrow()
    {
        var content = new RectNode(
            new RectF(50f, 50f, 100f, 100f), PaintStyle.Fill(LokiColor.Blue));
        var shadow = new ShadowNode(
            new RectF(40f, 40f, 120f, 120f),
            content,
            new PointF(10f, 10f),
            BlurRadius: 5f,
            ShadowColor: LokiColor.Black);

        var band  = new PaintBand(0f, 842f, ImmutableArray.Create<PaintNode>(shadow), 1L);
        var scene = PaintScene.CreateBuilder(0).WithSize(595f, 842f).AddBand(band).Build();

        SKBitmap? bitmap = null;
        var act = async () => bitmap = await _renderer.RenderTileAsync(
            scene, TileRequest.ForHeadless(0, 1f, 0, 0));

        await act.Should().NotThrowAsync();
        bitmap?.Dispose();
    }

    [Fact]
    public async Task RenderTileAsync_GroupNode_ChildrenRendered()
    {
        // Red rect child centred at logical (256,256) — within tile (0,0) at zoom=1.
        var child = new RectNode(
            new RectF(100f, 100f, 300f, 300f), PaintStyle.Fill(LokiColor.Red));
        var group = new GroupNode(
            new RectF(100f, 100f, 300f, 300f),
            ImmutableArray.Create<PaintNode>(child));

        var band  = new PaintBand(0f, 842f, ImmutableArray.Create<PaintNode>(group), 1L);
        var scene = PaintScene.CreateBuilder(0).WithSize(595f, 842f).AddBand(band).Build();

        using var bitmap = await _renderer.RenderTileAsync(
            scene, TileRequest.ForHeadless(0, 1f, 0, 0));

        // Pixel at (250,250) is within the child rect — should be red
        var px = bitmap.GetPixel(250, 250);
        px.Red.Should().BeGreaterThan(200);
        px.Green.Should().BeLessThan(50);
        px.Blue.Should().BeLessThan(50);
    }

    [Fact]
    public async Task RenderTileAsync_OnRenderFailure_ThrowsTileRenderException()
    {
        // IImageStore throws InvalidOperationException (not StorageException);
        // LokiSkiaPainter does not catch it, so it propagates to TileRenderer
        // which wraps it in TileRenderException.
        var brokenStore = Substitute.For<IImageStore>();
        brokenStore.Decode(Arg.Any<ImageRef>())
                   .Throws(new InvalidOperationException("Broken image store"));

        var renderer  = new TileRenderer(_factory, brokenStore, NullLokiLogger.Instance);
        var imageRef  = new ImageRef("aabbccdd", 4, 4, "image/png");
        var imageNode = new ImageNode(new RectF(0f, 0f, 100f, 100f), imageRef);
        var band      = new PaintBand(0f, 842f, ImmutableArray.Create<PaintNode>(imageNode), 1L);
        var scene     = PaintScene.CreateBuilder(0).WithSize(595f, 842f).AddBand(band).Build();

        Func<Task<SKBitmap>> act = () =>
            renderer.RenderTileAsync(scene, TileRequest.ForHeadless(0, 1f, 0, 0));

        await act.Should().ThrowAsync<TileRenderException>();
    }

    [Fact]
    public async Task RenderToPdfAsync_SingleScene_WritesNonEmptyStream()
    {
        using var stream = new MemoryStream();
        var meta = new PdfMetadata("Test", "Tester", "Loki");

        await _renderer.RenderToPdfAsync(new[] { RedRectScene() }, stream, meta);

        stream.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RenderToPdfAsync_MultipleScenes_WritesNonEmptyStream()
    {
        using var stream = new MemoryStream();
        var meta = new PdfMetadata("Test", "Tester", "Loki");

        await _renderer.RenderToPdfAsync(
            new[] { RedRectScene(), RedRectScene() }, stream, meta);

        stream.Length.Should().BeGreaterThan(0);
    }
}
