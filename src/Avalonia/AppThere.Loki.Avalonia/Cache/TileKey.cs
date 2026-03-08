// LAYER:   AppThere.Loki.Avalonia — Tile Cache
// KIND:    Record struct (value type cache key)
// PURPOSE: Uniquely identifies a rendered tile. Used as Dictionary key in
//          LokiTileCache — must be a readonly record struct for efficient
//          hashing with no heap allocation.
//          Zoom is included because a tile rendered at one zoom level is
//          not valid at another.
// DEPENDS: —
// USED BY: LokiTileCache, LokiTileControl
// PHASE:   4
// ADR:     ADR-011

namespace AppThere.Loki.Avalonia.Cache;

/// <summary>
/// Uniquely identifies a rendered tile within a document view.
/// </summary>
public readonly record struct TileKey(
    int   PartIndex,   // which document part (page)
    int   TileX,       // column in the tile grid (0-based)
    int   TileY,       // row in the tile grid (0-based)
    float Zoom);       // zoom level at render time
