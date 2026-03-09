// LAYER:   AppThere.Loki.Avalonia — Tile Cache
// KIND:    Interface
// PURPOSE: Viewport-aware tile cache contract. Owned by LokiTileControl
//          (one instance per open document view). Manages the full lifecycle:
//          schedule renders, serve cached bitmaps, deduplicate in-flight
//          requests, and evict cold tiles within a memory budget.
//          All public methods are called on the UI thread unless noted.
// DEPENDS: TileKey, TileCacheOptions, ILokiView
// USED BY: LokiTileControl, LokiCompositionDrawOp
// PHASE:   4
// ADR:     ADR-011

using Avalonia.Media.Imaging;
using AppThere.Loki.Avalonia.Controls;
using AppThere.Loki.LokiKit.Events;

namespace AppThere.Loki.Avalonia.Cache;

public interface ILokiTileCache : IAsyncDisposable
{
    /// <summary>
    /// Notify the cache of the current viewport geometry.
    /// Called on every scroll or resize event.
    /// Triggers zone reclassification, eviction of Cold tiles,
    /// pre-render scheduling for Warm zone misses, and cancellation
    /// of in-flight renders for newly Cold tiles.
    /// </summary>
    void UpdateViewport(ViewportGeometry viewport);

    /// <summary>
    /// Return the cached bitmap for the given tile key, or null if not
    /// yet rendered. A null return means the control should draw a
    /// placeholder (white rectangle) and the cache will call
    /// TileReady when the render completes.
    /// </summary>
    WriteableBitmap? TryGetTile(TileKey key);

    /// <summary>
    /// Invalidate all tiles in the given region. Called in response to
    /// ILokiView.TileInvalidated. Hot zone tiles are re-rendered
    /// immediately; Warm zone tiles after InvalidationDebouncedMs.
    /// </summary>
    void Invalidate(TileInvalidatedEventArgs args);

    /// <summary>
    /// Invalidate all tiles for all parts at all zoom levels.
    /// Called on zoom change.
    /// </summary>
    void InvalidateAll();

    /// <summary>
    /// Fired on the UI thread when a tile render completes and the bitmap
    /// is available. The control subscribes to this event to call
    /// InvalidateVisual().
    /// </summary>
    event EventHandler<TileKey> TileReady;

    /// <summary>Current number of tiles in the completed cache.</summary>
    int CachedTileCount { get; }

    /// <summary>Current total memory used by completed tiles in bytes.</summary>
    long CachedMemoryBytes { get; }
}
