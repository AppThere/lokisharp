using AppThere.Loki.LokiKit.Commands;
using AppThere.Loki.LokiKit.Document;
using AppThere.Loki.LokiKit.Host;
using AppThere.Loki.Writer.Editing;
using AppThere.Loki.Writer.Model;
using Xunit;

namespace AppThere.Loki.Tests.Writer.Editing;

public class CommandHistoryTests
{
    private readonly LokiDocument _emptyDoc = LokiDocument.Empty;
    private readonly IEditCommand _cmd = new InsertTextCommand(SessionId.NewRandom(), DocumentVersion.Zero, new CaretPosition(0, 0, 0, false), "A");
    private readonly IEditCommand _cmd2 = new InsertTextCommand(SessionId.NewRandom(), DocumentVersion.Zero, new CaretPosition(0, 0, 1, false), "B");

    [Fact]
    public void Push_SingleCommand_UndoDepthIsOne()
    {
        var history = new CommandHistory(10);
        history.Push(_cmd, _emptyDoc);
        Assert.Equal(1, history.UndoDepth);
    }

    [Fact]
    public void Push_TwoCommands_UndoDepthIsTwo()
    {
        var history = new CommandHistory(10);
        history.Push(_cmd, _emptyDoc);
        history.Push(_cmd2, _emptyDoc);
        Assert.Equal(2, history.UndoDepth);
    }

    [Fact]
    public void Push_ClearsRedoStack()
    {
        var history = new CommandHistory(10);
        history.Push(_cmd, _emptyDoc);
        history.PopUndo();
        Assert.Equal(1, history.RedoDepth);
        
        history.Push(_cmd2, _emptyDoc);
        Assert.Equal(0, history.RedoDepth);
    }

    [Fact]
    public void PopUndo_EmptyStack_ReturnsNull()
    {
        var history = new CommandHistory(10);
        Assert.Null(history.PopUndo());
    }

    [Fact]
    public void PopUndo_ReturnsLastPushed()
    {
        var history = new CommandHistory(10);
        history.Push(_cmd, _emptyDoc);
        history.Push(_cmd2, _emptyDoc);
        
        var popped = history.PopUndo();
        Assert.NotNull(popped);
        Assert.Same(_cmd2, popped.Value.Command);
        Assert.Equal(1, history.UndoDepth);
    }

    [Fact]
    public void PopUndo_MovesToRedoStack()
    {
        var history = new CommandHistory(10);
        history.Push(_cmd, _emptyDoc);
        history.PopUndo();
        Assert.Equal(1, history.RedoDepth);
    }

    [Fact]
    public void PopRedo_EmptyStack_ReturnsNull()
    {
        var history = new CommandHistory(10);
        Assert.Null(history.PopRedo());
    }

    [Fact]
    public void PopRedo_ReturnsLastUndone()
    {
        var history = new CommandHistory(10);
        history.Push(_cmd, _emptyDoc);
        history.PopUndo();
        
        var popped = history.PopRedo();
        Assert.NotNull(popped);
        Assert.Same(_cmd, popped.Value.Command);
        Assert.Equal(1, history.UndoDepth);
        Assert.Equal(0, history.RedoDepth);
    }

    [Fact]
    public void MaxUndoDepth_ExceedCap_OldestEvicted()
    {
        int maxDepth = 3;
        var history = new CommandHistory(maxDepth);
        
        var cmds = new[]
        {
            new InsertTextCommand(SessionId.NewRandom(), DocumentVersion.Zero, new CaretPosition(0, 0, 0, false), "1"),
            new InsertTextCommand(SessionId.NewRandom(), DocumentVersion.Zero, new CaretPosition(0, 0, 0, false), "2"),
            new InsertTextCommand(SessionId.NewRandom(), DocumentVersion.Zero, new CaretPosition(0, 0, 0, false), "3"),
            new InsertTextCommand(SessionId.NewRandom(), DocumentVersion.Zero, new CaretPosition(0, 0, 0, false), "4")
        };

        foreach (var c in cmds)
        {
            history.Push(c, _emptyDoc);
        }

        Assert.Equal(3, history.UndoDepth);
        
        // Oldest ("1") should be gone.
        var pop1 = history.PopUndo();
        Assert.Same(cmds[3], pop1!.Value.Command);
        
        var pop2 = history.PopUndo();
        Assert.Same(cmds[2], pop2!.Value.Command);
        
        var pop3 = history.PopUndo();
        Assert.Same(cmds[1], pop3!.Value.Command);
        
        Assert.Null(history.PopUndo());
    }

    [Fact]
    public void Clear_EmptiesBothStacks()
    {
        var history = new CommandHistory(10);
        history.Push(_cmd, _emptyDoc);
        history.Push(_cmd2, _emptyDoc);
        history.PopUndo();
        
        history.Clear();
        
        Assert.Equal(0, history.UndoDepth);
        Assert.Equal(0, history.RedoDepth);
    }
}
