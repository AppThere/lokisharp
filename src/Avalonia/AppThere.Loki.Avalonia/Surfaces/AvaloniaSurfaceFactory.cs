// LAYER:   AppThere.Loki.Avalonia — Surfaces
// KIND:    Class (ISurfaceFactory implementation)
// PURPOSE: Avalonia-aware ISurfaceFactory. Creates CPU-backed SKSurfaces
//          for tile rendering on the thread pool (Phase 4). GPU-backed
//          surfaces are reserved for Phase 6 behind a feature flag.
//          Registered as singleton via UseAvaloniaSurfaces().
// DEPENDS: ISurfaceFactory (Kernel), TileCacheOptions
// USED BY: LokiHostBuilderExtensions.UseAvaloniaSurfaces, WriterEngine
// PHASE:   4
// ADR:     ADR-010

using SkiaSharp;
using AppThere.Loki.Kernel.Surfaces;

namespace AppThere.Loki.Avalonia.Surfaces;

public sealed class AvaloniaSurfaceFactory : ISurfaceFactory
{
    private readonly TileCacheOptions _options;

    public AvaloniaSurfaceFactory(TileCacheOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Creates a CPU-backed SKSurface for tile rendering.
    /// Safe to call on any thread — no GPU context required.
    /// </summary>
    public SKSurface CreateTileSurface(int widthPx, int heightPx)
    {
        var info = new SKImageInfo(widthPx, heightPx,
            SKColorType.Rgba8888, SKAlphaType.Premul);
        return SKSurface.Create(info)
            ?? throw new LokiSurfaceException(
                $"Failed to create {widthPx}×{heightPx} CPU tile surface.");
    }

    /// <summary>Tile size from options. Used by the tile grid math.</summary>
    public int TileSizePx => _options.TileSizePx;
}
