using System.Collections.Immutable;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.LokiKit.Commands;
using AppThere.Loki.LokiKit.Document;
using AppThere.Loki.LokiKit.Host;
using AppThere.Loki.Writer.Editing;
using AppThere.Loki.Writer.Model;
using AppThere.Loki.Writer.Model.Inlines;
using AppThere.Loki.Writer.Model.Styles;
using Xunit;

namespace AppThere.Loki.Tests.Writer.Editing;

public class PendingInputBufferTests
{
    private class DummyLogger : ILokiLogger
    {
        public void Debug(string message, params object?[] args) { }
        public void Info(string message,  params object?[] args) { }
        public void Warn(string message,  params object?[] args) { }
        public void Error(string message, Exception? exception = null, params object?[] args) { }
        public bool IsDebugEnabled => false;
    }

    private static LokiDocument MakeDoc(params string[] paragraphTexts)
    {
        var blocks = ImmutableList.CreateBuilder<BlockNode>();
        foreach (var text in paragraphTexts)
        {
            var run = new RunNode(text, ParagraphStyle.Default.AsCharStyle(), null);
            var inlines = ImmutableList.Create<InlineNode>(run);
            blocks.Add(new ParagraphNode(inlines, ParagraphStyle.Default, null));
        }
        return LokiDocument.Empty with { Body = blocks.ToImmutable() };
    }

    private static CaretPosition At(int p, int r, int c) => new(p, r, c, false);

    private readonly ICommandHistory   _history;
    private readonly IDocumentMutator  _mutator;
    private readonly SessionId         _sessionId;

    public PendingInputBufferTests()
    {
        _history   = new CommandHistory(10);
        _mutator   = new DocumentMutator(new DummyLogger());
        _sessionId = SessionId.NewRandom();
    }

    [Fact]
    public void Append_SingleChar_HasPendingIsTrue()
    {
        var buffer = new PendingInputBuffer(_history, _mutator, _sessionId, 1000, new DummyLogger());
        var doc = MakeDoc("Hello");
        
        buffer.Append('!', At(0, 0, 5), doc);
        
        Assert.True(buffer.HasPending);
    }

    [Fact]
    public void Append_SingleChar_PendingSnapshotContainsChar()
    {
        var buffer = new PendingInputBuffer(_history, _mutator, _sessionId, 1000, new DummyLogger());
        var doc = MakeDoc("Hello");
        
        buffer.Append('!', At(0, 0, 5), doc);
        
        var snapshot = buffer.PendingSnapshot;
        var run = (RunNode)((ParagraphNode)snapshot.Body[0]).Inlines[0];
        Assert.Equal("Hello!", run.Text);
    }

    [Fact]
    public void Commit_AfterAppend_HasPendingIsFalse()
    {
        var buffer = new PendingInputBuffer(_history, _mutator, _sessionId, 1000, new DummyLogger());
        buffer.Append('A', At(0, 0, 0), MakeDoc(""));
        
        buffer.Commit();
        
        Assert.False(buffer.HasPending);
    }

    [Fact]
    public void Commit_PushesCommandToHistory()
    {
        var buffer = new PendingInputBuffer(_history, _mutator, _sessionId, 1000, new DummyLogger());
        buffer.Append('A', At(0, 0, 0), MakeDoc(""));
        buffer.Commit();
        
        Assert.Equal(1, _history.UndoDepth);
        var entry = _history.PopUndo()!.Value;
        Assert.IsType<InsertTextCommand>(entry.Command);
        var cmd = (InsertTextCommand)entry.Command;
        Assert.Equal("A", cmd.Text);
    }

    [Fact]
    public void Commit_Empty_NoopDoesNotThrow()
    {
        var buffer = new PendingInputBuffer(_history, _mutator, _sessionId, 1000, new DummyLogger());
        buffer.Commit(); // Should not throw
        Assert.Equal(0, _history.UndoDepth);
    }

    [Fact]
    public void Discard_AfterAppend_HasPendingIsFalse()
    {
        var buffer = new PendingInputBuffer(_history, _mutator, _sessionId, 1000, new DummyLogger());
        buffer.Append('A', At(0, 0, 0), MakeDoc(""));
        
        buffer.Discard();
        
        Assert.False(buffer.HasPending);
        Assert.Equal(0, _history.UndoDepth); // Does not push
    }

    [Fact]
    public void Discard_DoesNotPushToHistory()
    {
        var buffer = new PendingInputBuffer(_history, _mutator, _sessionId, 1000, new DummyLogger());
        buffer.Append('A', At(0, 0, 0), MakeDoc(""));
        buffer.Discard();
        
        Assert.Equal(0, _history.UndoDepth);
    }

    [Fact]
    public void IdleCommit_ZeroMs_CommitsImmediately()
    {
        // 0ms idle time = synchronous commit
        var buffer = new PendingInputBuffer(_history, _mutator, _sessionId, 0, new DummyLogger());
        
        buffer.Append('X', At(0, 0, 0), MakeDoc(""));
        
        Assert.False(buffer.HasPending);
        Assert.Equal(1, _history.UndoDepth);
        
        var entry = _history.PopUndo()!.Value;
        Assert.Equal("X", ((InsertTextCommand)entry.Command).Text);
    }
}
