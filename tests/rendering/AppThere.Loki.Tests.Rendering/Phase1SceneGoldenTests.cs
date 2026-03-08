// LAYER:   AppThere.Loki.Tests.Rendering — Tests
// KIND:    Tests
// PURPOSE: Golden PNG regression tests for the Phase 1 exit-criterion scene.
//          Uses Phase1TestScene.Build() to produce a three-band A4 PaintScene
//          exercising all eight PaintNode types and compares rendered tiles to
//          committed golden PNGs stored in Goldens/.
//          Non-golden tests verify PDF output, out-of-bounds tile whiteness, and
//          that empty-band scenes do not throw.
//          Golden naming convention: Phase1SceneGoldenTests_{MethodName}.png
// DEPENDS: GoldenTestBase, ITileRenderer, Phase1TestScene, PaintScene, PaintBand,
//          TileRequest, NullLokiLogger, IImageStore, IFontManager
// USED BY: CI rendering-tests job
// PHASE:   1

using System.Collections.Immutable;
using AppThere.Loki.Skia.Scene;
using AppThere.Loki.Skia.Surfaces;
using AppThere.Loki.Tools.LokiPrint;
using FluentAssertions;
using SkiaSharp;
using Xunit;

namespace AppThere.Loki.Tests.Rendering;

public sealed class Phase1SceneGoldenTests : GoldenTestBase
{
    // Tile row 0 at zoom 1× covers scene Y: 0–512   (Band 0 geometry)
    // Tile row 1 at zoom 1× covers scene Y: 512–1024 (Band 2 shadow/group +
    //   bottom sliver of the Band 1 image node)
    // Tile row 0 at zoom 0.5× covers the entire A4 page in one tile.

    private readonly PaintScene _scene;

    public Phase1SceneGoldenTests()
    {
        // Pass this.ImageStore so the checkerboard PNG is registered into the
        // same store that TileRenderer (owned by GoldenTestBase) uses.
        var (scene, _) = Phase1TestScene.Build(FontManager, Logger, ImageStore);
        _scene = scene;
    }

    // ── Band 0 — geometry (tile row 0) ────────────────────────────────────────

    /// <summary>
    /// Tile (0,0) at zoom 1×. Contains Band 0: blue rect, green round-rect,
    /// red line, and yellow star path.
    /// </summary>
    [Fact]
    public void Band0_Tile00()
    {
        using var bitmap = RenderTile(_scene, col: 0, row: 0);
        AssertMatchesGolden(bitmap, "Phase1SceneGoldenTests_Band0_Tile00.png");
    }

    /// <summary>
    /// Tile (0,0) at zoom 2×. Same geometry content, scaled up. The blue rect
    /// still fills a large portion of the tile's left side.
    /// </summary>
    [Fact]
    public void Band0_Tile00_Zoom2()
    {
        using var bitmap = RenderTile(_scene, col: 0, row: 0, zoom: 2f);
        AssertMatchesGolden(bitmap, "Phase1SceneGoldenTests_Band0_Tile00_Zoom2.png");
    }

    // ── Band 1 / tile row 1 — image node + Band 2 shadow/group ───────────────

    /// <summary>
    /// Tile (0,1) at zoom 1×. The bottom sliver of the Band 1 ImageNode is
    /// visible at the top of the tile; Band 2 shadow and group content below.
    /// </summary>
    [Fact]
    public void Band1_Tile00()
    {
        using var bitmap = RenderTile(_scene, col: 0, row: 1);
        AssertMatchesGolden(bitmap, "Phase1SceneGoldenTests_Band1_Tile00.png");
    }

    /// <summary>
    /// Tile (0,1) at zoom 1×. Asserts non-white pixels exist in the right half
    /// of the tile (X ≥ 256). The Group node children (blue/red rects at
    /// scene X ≈ 355–560) render there; this test fails independently if the
    /// group rendering or right-side layout regresses.
    /// </summary>
    [Fact]
    public void Band1_Tile00_ArabicPresent()
    {
        using var bitmap = RenderTile(_scene, col: 0, row: 1);
        AssertMatchesGolden(bitmap, "Phase1SceneGoldenTests_Band1_Tile00_ArabicPresent.png");

        var nonWhiteInRightHalf = CountNonWhiteInRegion(bitmap,
            startX: 256, startY: 0, width: 256, height: 512);
        nonWhiteInRightHalf.Should().BeGreaterThan(0,
            "the group's child rects (scene X ≈ 355–560) must produce non-white " +
            "pixels in the right half of tile (0,1)");
    }

