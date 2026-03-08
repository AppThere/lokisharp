// LAYER:   AppThere.Loki.Writer — Layout Engine
// KIND:    Interface
// PURPOSE: Transforms a LokiDocument snapshot into PaintScene output.
//          Four-stage pipeline: measure → K-P line break → page break →
//          PaintScene construction. Uses LayoutCache for incremental
//          invalidation. Thread-safe for concurrent calls with the same
//          snapshot and different page geometries.
// DEPENDS: LokiDocument, PageStyle, LayoutCache, PaintScene
// USED BY: WriterEngine (document-scoped singleton)
// PHASE:   3
// ADR:     ADR-008

using AppThere.Loki.Skia.Scene;
using AppThere.Loki.Writer.Model;

namespace AppThere.Loki.Writer.Layout;

public interface ILayoutEngine
{
    /// <summary>
    /// Lay out the given document snapshot and return one PaintScene per page.
    /// Uses cache to skip unchanged paragraphs.
    /// Never returns an empty array — an empty document produces one blank page.
    /// </summary>
    IReadOnlyList<PaintScene> Layout(
        LokiDocument document,
        LayoutCache  cache);
}
