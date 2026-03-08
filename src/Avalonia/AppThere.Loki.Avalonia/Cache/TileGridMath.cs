// LAYER:   AppThere.Loki.Avalonia — Tile Cache
// KIND:    Implementation (static utility)
// PURPOSE: Pure tile-grid coordinate mathematics. No I/O, no DI, no state.
//          Converts between viewport geometry, document points, tile grid
//          coordinates, and screen DIPs. Used by LokiTileCache to enumerate
//          visible tiles and compute eviction zones.
//          Does NOT render tiles or access any cache state.
// DEPENDS: ViewportGeometry, TileKey, TileZone, RectF
// USED BY: LokiTileCache
// PHASE:   4
// ADR:     ADR-011

using Avalonia;
using AppThere.Loki.Avalonia.Controls;
using AppThere.Loki.Kernel.Geometry;

namespace AppThere.Loki.Avalonia.Cache;

internal static class TileGridMath
{
    /// <summary>
    /// Enumerates every TileKey whose document-space rectangle intersects
    /// the viewport. Results are clamped to document bounds.
    /// </summary>
    public static IEnumerable<TileKey> TilesForViewport(
        ViewportGeometry vp,
        float docWidthPts,
        float docHeightPts)
    {
        float tilePts = vp.TileSizePx / vp.Zoom;

        int firstCol = (int)Math.Floor(vp.ScrollOffsetXPts / tilePts);
        int firstRow = (int)Math.Floor(vp.ScrollOffsetYPts / tilePts);

        // Use ceiling so the right/bottom boundary tile is excluded when the
        // viewport edge falls exactly on a tile boundary (half-open interval).
        int lastCol = (int)Math.Ceiling((vp.ScrollOffsetXPts + vp.ViewportWidthPts) / tilePts) - 1;
        int lastRow = (int)Math.Ceiling((vp.ScrollOffsetYPts + vp.ViewportHeightPts) / tilePts) - 1;

        // Clamp to document bounds.
        int maxCol = (int)Math.Ceiling(docWidthPts / tilePts) - 1;
        int maxRow = (int)Math.Ceiling(docHeightPts / tilePts) - 1;

        firstCol = Math.Max(0, firstCol);
        firstRow = Math.Max(0, firstRow);
        lastCol  = Math.Min(lastCol, maxCol);
        lastRow  = Math.Min(lastRow, maxRow);

        for (int row = firstRow; row <= lastRow; row++)
            for (int col = firstCol; col <= lastCol; col++)
                yield return new TileKey(vp.PartIndex, col, row, vp.Zoom);
    }

    /// <summary>
    /// Returns the document-space rectangle (in points) covered by the tile.
    /// Origin = (TileX * tileSizePx / Zoom, TileY * tileSizePx / Zoom).
    /// Width and height = tileSizePx / Zoom.
    /// </summary>
    public static RectF TileRect(TileKey key, int tileSizePx)
    {
        float tilePts = tileSizePx / key.Zoom;
        return new RectF(
            key.TileX * tilePts,
            key.TileY * tilePts,
            tilePts,
            tilePts);
    }

    /// <summary>
    /// Classifies a tile into a TileZone relative to the current viewport.
    /// Hot  — tile intersects the visible viewport rectangle.
    /// Warm — Chebyshev distance ≤ keepMult × viewport tile count.
    /// Cool — Chebyshev distance ≤ retainMult × viewport tile count.
    /// Cold — beyond retainMult.
    /// </summary>
    public static TileZone ZoneForTile(
        TileKey key,
        ViewportGeometry vp,
        float keepMult,
        float retainMult)
    {
        float tilePts = vp.TileSizePx / vp.Zoom;

        int firstVisCol = (int)Math.Floor(vp.ScrollOffsetXPts / tilePts);
        int firstVisRow = (int)Math.Floor(vp.ScrollOffsetYPts / tilePts);
        int lastVisCol  = (int)Math.Ceiling((vp.ScrollOffsetXPts + vp.ViewportWidthPts)  / tilePts) - 1;
        int lastVisRow  = (int)Math.Ceiling((vp.ScrollOffsetYPts + vp.ViewportHeightPts) / tilePts) - 1;

        if (key.TileX >= firstVisCol && key.TileX <= lastVisCol &&
            key.TileY >= firstVisRow && key.TileY <= lastVisRow)
            return TileZone.Hot;

        // Chebyshev distance in tiles to nearest visible tile.
        int nearestX = Math.Clamp(key.TileX, firstVisCol, lastVisCol);
        int nearestY = Math.Clamp(key.TileY, firstVisRow, lastVisRow);
        int distance = Math.Max(
            Math.Abs(key.TileX - nearestX),
            Math.Abs(key.TileY - nearestY));

        // Viewport tile count: maximum of X and Y tile spans.
        float viewportTilesX = Math.Max(1f, lastVisCol - firstVisCol + 1f);
        float viewportTilesY = Math.Max(1f, lastVisRow - firstVisRow + 1f);
        float viewportTileCount = Math.Max(viewportTilesX, viewportTilesY);

        if (distance <= keepMult * viewportTileCount)
            return TileZone.Warm;

        if (distance <= retainMult * viewportTileCount)
            return TileZone.Cool;

        return TileZone.Cold;
    }

    /// <summary>
    /// Converts a tile's document-space rect to screen device-independent
    /// pixels (DIPs).
    /// screenX = (tileDocX - scrollOffsetXPts) * zoom
    /// screenY = (tileDocY - scrollOffsetYPts) * zoom
    /// Width and height = tileSizePx (tiles are always tileSizePx DIPs on screen).
    /// </summary>
    public static Rect ScreenRect(TileKey key, ViewportGeometry vp)
    {
        float tileDocX = key.TileX * vp.TileSizePx / vp.Zoom;
        float tileDocY = key.TileY * vp.TileSizePx / vp.Zoom;

        double screenX = (tileDocX - vp.ScrollOffsetXPts) * vp.Zoom;
        double screenY = (tileDocY - vp.ScrollOffsetYPts) * vp.Zoom;

        return new Rect(screenX, screenY, vp.TileSizePx, vp.TileSizePx);
    }
}
