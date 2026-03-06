// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Record
// PURPOSE: A raster image placed in the scene. Pixel data is not embedded here —
//          the ImageRef is a content-addressed handle resolved via IImageStore.
// DEPENDS: PaintNode, ImageRef, ImageFit, RectF
// USED BY: PaintBand, TileRenderer
// PHASE:   1
// ADR:     ADR-003

using AppThere.Loki.Kernel.Geometry;

namespace AppThere.Loki.Skia.Scene.Nodes;

public sealed record ImageNode(
    RectF    Bounds,
    ImageRef Image,
    float    Opacity = 1f,
    ImageFit Fit     = ImageFit.Contain) : PaintNode(Bounds);
