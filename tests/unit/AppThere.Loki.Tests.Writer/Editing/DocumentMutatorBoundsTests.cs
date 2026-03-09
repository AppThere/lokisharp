using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.LokiKit.Commands;
using AppThere.Loki.LokiKit.Document;
using AppThere.Loki.LokiKit.Host;
using AppThere.Loki.Writer.Editing;
using AppThere.Loki.Writer.Model;
using AppThere.Loki.Writer.Model.Inlines;
using AppThere.Loki.Writer.Model.Styles;
using System.Collections.Immutable;
using Xunit;

namespace AppThere.Loki.Tests.Writer.Editing;

public class DocumentMutatorBoundsTests
{
    private readonly DocumentMutator _mutator = new(NullLokiLogger.Instance);
    private readonly SessionId _sessionId = SessionId.NewRandom();
    private readonly DocumentVersion _version = DocumentVersion.Zero;

    private LokiDocument MakeDoc(string text)
    {
        var run = new RunNode(text, ParagraphStyle.Default.AsCharStyle(), null);
        var para = new ParagraphNode(ImmutableList.Create<InlineNode>(run), ParagraphStyle.Default, null);
        return LokiDocument.Empty with { Body = ImmutableList.Create<BlockNode>(para) };
    }

    [Fact]
    public void Insert_ParagraphIndexOutOfRange_ClampsToLastParagraph()
    {
        var doc = MakeDoc("Hello");
        var at = new CaretPosition(99, 0, 0, false);
        var cmd = new InsertTextCommand(_sessionId, _version, at, "!");
        
        var result = _mutator.Apply(doc, cmd);
        
        var para = result.Body[0] as ParagraphNode;
        Assert.NotNull(para);
        var run = para.Inlines[0] as RunNode;
        Assert.NotNull(run);
        Assert.Equal("!Hello", run.Text);
    }

    [Fact]
    public void Insert_CharOffsetPastEnd_ClampsToRunEnd()
    {
        var doc = MakeDoc("Hi");
        var at = new CaretPosition(0, 0, 99, false);
        var cmd = new InsertTextCommand(_sessionId, _version, at, "X");
        
        var result = _mutator.Apply(doc, cmd);
        
        var para = result.Body[0] as ParagraphNode;
        Assert.NotNull(para);
        var run = para.Inlines[0] as RunNode;
        Assert.NotNull(run);
        Assert.Equal("HiX", run.Text);
    }

    [Fact]
    public void Insert_EmptyDocument_CreatesContent()
    {
        var emptyPara = new ParagraphNode(ImmutableList<InlineNode>.Empty, ParagraphStyle.Default, null);
        var doc = LokiDocument.Empty with { Body = ImmutableList.Create<BlockNode>(emptyPara) };
        var at = new CaretPosition(0, 0, 0, false);
        var cmd = new InsertTextCommand(_sessionId, _version, at, "A");
        
        var result = _mutator.Apply(doc, cmd);
        
        var para = result.Body[0] as ParagraphNode;
        Assert.NotNull(para);
        var run = para.Inlines[0] as RunNode;
        Assert.NotNull(run);
        Assert.Equal("A", run.Text);
    }
}
