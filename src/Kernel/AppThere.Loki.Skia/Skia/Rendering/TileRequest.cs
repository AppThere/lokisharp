// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Record
// PURPOSE: Fully describes one tile to be rendered: which part, at what zoom,
//          at which grid position, and at what physical resolution.
//          TileKey (partIndex, zoom, col, row) is the cache identity.
//          PixelSize defaults to 512 — the standard tile size.
//          DpiScale is used to compute physical pixel dimensions.
// DEPENDS: DpiScale
// USED BY: TileRenderer, tile cache (Phase 4+)
// PHASE:   1

using AppThere.Loki.Kernel.Color;

namespace AppThere.Loki.Skia.Rendering;

public sealed record TileRequest(
    int      PartIndex,
    float    ZoomLevel,    // 1.0 = 100%, 2.0 = 200%, etc.
    int      TileCol,
    int      TileRow,
    int      PixelSize  = 512,
    DpiScale DpiScale   = default)
{
    public TileKey Key => new(PartIndex, ZoomLevel, TileCol, TileRow);

    /// <summary>Factory for screen rendering at the given DPI scale.</summary>
    public static TileRequest ForScreen(int part, float zoom, int col, int row, DpiScale dpi) =>
        new(part, zoom, col, row, 512, dpi);

    /// <summary>Factory for headless/test rendering at 1× DPI.</summary>
    public static TileRequest ForHeadless(int part, float zoom, int col, int row) =>
        new(part, zoom, col, row, 512, DpiScale.Identity);
}

public readonly record struct TileKey(int PartIndex, float ZoomLevel, int TileCol, int TileRow);
