// LAYER:   AppThere.Loki.Writer — Editing
// KIND:    Implementation (command history)
// PURPOSE: CommandHistory manages the undo/redo stacks for one document.
//          Owned by WriterEngine.
// DEPENDS: ICommandHistory, IEditCommand, LokiDocument
// USED BY: WriterEngine, PendingInputBuffer
// PHASE:   5
// ADR:     ADR-013, ADR-014

using AppThere.Loki.LokiKit.Commands;
using AppThere.Loki.Writer.Model;

namespace AppThere.Loki.Writer.Editing;

internal sealed class CommandHistory : ICommandHistory
{
    private readonly int _maxUndoDepth;
    private Stack<(IEditCommand Command, LokiDocument StateBefore)> _undoStack = new();
    private readonly Stack<(IEditCommand Command, LokiDocument StateBefore)> _redoStack = new();
    private readonly object _lock = new();

    public CommandHistory(int maxUndoDepth)
    {
        _maxUndoDepth = Math.Max(1, maxUndoDepth);
    }

    public int UndoDepth
    {
        get
        {
            lock (_lock) return _undoStack.Count;
        }
    }

    public int RedoDepth
    {
        get
        {
            lock (_lock) return _redoStack.Count;
        }
    }

    public void Push(IEditCommand command, LokiDocument stateBefore)
    {
        lock (_lock)
        {
            _redoStack.Clear();
            _undoStack.Push((command, stateBefore));
            
            if (_undoStack.Count > _maxUndoDepth)
            {
                var items = _undoStack.ToArray();
                // ToArray gives newest first. We want all items except the very last one (oldest)
                _undoStack = new Stack<(IEditCommand, LokiDocument)>(items.Take(items.Length - 1).Reverse());
            }
        }
    }

    public (IEditCommand Command, LokiDocument StateBefore)? PopUndo()
    {
        lock (_lock)
        {
            if (_undoStack.Count == 0) return null;
            
            var entry = _undoStack.Pop();
            _redoStack.Push(entry);
            return entry;
        }
    }

    public (IEditCommand Command, LokiDocument StateBefore)? PopRedo()
    {
        lock (_lock)
        {
            if (_redoStack.Count == 0) return null;
            
            var entry = _redoStack.Pop();
            _undoStack.Push(entry);
            return entry;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }
    }
}
