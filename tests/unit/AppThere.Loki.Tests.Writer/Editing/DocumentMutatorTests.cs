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

public class DocumentMutatorTests
{
    private class DummyLogger : ILokiLogger
    {
        public void Debug(string message, params object?[] args) { }
        public void Info(string message,  params object?[] args) { }
        public void Warn(string message,  params object?[] args) { }
        public void Error(string message, Exception? exception = null, params object?[] args) { }
        public bool IsDebugEnabled => false;
    }

    private static CaretPosition At(int p, int r, int c) => new(p, r, c, false);
    
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

    private readonly IDocumentMutator _mutator;

    public DocumentMutatorTests()
    {
        _mutator = new DocumentMutator(new DummyLogger());
    }

    [Fact]
    public void Insert_SingleChar_AppendsToRun()
    {
        var doc = MakeDoc("Hello");
        var cmd = new InsertTextCommand(SessionId.NewRandom(), DocumentVersion.Zero, At(0, 0, 5), "!");
        
        var newDoc = _mutator.Apply(doc, cmd);
        
        var run = (RunNode)((ParagraphNode)newDoc.Body[0]).Inlines[0];
        Assert.Equal("Hello!", run.Text);
    }

    [Fact]
    public void Insert_AtStart_PrependsToRun()
    {
        var doc = MakeDoc("Hello");
        var cmd = new InsertTextCommand(SessionId.NewRandom(), DocumentVersion.Zero, At(0, 0, 0), "X");
        
        var newDoc = _mutator.Apply(doc, cmd);
        
        var run = (RunNode)((ParagraphNode)newDoc.Body[0]).Inlines[0];
        Assert.Equal("XHello", run.Text);
    }

    [Fact]
    public void Insert_EmptyParagraph_CreatesRun()
    {
        var doc = MakeDoc("");
        // A truly empty paragraph in standard Loki model might have 0 inlines.
        // MakeDoc("") adds an empty RunNode which is the correct internal state
        // to receive input at RunIndex 0, or we can test explicitly with 0 inlines.
        var emptyPara = new ParagraphNode(ImmutableList<InlineNode>.Empty, ParagraphStyle.Default, null);
        var trueEmptyDoc = LokiDocument.Empty with { Body = ImmutableList.Create<BlockNode>(emptyPara) };

        var cmd = new InsertTextCommand(SessionId.NewRandom(), DocumentVersion.Zero, At(0, 0, 0), "A");
        
        var newDoc = _mutator.Apply(trueEmptyDoc, cmd);
        
        var run = (RunNode)((ParagraphNode)newDoc.Body[0]).Inlines[0];
        Assert.Equal("A", run.Text);
    }



    [Fact]
    public void Delete_MiddleChars_RemovesCorrectly()
    {
        var doc = MakeDoc("Hello");
        var cmd = new DeleteTextCommand(SessionId.NewRandom(), DocumentVersion.Zero, At(0, 0, 1), 3, "ell");
        
        var newDoc = _mutator.Apply(doc, cmd);
        var run = (RunNode)((ParagraphNode)newDoc.Body[0]).Inlines[0];
        Assert.Equal("Ho", run.Text);
    }

    [Fact]
    public void Delete_EntireRun_LeavesEmptyRun()
    {
        var doc = MakeDoc("Hi");
        var cmd = new DeleteTextCommand(SessionId.NewRandom(), DocumentVersion.Zero, At(0, 0, 0), 2, "Hi");
        
        var newDoc = _mutator.Apply(doc, cmd);
        var inlines = ((ParagraphNode)newDoc.Body[0]).Inlines;
        Assert.Single(inlines);
        var run = (RunNode)inlines[0];
        Assert.Equal("", run.Text);
    }



    [Fact]
    public void Split_MiddleOfRun_ProducesTwoParagraphs()
    {
        var doc = MakeDoc("Hello World");
        var cmd = new SplitParagraphCommand(SessionId.NewRandom(), DocumentVersion.Zero, At(0, 0, 5));
        
        var newDoc = _mutator.Apply(doc, cmd);
        
        Assert.Equal(2, newDoc.Body.Count);
        Assert.Equal("Hello", ((RunNode)((ParagraphNode)newDoc.Body[0]).Inlines[0]).Text);
        Assert.Equal(" World", ((RunNode)((ParagraphNode)newDoc.Body[1]).Inlines[0]).Text);
    }

    [Fact]
    public void Split_AtStart_ProducesEmptyFirstParagraph()
    {
        var doc = MakeDoc("Hello World");
        var cmd = new SplitParagraphCommand(SessionId.NewRandom(), DocumentVersion.Zero, At(0, 0, 0));
        
        var newDoc = _mutator.Apply(doc, cmd);
        
        Assert.Equal(2, newDoc.Body.Count);
        Assert.Equal("", ((RunNode)((ParagraphNode)newDoc.Body[0]).Inlines[0]).Text);
        Assert.Equal("Hello World", ((RunNode)((ParagraphNode)newDoc.Body[1]).Inlines[0]).Text);
    }

