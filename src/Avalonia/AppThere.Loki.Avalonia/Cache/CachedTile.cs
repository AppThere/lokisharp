// LAYER:   AppThere.Loki.Avalonia — Tile Cache
// KIND:    Records and enum
// PURPOSE: Internal tile cache entry and zone classification.
//          CachedTile holds the rendered bitmap and access metadata.
//          TileZone is the eviction classification (Hot/Warm/Cool/Cold).
// DEPENDS: TileKey
// USED BY: LokiTileCache
// PHASE:   4
// ADR:     ADR-011

using Avalonia.Media.Imaging;

namespace AppThere.Loki.Avalonia.Cache;

/// <summary>Zone classification for viewport-aware eviction.</summary>
public enum TileZone
{
    /// <summary>Currently visible. Never evicted.</summary>
    Hot,
    /// <summary>Within KeepRadiusMultiplier of viewport. Pre-rendered, evicted last.</summary>
    Warm,
    /// <summary>Within RetainRadiusMultiplier of viewport. Retained if memory permits.</summary>
    Cool,
    /// <summary>Beyond RetainRadiusMultiplier. Evicted on next maintenance cycle.</summary>
    Cold,
}

/// <summary>
/// A completed tile entry in the cache.
/// Bitmap is an Avalonia WriteableBitmap — drawn directly by the render callback.
/// ByteCost is pre-computed as TileSizePx × TileSizePx × 4.
/// </summary>
internal sealed class CachedTile
{
    public TileKey         Key          { get; }
    public WriteableBitmap Bitmap       { get; }
    public long            ByteCost     { get; }
    public DateTime        LastAccessed { get; set; }
    public TileZone        Zone         { get; set; }

    public CachedTile(TileKey key, WriteableBitmap bitmap, long byteCost)
    {
        Key          = key;
        Bitmap       = bitmap;
        ByteCost     = byteCost;
        LastAccessed = DateTime.UtcNow;
        Zone         = TileZone.Cold;
    }
}
