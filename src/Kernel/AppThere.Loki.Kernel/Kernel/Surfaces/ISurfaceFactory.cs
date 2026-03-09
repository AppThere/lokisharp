// LAYER:   AppThere.Loki.Kernel — Kernel
// KIND:    Interface
// PURPOSE: Defines the contract for creating render surfaces.
//          Intentionally contains no Skia or UI types so Kernel stays
//          dependency-free. Concrete tile-surface creation lives in
//          AppThere.Loki.Avalonia.Surfaces.AvaloniaSurfaceFactory.
// DEPENDS: —
// USED BY: AvaloniaSurfaceFactory
// PHASE:   4
// ADR:     ADR-010

namespace AppThere.Loki.Kernel.Surfaces;

public interface ISurfaceFactory
{
    /// <summary>Tile edge length in pixels. Tiles are always square.</summary>
    int TileSizePx { get; }
}
