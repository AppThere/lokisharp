// LAYER:   AppThere.Loki.Tests.Writer — Tests
// KIND:    Tests
// PURPOSE: Unit tests for InlineMeasurer (Layout Stage 1).
//          Verifies the item sequence (Box/Glue/Penalty types and counts)
//          produced when walking various ParagraphNode configurations.
//          Tests item types and structure, not exact pixel widths.
// DEPENDS: InlineMeasurer, ParagraphNode, RunNode, HardLineBreakNode, TabNode,
//          IFontManager, ILokiTypeface, ILokiLogger, MeasuredParagraph
// USED BY: CI test runner
// PHASE:   3
// ADR:     ADR-008

namespace AppThere.Loki.Tests.Writer.Layout;

public sealed class InlineMeasurerTests
{
    // ── Fixture helpers ────────────────────────────────────────────────────────

    private readonly IFontManager  _fontManager = Substitute.For<IFontManager>();
    private readonly ILokiTypeface _typeface    = Substitute.For<ILokiTypeface>();
    private readonly ILokiLogger   _logger      = Substitute.For<ILokiLogger>();

    public InlineMeasurerTests()
    {
        _typeface.FamilyName.Returns("sans-serif");

        // TryGetTypeface returns the mock typeface for any descriptor
        _fontManager
            .TryGetTypeface(Arg.Any<FontDescriptor>(), out Arg.Any<ILokiTypeface?>())
            .Returns(x => { x[1] = _typeface; return true; });

        // GetFallbackForScript returns the mock typeface
        _fontManager
            .GetFallbackForScript(Arg.Any<UnicodeScript>())
            .Returns(_typeface);
    }

    private InlineMeasurer CreateSut() => new(_fontManager, _logger);

    private static ParagraphNode EmptyParagraph() =>
        new(ImmutableList<InlineNode>.Empty, ParagraphStyle.Default, null);

    private static ParagraphNode ParagraphWith(params InlineNode[] inlines) =>
        new(inlines.ToImmutableList(), ParagraphStyle.Default, null);

    private static RunNode Run(string text) =>
        new(text, CharacterStyle.Default, null);

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Measure_EmptyParagraph_ReturnsFinishingSequenceOnly()
    {
        var sut = CreateSut();
        var result = sut.Measure(EmptyParagraph(), 400f);

        // Finishing sequence: GlueItem(0, +Inf, 0) + PenaltyItem(-Inf)
        result.Items.Should().HaveCount(2);
        result.Items[0].Should().BeOfType<GlueItem>();
        result.Items[1].Should().BeOfType<PenaltyItem>();

        var endPen = ((PenaltyItem)result.Items[1]).Pen;
        endPen.Cost.Should().Be(float.NegativeInfinity);
    }

    [Fact]
    public void Measure_SingleWord_ReturnsSingleBoxThenFinishingSequence()
    {
        var sut = CreateSut();
        var result = sut.Measure(ParagraphWith(Run("Hello")), 400f);

        // Box + finishing Glue + finishing Penalty = 3 items
        result.Items.Should().HaveCount(3);
        result.Items[0].Should().BeOfType<BoxItem>();
        result.Items[1].Should().BeOfType<GlueItem>();
        result.Items[2].Should().BeOfType<PenaltyItem>();

        var finGlue = ((GlueItem)result.Items[1]).Glue;
        finGlue.Stretch.Should().Be(float.PositiveInfinity, "finishing glue has infinite stretch");
    }

    [Fact]
    public void Measure_TwoWords_ReturnsTwoBoxesWithGlueAndPenalty()
    {
        // "Hello World" → Box | Glue | Penalty(0) | Box | finishing Glue | finishing Penalty
        var sut    = CreateSut();
        var result = sut.Measure(ParagraphWith(Run("Hello World")), 400f);

        result.Items.Should().HaveCount(6);
        result.Items[0].Should().BeOfType<BoxItem>();
        result.Items[1].Should().BeOfType<GlueItem>();
        result.Items[2].Should().BeOfType<PenaltyItem>();
        result.Items[3].Should().BeOfType<BoxItem>();
        result.Items[4].Should().BeOfType<GlueItem>();  // finishing glue
        result.Items[5].Should().BeOfType<PenaltyItem>(); // finishing penalty

        var breakPen = ((PenaltyItem)result.Items[2]).Pen;
        breakPen.Cost.Should().Be(0f, "inter-word penalty is neutral");
        breakPen.Flagged.Should().BeFalse();
    }

    [Fact]
    public void Measure_HardLineBreak_ReturnsForcedPenalty()
    {
        // RunNode("A") + HardLineBreakNode + RunNode("B")
        var sut = CreateSut();
        var result = sut.Measure(ParagraphWith(
            Run("A"),
            new HardLineBreakNode(),
            Run("B")), 400f);

        // Should contain a Penalty with NegativeInfinity cost (forced break)
        var forcedBreaks = result.Items
            .OfType<PenaltyItem>()
            .Where(p => float.IsNegativeInfinity(p.Pen.Cost))
            .ToList();

        forcedBreaks.Should().HaveCountGreaterThanOrEqualTo(1,
            "HardLineBreakNode must emit a forced (-Infinity) penalty");
    }

    [Fact]
    public void Measure_Tab_ReturnsFixedGlue()
    {
        var sut    = CreateSut();
        var result = sut.Measure(ParagraphWith(new TabNode()), 400f);

        // Tab + finishing Glue + finishing Penalty = 3 items
        result.Items.Should().HaveCount(3);
        var tabGlue = result.Items[0].Should().BeOfType<GlueItem>().Subject.Glue;

        tabGlue.Width.Should().BeApproximately(36f, 0.001f, "default tab width is 36pt (0.5 inch)");
        tabGlue.Stretch.Should().Be(0f, "tab is fixed — no stretch");
        tabGlue.Shrink.Should().Be(0f, "tab is fixed — no shrink");
    }
}
