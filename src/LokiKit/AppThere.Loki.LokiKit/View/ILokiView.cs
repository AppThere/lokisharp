// LAYER:   AppThere.Loki.LokiKit — Hourglass Waist
// KIND:    Interface
// PURPOSE: Rendering viewport onto one ILokiDocument. Created by ILokiHost.CreateView.
//          A document may have multiple views (e.g. split panels).
//          Each view owns its own tile cache and scroll/zoom state.
//          Fires TileInvalidated when the document changes and tiles must
//          be re-requested by the UI.
// DEPENDS: ILokiDocument, TileRequest, PdfMetadata, TileInvalidatedEventArgs, SKBitmap
// USED BY: lokiprint CLI, Avalonia tile control (Phase 4+), rendering tests
// PHASE:   2
// ADR:     ADR-005

using AppThere.Loki.Kernel.Geometry;
using AppThere.Loki.LokiKit.Document;
using AppThere.Loki.LokiKit.Events;
using AppThere.Loki.Skia.Rendering;
using AppThere.Loki.Skia.Surfaces;
using SkiaSharp;

namespace AppThere.Loki.LokiKit.View;

public interface ILokiView : IDisposable
{
    // ── Document binding ──────────────────────────────────────────────────────

    /// <summary>The document this view is bound to.</summary>
    ILokiDocument Document { get; }

    // ── View state ────────────────────────────────────────────────────────────

    /// <summary>
    /// Zero-based index of the part currently in view.
    /// Setting this fires TileInvalidated for all cached tiles.
    /// </summary>
    int ActivePart { get; set; }

    /// <summary>
    /// Zoom level. 1.0 = 100% (1pt = 1px). Must be > 0.
    /// Setting this fires TileInvalidated for all cached tiles.
    /// </summary>
    float Zoom { get; set; }

    /// <summary>
    /// Scroll position in logical points from the top-left of the active part.
    /// Setting this does NOT fire TileInvalidated — the UI adjusts which
    /// tiles it requests rather than invalidating the cache.
    /// </summary>
    PointF ScrollOffset { get; set; }

    // ── Tile rendering ────────────────────────────────────────────────────────

    /// <summary>
    /// Renders one tile and returns an SKBitmap the caller owns.
    /// Internally delegates to ITileRenderer.RenderTileAsync.
    /// Results are not cached by the view — the UI tile cache sits above this.
    /// Never returns null. Throws TileRenderException on failure.
    /// Thread-safe: multiple tiles can be in-flight concurrently.
    /// </summary>
    Task<SKBitmap> RenderTileAsync(
        TileRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Renders all parts of the document to a PDF stream.
    /// Convenience wrapper over ITileRenderer.RenderToPdfAsync.
    /// The view assembles the PaintScene list from all document parts.
    /// </summary>
    Task RenderToPdfAsync(
        Stream output,
        PdfMetadata meta,
        CancellationToken ct = default);

    // ── Layout queries ────────────────────────────────────────────────────────

    /// <summary>
    /// Size of the given part in logical points.
    /// Shortcut for Document.GetPart(partIndex).SizeInPoints.
    /// </summary>
    SizeF GetPartSize(int partIndex);

    // ── Invalidation ──────────────────────────────────────────────────────────

    /// <summary>
    /// Fired when one or more tiles are stale and must be re-requested.
    /// Raised on the thread that triggered the document change.
    /// The UI tile cache should evict the listed TileKeys and schedule
    /// new RenderTileAsync calls.
    /// </summary>
    event EventHandler<TileInvalidatedEventArgs> TileInvalidated;
}
