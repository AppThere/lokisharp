// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Implementation
// PURPOSE: Skia-layer typed exceptions. All extend LokiException.
//          TileRenderException: caught by TileRenderer to degrade to CPU fallback.
//          GpuContextLostException: triggers GPU context reset in Phase 4.
//          PdfRenderException: propagates to the lokiprint CLI top-level handler.
//          See ADR-004 for the full exception hierarchy and catch boundaries.
// DEPENDS: LokiException
// USED BY: TileRenderer, PdfRenderer, GpuRenderSurface (Phase 4)
// PHASE:   1
// ADR:     ADR-004

using AppThere.Loki.Kernel.Errors;

namespace AppThere.Loki.Skia.Errors;

public sealed class TileRenderException : LokiException
{
    public int  PartIndex { get; }
    public int  TileCol   { get; }
    public int  TileRow   { get; }

    public TileRenderException(int part, int col, int row, string message, Exception? inner = null)
        : base(message, inner) { PartIndex = part; TileCol = col; TileRow = row; }
}

public sealed class GpuContextLostException : LokiException
{
    public GpuContextLostException(string message, Exception? inner = null)
        : base(message, inner) { }
}

public sealed class PdfRenderException : LokiException
{
    public int? PageIndex { get; }
    public PdfRenderException(string message, int? pageIndex = null, Exception? inner = null)
        : base(message, inner) => PageIndex = pageIndex;
}
