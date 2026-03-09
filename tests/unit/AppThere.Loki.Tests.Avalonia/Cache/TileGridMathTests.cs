// LAYER:   AppThere.Loki.Tests.Avalonia — Tests
// KIND:    Tests
// PURPOSE: Unit tests for TileGridMath pure coordinate calculations.
//          Validates tile enumeration, doc-space rectangles, zone
//          classification, and screen-space conversion.
//          No rendering, no DI, no Avalonia platform required.
// DEPENDS: TileGridMath, TileKey, TileZone, ViewportGeometry
// USED BY: CI
// PHASE:   4
// ADR:     ADR-011

using FluentAssertions;
using AppThere.Loki.Avalonia.Cache;
using AppThere.Loki.Avalonia.Controls;

namespace AppThere.Loki.Tests.Avalonia.Cache;

public sealed class TileGridMathTests
{
    // ── TilesForViewport ──────────────────────────────────────────────────────

    [Fact]
    public void TilesForViewport_SmallViewport_ReturnsCorrectKeys()
    {
        // Viewport 512×512 pts, zoom 1.0, tileSizePx 512, doc 1024×1024 pts
        var vp = new ViewportGeometry(
            PartIndex: 0,
            ViewportWidthPts: 512f,
            ViewportHeightPts: 512f,
            ScrollOffsetXPts: 0f,
            ScrollOffsetYPts: 0f,
            Zoom: 1.0f,
            TileSizePx: 512);

        var tiles = TileGridMath.TilesForViewport(vp, 1024f, 1024f).ToList();

        tiles.Should().HaveCount(1);
        tiles[0].Should().Be(new TileKey(0, 0, 0, 1.0f));
    }

    [Fact]
    public void TilesForViewport_LargeViewport_ReturnsMultipleTiles()
    {
        // Viewport 1024×1024 pts, zoom 1.0, tileSizePx 512, doc 2048×2048 pts
        var vp = new ViewportGeometry(
            PartIndex: 0,
            ViewportWidthPts: 1024f,
            ViewportHeightPts: 1024f,
            ScrollOffsetXPts: 0f,
            ScrollOffsetYPts: 0f,
            Zoom: 1.0f,
            TileSizePx: 512);

        var tiles = TileGridMath.TilesForViewport(vp, 2048f, 2048f).ToList();

        tiles.Should().HaveCount(4);
        tiles.Should().Contain(new TileKey(0, 0, 0, 1.0f));
        tiles.Should().Contain(new TileKey(0, 1, 0, 1.0f));
        tiles.Should().Contain(new TileKey(0, 0, 1, 1.0f));
        tiles.Should().Contain(new TileKey(0, 1, 1, 1.0f));
    }

    [Fact]
    public void TilesForViewport_ScrolledRight_ReturnsCorrectColumn()
    {
        // Viewport 512×512, scrollX=512, zoom 1.0, tileSizePx 512, doc 2048×2048
        var vp = new ViewportGeometry(
            PartIndex: 0,
            ViewportWidthPts: 512f,
            ViewportHeightPts: 512f,
            ScrollOffsetXPts: 512f,
            ScrollOffsetYPts: 0f,
            Zoom: 1.0f,
            TileSizePx: 512);

        var tiles = TileGridMath.TilesForViewport(vp, 2048f, 2048f).ToList();

        tiles.Should().HaveCount(1);
        tiles[0].TileX.Should().Be(1);
        tiles[0].TileY.Should().Be(0);
    }

    [Fact]
    public void TilesForViewport_ClampsToDocBounds()
    {
        // Viewport extends beyond doc bounds
        var vp = new ViewportGeometry(
            PartIndex: 0,
            ViewportWidthPts: 2000f,
            ViewportHeightPts: 2000f,
            ScrollOffsetXPts: 0f,
            ScrollOffsetYPts: 0f,
            Zoom: 1.0f,
            TileSizePx: 512);

        float docW = 768f;
        float docH = 600f;
        var tiles = TileGridMath.TilesForViewport(vp, docW, docH).ToList();

        float tilePts = 512f / 1.0f;
        int maxCol = (int)Math.Ceiling(docW / tilePts) - 1;
        int maxRow = (int)Math.Ceiling(docH / tilePts) - 1;

        tiles.Should().NotContain(t => t.TileX > maxCol || t.TileY > maxRow);
        tiles.Should().AllSatisfy(t =>
        {
            t.TileX.Should().BeLessOrEqualTo(maxCol);
            t.TileY.Should().BeLessOrEqualTo(maxRow);
        });
    }

