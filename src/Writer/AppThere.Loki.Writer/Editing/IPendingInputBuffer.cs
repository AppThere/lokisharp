// LAYER:   AppThere.Loki.Writer — Editing
// KIND:    Interface (pending input buffer contract)
// PURPOSE: Decouples keystroke accumulation from command commit.
//          Keystrokes are appended to the buffer and applied to a
//          pending document snapshot for immediate rendering. The buffer
//          commits to CommandHistory at word boundaries or after the
//          idle timeout. Owned by WriterEngine (one per open document).
// DEPENDS: CaretPosition, LokiDocument, ICommandHistory
// USED BY: WriterEngine
// PHASE:   5
// ADR:     ADR-014

using AppThere.Loki.Writer.Model;
using AppThere.Loki.LokiKit.Document;

namespace AppThere.Loki.Writer.Editing;

public interface IPendingInputBuffer : IAsyncDisposable
{
    /// <summary>True if the buffer contains uncommitted characters.</summary>
    bool HasPending { get; }

    /// <summary>
    /// The pending document snapshot — document state with buffered
    /// characters applied. Used by WriterEngine.GetPaintScene for
    /// immediate rendering. Equals the committed snapshot when HasPending=false.
    /// </summary>
    LokiDocument PendingSnapshot { get; }

    /// <summary>
    /// Append a character at the given position. Updates PendingSnapshot
    /// immediately. Resets the idle commit timer.
    /// Returns the new caret position after the insertion.
    /// </summary>
    CaretPosition Append(char character, CaretPosition at, LokiDocument baseDocument);

    /// <summary>
    /// Commit the buffered text to CommandHistory as a single InsertTextCommand.
    /// Called at word boundaries, on Enter, and by the idle timer.
    /// No-op if HasPending is false.
    /// </summary>
    void Commit();

    /// <summary>
    /// Discard buffered text without committing.
    /// Called when a conflicting remote edit arrives (Phase 7 hook).
    /// </summary>
    void Discard();
}
