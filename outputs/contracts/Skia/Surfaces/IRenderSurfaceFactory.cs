// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Interface
// PURPOSE: Creates platform-appropriate render surfaces. This is the single
//          point of platform branching in the rendering stack.
//          All callers use this factory; none construct surfaces directly.
//          Phase 1 implementation: HeadlessSurfaceFactory (CPU only).
//          Phase 4 implementation: AvaloniaSurfaceFactory (GPU + CPU fallback).
// DEPENDS: IRenderSurface, RenderContext, SizeF, PdfMetadata
// USED BY: TileRenderer, PdfRenderer — injected via DI
// PHASE:   1
// ADR:     ADR-002

using AppThere.Loki.Kernel.Geometry;

namespace AppThere.Loki.Skia.Surfaces;

public interface IRenderSurfaceFactory
{
    /// <summary>
    /// Creates a surface for on-screen tile rendering.
    /// Phase 1: always returns a CPU bitmap surface (gpuContext is ignored).
    /// Phase 4: returns GPU surface when gpuContext is non-null; CPU fallback otherwise.
    /// </summary>
    IRenderSurface CreateTileSurface(SizeF sizeInPixels, RenderContext? gpuContext = null);

    /// <summary>Creates a CPU bitmap surface for headless/CLI rendering. Always CPU.</summary>
    IRenderSurface CreateHeadlessBitmapSurface(SizeF sizeInPixels);

    /// <summary>Creates a PDF page surface backed by SKDocument.</summary>
    IRenderSurface CreatePdfSurface(Stream output, SizeF pageSizeInPoints, PdfMetadata meta);

    bool          IsGpuAvailable    { get; }
    RenderBackend PreferredBackend  { get; }
}

public enum RenderBackend { Cpu, OpenGl, Metal, Vulkan }

/// <summary>
/// Opaque platform GPU context passed from the Avalonia host to the factory.
/// Phase 1: always null. Phase 4: populated by the Avalonia GL/Metal control.
/// </summary>
public sealed class RenderContext
{
    public RenderBackend Backend      { get; init; }
    public nint          NativeHandle { get; init; }
    public int           SampleCount  { get; init; }
}

public sealed record PdfMetadata(
    string Title    = "",
    string Author   = "",
    string Creator  = "AppThere Loki");