    // ── TileRect ──────────────────────────────────────────────────────────────

    [Fact]
    public void TileRect_ZeroZero_ReturnsOrigin()
    {
        var rect = TileGridMath.TileRect(new TileKey(0, 0, 0, 1.0f), 512);

        rect.X.Should().Be(0f);
        rect.Y.Should().Be(0f);
        rect.Width.Should().Be(512f);
        rect.Height.Should().Be(512f);
    }

    [Fact]
    public void TileRect_TileOneOne_ReturnsOffset()
    {
        var rect = TileGridMath.TileRect(new TileKey(0, 1, 1, 1.0f), 512);

        rect.X.Should().Be(512f);
        rect.Y.Should().Be(512f);
    }

    [Fact]
    public void TileRect_HalfZoom_DoublePtSize()
    {
        var rect = TileGridMath.TileRect(new TileKey(0, 0, 0, 0.5f), 512);

        rect.Width.Should().BeApproximately(1024f, 0.01f);
        rect.Height.Should().BeApproximately(1024f, 0.01f);
    }

    // ── ZoneForTile ───────────────────────────────────────────────────────────

    [Fact]
    public void ZoneForTile_VisibleTile_IsHot()
    {
        var vp = new ViewportGeometry(0, 512f, 512f, 0f, 0f, 1.0f, 512);

        var zone = TileGridMath.ZoneForTile(
            new TileKey(0, 0, 0, 1.0f), vp, keepMult: 2.0f, retainMult: 4.0f);

        zone.Should().Be(TileZone.Hot);
    }

    [Fact]
    public void ZoneForTile_AdjacentTile_IsWarm()
    {
        // Viewport covers only tile (0,0); tile (1,0) is immediately adjacent.
        var vp = new ViewportGeometry(0, 512f, 512f, 0f, 0f, 1.0f, 512);

        var zone = TileGridMath.ZoneForTile(
            new TileKey(0, 1, 0, 1.0f), vp, keepMult: 2.0f, retainMult: 4.0f);

        zone.Should().Be(TileZone.Warm);
    }

    [Fact]
    public void ZoneForTile_FarTile_IsCold()
    {
        // Viewport covers tile (0,0); tile (5,0) is 5 tiles away → beyond retain.
        var vp = new ViewportGeometry(0, 512f, 512f, 0f, 0f, 1.0f, 512);

        var zone = TileGridMath.ZoneForTile(
            new TileKey(0, 5, 0, 1.0f), vp, keepMult: 2.0f, retainMult: 4.0f);

        zone.Should().Be(TileZone.Cold);
    }

    // ── ScreenRect ────────────────────────────────────────────────────────────

    [Fact]
    public void ScreenRect_VisibleTile_PositiveCoords()
    {
        var vp = new ViewportGeometry(0, 512f, 512f, 0f, 0f, 1.0f, 512);

        var rect = TileGridMath.ScreenRect(new TileKey(0, 0, 0, 1.0f), vp);

        rect.X.Should().Be(0);
        rect.Y.Should().Be(0);
        rect.Width.Should().Be(512);
        rect.Height.Should().Be(512);
    }

    [Fact]
    public void ScreenRect_ScrolledTile_NegativeOffset()
    {
        // scrollOffsetX = 256: tile at doc-x=0 appears at screen-x = -256
        var vp = new ViewportGeometry(0, 512f, 512f, 256f, 0f, 1.0f, 512);

        var rect = TileGridMath.ScreenRect(new TileKey(0, 0, 0, 1.0f), vp);

        rect.X.Should().BeApproximately(-256, 0.01);
    }
}