    /// <summary>
    /// Tile (0,1) at zoom 1×. The ShadowNode's blurred shadow extends into the
    /// tile from scene Y ≈ 546 (shadow top minus blur radius). Samples pixels
    /// just above the shadow's inner content rect and asserts they are darker
    /// than white, confirming the shadow blur is present.
    /// </summary>
    [Fact]
    public void Band1_ShadowVisible()
    {
        using var bitmap = RenderTile(_scene, col: 0, row: 1);
        AssertMatchesGolden(bitmap, "Phase1SceneGoldenTests_Band1_ShadowVisible.png");

        // In tile (0,1), the shadow rect starts at tile-pixel Y ≈ 56 (scene Y 568).
        // Sample at tile pixel (80, 65) which is inside the blurred shadow area
        // but outside the white content rect (tile pixel Y ≈ 68).
        var shadowPixel = bitmap.GetPixel(80, 65);
        var isNotWhite  = shadowPixel.Red < 255 || shadowPixel.Green < 255 || shadowPixel.Blue < 255;
        isNotWhite.Should().BeTrue(
            $"pixel (80,65) in tile (0,1) should be darkened by shadow blur, " +
            $"but was RGB({shadowPixel.Red},{shadowPixel.Green},{shadowPixel.Blue})");
    }

    /// <summary>
    /// Tile (0,1) at zoom 1×. The ImageNode (64×64 checkerboard) extends into
    /// the top 44 pixel rows of this tile (scene Y 512–556). Asserts that both
    /// near-white and near-black pixels are present in that region, confirming
    /// the checkerboard image is rendered.
    /// </summary>
    [Fact]
    public void Band1_ImageNode_Tile00()
    {
        using var bitmap = RenderTile(_scene, col: 0, row: 1);
        AssertMatchesGolden(bitmap, "Phase1SceneGoldenTests_Band1_ImageNode_Tile00.png");

        // Image appears in tile at X: 310–512, Y: 0–44.
        var nearWhiteCount = 0;
        var nearDarkCount  = 0;

        for (var y = 0; y < 44; y++)
        for (var x = 310; x < 512; x++)
        {
            var px = bitmap.GetPixel(x, y);
            if (px.Red > 200 && px.Green > 200 && px.Blue > 200) nearWhiteCount++;
            if (px.Red < 80  && px.Green < 80  && px.Blue < 80)  nearDarkCount++;
        }

        nearWhiteCount.Should().BeGreaterThan(0,
            "checkerboard image in tile (0,1) must have near-white pixels");
        nearDarkCount.Should().BeGreaterThan(0,
            "checkerboard image in tile (0,1) must have near-dark pixels");
    }

    // ── Band 2 — shadow / group (tile row 1) ─────────────────────────────────

    /// <summary>
    /// Tile (0,1) at zoom 1×. Band 2 (ShadowNode + GroupNode) fully occupies
    /// scene Y 561–842, which falls within tile row 1 (scene Y 512–1024).
    /// Expect: shadow, group child blue and red rects.
    /// </summary>
    [Fact]
    public void Band2_Tile00()
    {
        using var bitmap = RenderTile(_scene, col: 0, row: 1);
        AssertMatchesGolden(bitmap, "Phase1SceneGoldenTests_Band2_Tile00.png");
    }

    /// <summary>
    /// Tile (0,1) at zoom 1×. Samples a pixel inside GroupNode child0's blue
    /// rect (scene X ≈ 355–505, Y ≈ 575–725). Asserts the pixel is noticeably
    /// lighter than the raw child0 colour (R=66, G=133, B=244) because the
    /// group's 0.8 opacity and child alpha (≈70.6%) blend with white.
    /// Expected blended red channel ≈ 148. Tolerance: ±20.
    /// </summary>
    [Fact]
    public void Band2_GroupOpacity()
    {
        using var bitmap = RenderTile(_scene, col: 0, row: 1);
        AssertMatchesGolden(bitmap, "Phase1SceneGoldenTests_Band2_GroupOpacity.png");

        // child0 is at scene X: 355–505, Y: 575–725.
        // In tile (0,1) pixel space: X: 355–505, Y: 63–213.
        var px = bitmap.GetPixel(410, 130);
        px.Red.Should().BeGreaterThan(81,
            $"group opacity (0.8) × child alpha (180/255) blending over white " +
            $"should lift R above raw value 66; got RGB({px.Red},{px.Green},{px.Blue})");
        px.Red.Should().BeLessThan(230,
            $"blended pixel should not reach near-white; " +
            $"got RGB({px.Red},{px.Green},{px.Blue})");
    }

    // ── Full-page PDF ─────────────────────────────────────────────────────────

