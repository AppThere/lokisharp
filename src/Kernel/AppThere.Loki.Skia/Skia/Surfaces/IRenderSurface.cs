// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Interface
// PURPOSE: Opaque render target produced by IRenderSurfaceFactory.
//          Callers never construct surfaces directly — always use the factory.
//          A surface may be backed by a CPU bitmap, a PDF page, or (Phase 4+)
//          a GPU texture. Callers only see an SKCanvas via ILokiPainter.
// DEPENDS: (none — SKCanvas is accessed through ILokiPainter, not here)
// USED BY: TileRenderer, PdfRenderer, ILokiPainter
// PHASE:   1
// ADR:     ADR-002

namespace AppThere.Loki.Skia.Surfaces;

public interface IRenderSurface : IDisposable
{
    /// <summary>Width of the surface in physical pixels.</summary>
    int WidthPx  { get; }
    /// <summary>Height of the surface in physical pixels.</summary>
    int HeightPx { get; }

    /// <summary>
    /// Flushes any pending draw calls and returns the surface to a consistent
    /// state. Must be called before reading pixels (CPU) or submitting a page (PDF).
    /// </summary>
    void Flush();
}
