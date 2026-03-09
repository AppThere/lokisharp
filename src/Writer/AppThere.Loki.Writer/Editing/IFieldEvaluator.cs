// LAYER:   AppThere.Loki.Writer — Editing
// KIND:    Interface + record (Phase 6 field evaluation hook)
// PURPOSE: Defined in Phase 5 but not implemented. InlineMeasurer uses
//          FieldNode.StaticText directly in Phase 5. Phase 6 injects an
//          IFieldEvaluator implementation that computes live values for
//          page numbers, dates, and metadata fields.
//          Placing the interface here now prevents a breaking API change
//          when Phase 6 wires it in.
// DEPENDS: FieldNode, FieldKind
// USED BY: InlineMeasurer (Phase 6), LayoutEngine (Phase 6)
// PHASE:   5 (defined), 6 (implemented)
// ADR:     ADR-016

using AppThere.Loki.Writer.Model.Inlines;

namespace AppThere.Loki.Writer.Editing;

/// <summary>
/// Context provided to IFieldEvaluator at layout time.
/// Phase 6: populated by LayoutEngine from the current page state.
/// </summary>
public sealed record FieldContext(
    int      PageNumber,
    int      PageCount,
    string?  DocumentTitle,
    string?  DocumentAuthor,
    DateTime Now);

/// <summary>
/// Computes the display string for a FieldNode.
/// Phase 5: not used — InlineMeasurer reads FieldNode.StaticText directly.
/// Phase 6: injected into InlineMeasurer; replaces static text with
/// live-computed values per FieldKind.
/// </summary>
public interface IFieldEvaluator
{
    string Evaluate(FieldNode field, FieldContext context);
}

/// <summary>
/// Phase 5 no-op implementation. Returns StaticText unchanged.
/// Registered in DI so InlineMeasurer can accept IFieldEvaluator
/// without requiring Phase 6 to be implemented first.
/// </summary>
public sealed class StaticFieldEvaluator : IFieldEvaluator
{
    public string Evaluate(FieldNode field, FieldContext context)
        => field.StaticText;
}
