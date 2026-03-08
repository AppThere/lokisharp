// LAYER:   AppThere.Loki.Tests.Writer — Tests
// KIND:    Tests
// PURPOSE: Integration tests for LayoutEngine (all four stages end-to-end).
//          Uses a real SkiaFontManager (embedded fonts) and real InlineMeasurer
//          to verify that LayoutEngine produces the expected number of
//          PaintScenes and bands for known document inputs.
//          Tests structural properties (count, type) not pixel values.
// DEPENDS: LayoutEngine, ILayoutEngine, LayoutCache, LokiDocument,
//          ParagraphNode, RunNode, ParagraphStyle, SkiaFontManager, NullLokiLogger
// USED BY: CI test runner
// PHASE:   3
// ADR:     ADR-008

using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.Skia.Fonts;

namespace AppThere.Loki.Tests.Writer.Layout;

public sealed class LayoutEngineIntegrationTests
{
    // ── Fixture helpers ────────────────────────────────────────────────────────

    private static ILayoutEngine CreateSut()
    {
        var fontManager = new SkiaFontManager(NullLokiLogger.Instance);
        return new LayoutEngine(fontManager, NullLokiLogger.Instance);
    }

    private static LokiDocument SingleParaDoc(string text) =>
        LokiDocument.Empty with
        {
            Body = ImmutableList.Create<BlockNode>(
                new ParagraphNode(
                    ImmutableList.Create<InlineNode>(new RunNode(text, CharacterStyle.Default, null)),
                    ParagraphStyle.Default,
                    null)),
        };

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Layout_EmptyDocument_ReturnsOnePaintScene()
    {
        var sut    = CreateSut();
        var cache  = new LayoutCache();
        var result = sut.Layout(LokiDocument.Empty, cache);

        result.Should().HaveCount(1, "empty document always yields one (blank) page");
    }

    [Fact]
    public void Layout_SingleShortParagraph_ReturnsOnePaintScene()
    {
        var sut    = CreateSut();
        var cache  = new LayoutCache();
        var doc    = SingleParaDoc("Hello World");

        var result = sut.Layout(doc, cache);

        result.Should().HaveCount(1);
    }

    [Fact]
    public void Layout_SingleParagraph_SceneContainsBands()
    {
        var sut   = CreateSut();
        var cache = new LayoutCache();
        var doc   = SingleParaDoc("Hello World");

        var result = sut.Layout(doc, cache);
        var scene  = result[0];

        scene.Bands.Should().NotBeEmpty("a paragraph with text must produce at least one band");
    }

    [Fact]
    public void Layout_MultipleParagraphs_AllBandsPresent()
    {
        var sut   = CreateSut();
        var cache = new LayoutCache();
        var body  = ImmutableList.CreateRange(
            Enumerable.Range(0, 5).Select(i =>
                (BlockNode)new ParagraphNode(
                    ImmutableList.Create<InlineNode>(
                        new RunNode($"Paragraph {i}", CharacterStyle.Default, null)),
                    ParagraphStyle.Default,
                    null)));

        var doc    = LokiDocument.Empty with { Body = body };
        var result = sut.Layout(doc, cache);

        result.Should().NotBeEmpty();
        result.Sum(s => s.Bands.Length).Should().BeGreaterThanOrEqualTo(5,
            "five paragraphs produce at least five bands");
    }

    [Fact]
    public void Layout_SecondCallWithSameCache_ReturnsSameSceneCount()
    {
        var sut   = CreateSut();
        var cache = new LayoutCache();
        var doc   = SingleParaDoc("Cached paragraph");

        var result1 = sut.Layout(doc, cache);
        var result2 = sut.Layout(doc, cache);

        result1.Count.Should().Be(result2.Count,
            "second layout call with identical doc + cache must produce same page count");
    }

    [Fact]
    public void Layout_SceneSizeMatchesPageStyle()
    {
        var sut   = CreateSut();
        var cache = new LayoutCache();
        var doc   = LokiDocument.Empty;  // uses PageStyle.A4

        var result = sut.Layout(doc, cache);
        var scene  = result[0];

        scene.SizeInPoints.Width.Should().BeApproximately(595.28f, 1f,
            "A4 width is ~595pt");
        scene.SizeInPoints.Height.Should().BeApproximately(841.89f, 1f,
            "A4 height is ~842pt");
    }
}
