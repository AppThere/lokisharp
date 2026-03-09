// LAYER:   AppThere.Loki.Writer — Editing
// KIND:    Interface (document mutation contract)
// PURPOSE: Applies IEditCommand operations to a LokiDocument snapshot,
//          returning a new immutable snapshot. Stateless — takes a
//          document in, returns a document out. Used by both WriterEngine
//          (for committed commands) and PendingInputBuffer (for pending
//          character insertion preview).
//          Separated from WriterEngine so mutation logic is independently
//          testable without a full engine stack.
// DEPENDS: LokiDocument, IEditCommand, CaretPosition
// USED BY: WriterEngine, PendingInputBuffer
// PHASE:   5
// ADR:     ADR-013

using AppThere.Loki.LokiKit.Commands;
using AppThere.Loki.Writer.Model;
using AppThere.Loki.LokiKit.Document;

namespace AppThere.Loki.Writer.Editing;

public interface IDocumentMutator
{
    /// <summary>
    /// Apply an edit command to the given document snapshot.
    /// Returns a new immutable LokiDocument with the change applied.
    /// The input document is never modified.
    /// Throws InvalidOperationException if the command cannot be applied
    /// (e.g. CaretPosition out of range).
    /// </summary>
    LokiDocument Apply(LokiDocument document, IEditCommand command);

    /// <summary>
    /// Apply a single character insertion at a position without creating
    /// a full InsertTextCommand. Used by PendingInputBuffer for per-keystroke
    /// preview rendering.
    /// Returns (newDocument, newCaretPosition).
    /// </summary>
    (LokiDocument Document, CaretPosition NewCaret)
        InsertChar(LokiDocument document, char character, CaretPosition at);
}
