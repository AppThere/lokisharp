// LAYER:   AppThere.Loki.Avalonia — Controls
// KIND:    Record (value type geometry snapshot)
// PURPOSE: Immutable snapshot of the current viewport state passed to
//          ILokiTileCache.UpdateViewport on every scroll/resize.
//          All measurements in document points (not pixels) to match
//          ILokiView coordinate space. The control converts from Avalonia
//          device-independent pixels to points using the current zoom.
// DEPENDS: —
// USED BY: ILokiTileCache, LokiTileControl
// PHASE:   4
// ADR:     ADR-011

namespace AppThere.Loki.Avalonia.Controls;

public sealed record ViewportGeometry(
    int   PartIndex,       // which document part is primary in view
    float ViewportWidthPts,
    float ViewportHeightPts,
    float ScrollOffsetXPts,
    float ScrollOffsetYPts,
    float Zoom,
    int   TileSizePx);     // from TileCacheOptions — needed for tile grid math
