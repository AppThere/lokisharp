// LAYER:   AppThere.Loki.LokiKit — Hourglass Waist
// KIND:    Interface
// PURPOSE: Metadata for one part of a document (page, sheet, slide, or drawing).
//          Parts are indexed from zero. ILokiDocument.GetPart returns these.
//          Read-only — part metadata does not change during a view session
//          except when parts are inserted/removed (triggers DocumentChanged).
// DEPENDS: SizeF
// USED BY: ILokiDocument, ILokiView
// PHASE:   2

using AppThere.Loki.Kernel.Geometry;

namespace AppThere.Loki.LokiKit.Document;

public interface ILokiPart
{
    /// <summary>Zero-based index within the document.</summary>
    int Index { get; }

    /// <summary>
    /// Size of this part in logical points (1pt = 1/72 inch).
    /// For Writer: page size including margins.
    /// For Calc: sheet bounds (may be very large).
    /// For Draw/Impress: slide/canvas size.
    /// </summary>
    SizeF SizeInPoints { get; }

    /// <summary>
    /// Display name for this part.
    /// For Writer: "Page 1", "Page 2", etc.
    /// For Calc: sheet name (e.g. "Sheet1").
    /// For Impress: slide title or "Slide 1".
    /// </summary>
    string DisplayName { get; }
}
