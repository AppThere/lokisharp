// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Interface
// PURPOSE: Renders tiles from a PaintScene. The primary consumer of PaintScene
//          in Phase 1. RenderTile returns an SKBitmap; caller owns the bitmap.
//          RenderToPdf renders all Parts of a scene to a PDF stream.
//          All methods are async to support future GPU dispatch (Phase 4+).
//          Phase 1 implementation: TileRenderer (CPU-only).
// DEPENDS: PaintScene, TileRequest, IRenderSurfaceFactory, ILokiPainter, IImageStore
// USED BY: lokiprint CLI, rendering tests, UI tile consumer (Phase 4+)
// PHASE:   1

using AppThere.Loki.Skia.Scene;
using AppThere.Loki.Skia.Surfaces;
using SkiaSharp;

namespace AppThere.Loki.Skia.Rendering;

public interface ITileRenderer
{
    /// <summary>
    /// Renders one tile from the given scene. Returns an SKBitmap the caller owns.
    /// Never returns null — throws TileRenderException on failure.
    /// Thread-safe for CPU surfaces: multiple tiles can render concurrently.
    /// </summary>
    Task<SKBitmap> RenderTileAsync(PaintScene scene, TileRequest request,
                                    CancellationToken ct = default);

    /// <summary>
    /// Renders all Parts in the scenes array to a single PDF stream.
    /// Scenes are rendered sequentially; pages appear in scenes order.
    /// </summary>
    Task RenderToPdfAsync(IReadOnlyList<PaintScene> scenes, Stream output,
                          PdfMetadata meta, CancellationToken ct = default);
}
