// LAYER:   AppThere.Loki.Writer — Editing
// KIND:    Interface (command history contract)
// PURPOSE: CommandHistory manages the undo/redo stacks for one document.
//          Owned by WriterEngine. Push is called after each committed
//          IEditCommand. Undo/Redo are called by WriterEngine.ExecuteAsync
//          when UndoCommand/RedoCommand arrive.
//          ICommandHistory is the internal contract; CommandHistory is
//          the sealed implementation.
// DEPENDS: IEditCommand, LokiDocument
// USED BY: WriterEngine
// PHASE:   5
// ADR:     ADR-013

using AppThere.Loki.LokiKit.Commands;
using AppThere.Loki.Writer.Model;

namespace AppThere.Loki.Writer.Editing;

public interface ICommandHistory
{
    /// <summary>Number of commands available to undo.</summary>
    int UndoDepth { get; }

    /// <summary>Number of commands available to redo.</summary>
    int RedoDepth { get; }

    /// <summary>
    /// Push a committed command onto the undo stack.
    /// Clears the redo stack. Evicts oldest entry if MaxUndoDepth is reached.
    /// </summary>
    void Push(IEditCommand command, LokiDocument stateBefore);

    /// <summary>
    /// Pop the most recent command and return it with the document state
    /// to restore. Returns null if the undo stack is empty.
    /// </summary>
    (IEditCommand Command, LokiDocument StateBefore)? PopUndo();

    /// <summary>
    /// Pop the most recent undone command for redo.
    /// Returns null if the redo stack is empty.
    /// </summary>
    (IEditCommand Command, LokiDocument StateBefore)? PopRedo();

    /// <summary>Clear both stacks (called on document close).</summary>
    void Clear();
}