    [Fact]
    public void Merge_TwoParagraphs_CombinesInlines()
    {
        var doc = MakeDoc("Hello", "World");
        var cmd = new MergeParagraphCommand(SessionId.NewRandom(), DocumentVersion.Zero, 1);
        
        var newDoc = _mutator.Apply(doc, cmd);
        
        Assert.Single(newDoc.Body);
        var run = (RunNode)((ParagraphNode)newDoc.Body[0]).Inlines[0];
        // Ensure they were adjacent and merged since styles are identical
        Assert.Equal("HelloWorld", run.Text);
    }

    [Fact]
    public void Merge_IndexZero_ReturnsUnchanged()
    {
        var doc = MakeDoc("Hello", "World");
        var cmd = new MergeParagraphCommand(SessionId.NewRandom(), DocumentVersion.Zero, 0);
        
        var newDoc = _mutator.Apply(doc, cmd);
        Assert.Same(doc, newDoc);
    }

    [Fact]
    public void SetStyle_Bold_AppliesOverride()
    {
        var doc = MakeDoc("Hello World");
        var styleDef = new CharacterStyleDef { Bold = true };
        var cmd = new SetCharacterStyleCommand(SessionId.NewRandom(), DocumentVersion.Zero, At(0, 0, 0), At(0, 0, 5), styleDef);
        
        var newDoc = _mutator.Apply(doc, cmd);
        var inlines = ((ParagraphNode)newDoc.Body[0]).Inlines;
        
        Assert.Equal(2, inlines.Count);
        
        var run1 = (RunNode)inlines[0];
        Assert.Equal("Hello", run1.Text);
        Assert.True(run1.Style.Bold);

        var run2 = (RunNode)inlines[1];
        Assert.Equal(" World", run2.Text);
        Assert.False(run2.Style.Bold); // Was Default
    }

    [Fact]
    public void MergeAdjacentRuns_SameStyle_MergesCorrectly()
    {
        var run1 = new RunNode("Hello", ParagraphStyle.Default.AsCharStyle(), null);
        var run2 = new RunNode("World", ParagraphStyle.Default.AsCharStyle(), null);
        var para = new ParagraphNode(ImmutableList.Create<InlineNode>(run1, run2), ParagraphStyle.Default, null);
        var doc = LokiDocument.Empty with { Body = ImmutableList.Create<BlockNode>(para) };

        // Merging para 1 onto 0 will trigger inline merging on identical runs if they are adjacent.
        // Actually, let's trigger it by deleting a char at boundary to force a refresh, or inserting.
        var cmd = new DeleteTextCommand(SessionId.NewRandom(), DocumentVersion.Zero, At(0, 1, 0), 0, "");
        
        var newDoc = _mutator.Apply(doc, cmd);
        var inlines = ((ParagraphNode)newDoc.Body[0]).Inlines;
        
        Assert.Single(inlines);
        Assert.Equal("HelloWorld", ((RunNode)inlines[0]).Text);
    }

    [Fact]
    public void MergeAdjacentRuns_DifferentStyle_NotMerged()
    {
        var style1 = ParagraphStyle.Default.AsCharStyle() with { Bold = true };
        var style2 = ParagraphStyle.Default.AsCharStyle() with { Bold = false };
        
        var run1 = new RunNode("Hello", style1, null);
        var run2 = new RunNode("World", style2, null);
        var para = new ParagraphNode(ImmutableList.Create<InlineNode>(run1, run2), ParagraphStyle.Default, null);
        var doc = LokiDocument.Empty with { Body = ImmutableList.Create<BlockNode>(para) };

        var cmd = new DeleteTextCommand(SessionId.NewRandom(), DocumentVersion.Zero, At(0, 1, 0), 0, "");
        
        var newDoc = _mutator.Apply(doc, cmd);
        var inlines = ((ParagraphNode)newDoc.Body[0]).Inlines;
        
        Assert.Equal(2, inlines.Count);
    }

    [Fact]
    public void InsertChar_ReturnsNewCaretPosition()
    {
        var doc = MakeDoc("Hello");
        var (newDoc, newCaret) = _mutator.InsertChar(doc, 'X', At(0, 0, 0));
        
        Assert.Equal(1, newCaret.CharOffset);
        var run = (RunNode)((ParagraphNode)newDoc.Body[0]).Inlines[0];
        Assert.Equal("XHello", run.Text);
    }
}