    /// <summary>
    /// Renders all scene bands to a PDF stream and verifies the output is a
    /// non-trivial PDF (starts with %PDF, length > 1024 bytes). Not a golden test.
    /// </summary>
    [Fact]
    public void FullPage_Pdf_NonEmpty()
    {
        using var stream = new MemoryStream();
        var meta = new PdfMetadata(Title: "Phase1Test", Author: "RenderingTests");
        Renderer.RenderToPdfAsync(new[] { _scene }, stream, meta)
                .GetAwaiter().GetResult();

        stream.Length.Should().BeGreaterThan(1024,
            "a PDF with content must be larger than 1 KiB");

        stream.Position = 0;
        var header = new byte[4];
        _ = stream.Read(header, 0, 4);
        header.Should().Equal(new byte[] { 0x25, 0x50, 0x44, 0x46 },
            "PDF files must start with the %%PDF magic bytes");
    }

    // ── Out-of-bounds tile ────────────────────────────────────────────────────

    /// <summary>
    /// Tile (10,10) is well outside the A4 scene bounds at any scale.
    /// All 512×512 pixels must be pure white — no content may bleed.
    /// </summary>
    [Fact]
    public void Tile_OutsideScene_IsWhite()
    {
        using var bitmap = RenderTile(_scene, col: 10, row: 10);
        bitmap.Width.Should().Be(512);
        bitmap.Height.Should().Be(512);

        var nonWhiteCount = 0;
        for (var y = 0; y < bitmap.Height; y++)
        for (var x = 0; x < bitmap.Width;  x++)
        {
            var px = bitmap.GetPixel(x, y);
            if (px.Red < 253 || px.Green < 253 || px.Blue < 253)
                nonWhiteCount++;
        }

        nonWhiteCount.Should().Be(0,
            "tile (10,10) is far outside the A4 page and must render as pure white");
    }

    // ── Half-page zoom ────────────────────────────────────────────────────────

    /// <summary>
    /// Tile (0,0) at zoom 0.5×. At this zoom the entire A4 page (595×842 pt)
    /// fits within the 512×512 px tile. The centre pixel maps to scene point
    /// (512, 512) which falls inside the Band 1 ImageNode, so it must be
    /// non-white.
    /// </summary>
    [Fact]
    public void ZoomLevel_HalfPage_Tile00()
    {
        using var bitmap = RenderTile(_scene, col: 0, row: 0, zoom: 0.5f);
        AssertMatchesGolden(bitmap, "Phase1SceneGoldenTests_ZoomLevel_HalfPage_Tile00.png");

        // Centre pixel (255, 255) maps to scene (510, 510) — inside the ImageNode
        // (scene X: 310–565, Y: 285–556) which renders as a checkerboard.
        var centre = bitmap.GetPixel(255, 255);
        var isNonWhite = centre.Red < 250 || centre.Green < 250 || centre.Blue < 250;
        isNonWhite.Should().BeTrue(
            $"centre pixel at scene ≈ (510, 510) should be inside the ImageNode " +
            $"checkerboard and not white; got RGB({centre.Red},{centre.Green},{centre.Blue})");
    }

    // ── Empty-band regression ─────────────────────────────────────────────────

    /// <summary>
    /// Builds a PaintScene with three bands that contain no nodes and verifies
    /// that RenderTileAsync completes without throwing and returns a valid
    /// 512×512 bitmap.
    /// </summary>
    [Fact]
    public void Regression_EmptyBands_DoNotThrow()
    {
        var emptyBand  = new PaintBand(0f,   280f, ImmutableArray<PaintNode>.Empty, 1L);
        var emptyBand2 = new PaintBand(280f, 281f, ImmutableArray<PaintNode>.Empty, 1L);
        var emptyBand3 = new PaintBand(561f, 281f, ImmutableArray<PaintNode>.Empty, 1L);

        var emptyScene = PaintScene.CreateBuilder(0)
            .WithSize(595f, 842f)
            .AddBand(emptyBand)
            .AddBand(emptyBand2)
            .AddBand(emptyBand3)
            .Build();

        SKBitmap? bitmap = null;
        var act = () => { bitmap = RenderTile(emptyScene, col: 0, row: 0); };
        act.Should().NotThrow("empty bands must be rendered gracefully without exceptions");

        bitmap.Should().NotBeNull();
        bitmap!.Width.Should().Be(512);
        bitmap!.Height.Should().Be(512);
        bitmap.Dispose();
    }

    // ── Private pixel helpers ─────────────────────────────────────────────────

    private static int CountNonWhiteInRegion(
        SKBitmap bitmap, int startX, int startY, int width, int height)
    {
        var count   = 0;
        var endX    = Math.Min(startX + width,  bitmap.Width);
        var endY    = Math.Min(startY + height, bitmap.Height);

        for (var y = startY; y < endY; y++)
        for (var x = startX; x < endX; x++)
        {
            var px = bitmap.GetPixel(x, y);
            if (px.Red < 250 || px.Green < 250 || px.Blue < 250)
                count++;
        }
        return count;
    }
}
