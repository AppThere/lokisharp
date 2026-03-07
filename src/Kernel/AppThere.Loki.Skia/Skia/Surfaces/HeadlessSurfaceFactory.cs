// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Implementation
// PURPOSE: Implements IRenderSurfaceFactory for headless/CLI rendering (Phase 1).
//          Always returns CPU surfaces — IsGpuAvailable is always false.
//          CreateTileSurface and CreateHeadlessBitmapSurface both produce
//          BitmapRenderSurface instances. CreatePdfSurface produces PdfRenderSurface.
//          GPU context parameter is accepted but ignored in Phase 1.
// DEPENDS: IRenderSurfaceFactory, BitmapRenderSurface, PdfRenderSurface, ILokiLogger
// USED BY: TileRenderer, PdfRenderer — injected via DI
// PHASE:   1
// ADR:     ADR-002

using AppThere.Loki.Kernel.Geometry;
using AppThere.Loki.Kernel.Logging;

namespace AppThere.Loki.Skia.Surfaces;

public sealed class HeadlessSurfaceFactory : IRenderSurfaceFactory
{
    private readonly ILokiLogger _logger;

    public HeadlessSurfaceFactory(ILokiLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool          IsGpuAvailable   => false;
    public RenderBackend PreferredBackend => RenderBackend.Cpu;

    public IRenderSurface CreateTileSurface(SizeF sizeInPixels, RenderContext? gpuContext = null)
    {
        ValidateSize(sizeInPixels, nameof(sizeInPixels));
        _logger.Debug("CreateTileSurface {0}×{1} (CPU).", (int)sizeInPixels.Width, (int)sizeInPixels.Height);
        return new BitmapRenderSurface((int)sizeInPixels.Width, (int)sizeInPixels.Height);
    }

    public IRenderSurface CreateHeadlessBitmapSurface(SizeF sizeInPixels)
    {
        ValidateSize(sizeInPixels, nameof(sizeInPixels));
        _logger.Debug("CreateHeadlessBitmapSurface {0}×{1}.", (int)sizeInPixels.Width, (int)sizeInPixels.Height);
        return new BitmapRenderSurface((int)sizeInPixels.Width, (int)sizeInPixels.Height);
    }

    public IRenderSurface CreatePdfSurface(Stream output, SizeF pageSizeInPoints, PdfMetadata meta)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(meta);
        _logger.Debug("CreatePdfSurface {0}×{1} pt.", pageSizeInPoints.Width, pageSizeInPoints.Height);
        return new PdfRenderSurface(output, pageSizeInPoints, meta);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void ValidateSize(SizeF size, string paramName)
    {
        if (size.Width  <= 0f)
            throw new ArgumentOutOfRangeException(paramName, "Width must be > 0.");
        if (size.Height <= 0f)
            throw new ArgumentOutOfRangeException(paramName, "Height must be > 0.");
    }
}
