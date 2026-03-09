// LAYER:   AppThere.Loki.Tests.Writer — Tests
// KIND:    Tests
// PURPOSE: Unit tests for KnuthPlassBreaker (Layout Stage 2).
//          Uses fixed-width Box items so tests are independent of font metrics.
//          Verifies optimal break selection, forced breaks, fallback greedy
//          breaking, adjustment ratio calculation, and edge cases.
// DEPENDS: KnuthPlassBreaker, MeasuredParagraph, BrokenParagraph, LayoutItem
// USED BY: CI test runner
// PHASE:   3
// ADR:     ADR-008

namespace AppThere.Loki.Tests.Writer.Layout;

public sealed class KnuthPlassBreakerTests
{
    private readonly KnuthPlassBreaker _sut = new();

    // ── Helper: build a MeasuredParagraph from fixed word widths ─────────────

    /// <summary>
    /// Constructs a MeasuredParagraph whose item sequence mirrors what
    /// InlineMeasurer would produce for a single run of words.
    /// Each word becomes a BoxItem of the given width.
    /// Words are separated by Glue(6, 3, 2) + Penalty(0, false).
    /// The paragraph ends with the K-P finishing sequence.
    /// </summary>
    private static MeasuredParagraph BuildMeasured(float lineWidth, params float[] wordWidths)
    {
        var items = ImmutableArray.CreateBuilder<LayoutItem>();

        for (var i = 0; i < wordWidths.Length; i++)
        {
            var cluster = new GlyphCluster(
                Array.Empty<ushort>(), Array.Empty<float>(),
                Substitute.For<ILokiTypeface>(), 12f);
            items.Add(new BoxItem(new Box(wordWidths[i], cluster, 0, 0, "word")));

            if (i < wordWidths.Length - 1)
            {
                items.Add(new GlueItem(new Glue(6f, 3f, 2f)));
                items.Add(new PenaltyItem(new Penalty(0f, false)));
            }
        }

        // K-P finishing sequence
        items.Add(new GlueItem(new Glue(0f, float.PositiveInfinity, 0f)));
        items.Add(new PenaltyItem(new Penalty(float.NegativeInfinity, false)));

        return new MeasuredParagraph(0, ParagraphStyle.Default, items.ToImmutable(), lineWidth);
    }

    /// <summary>Builds a sequence with a forced break at a given position.</summary>
    private static MeasuredParagraph BuildWithForcedBreak(float lineWidth,
        float[] beforeBreak, float[] afterBreak)
    {
        var items = ImmutableArray.CreateBuilder<LayoutItem>();
        void AddWord(float w)
        {
            var c = new GlyphCluster(Array.Empty<ushort>(), Array.Empty<float>(),
                Substitute.For<ILokiTypeface>(), 12f);
            items.Add(new BoxItem(new Box(w, c, 0, 0, "word")));
        }

        foreach (var w in beforeBreak) { AddWord(w); items.Add(new GlueItem(new Glue(6f, 3f, 2f))); }
        items.Add(new PenaltyItem(new Penalty(float.NegativeInfinity, false))); // forced break
        foreach (var w in afterBreak)
        {
            AddWord(w);
            if (w != afterBreak[^1]) { items.Add(new GlueItem(new Glue(6f, 3f, 2f))); items.Add(new PenaltyItem(new Penalty(0f, false))); }
        }
        items.Add(new GlueItem(new Glue(0f, float.PositiveInfinity, 0f)));
        items.Add(new PenaltyItem(new Penalty(float.NegativeInfinity, false)));

        return new MeasuredParagraph(0, ParagraphStyle.Default, items.ToImmutable(), lineWidth);
    }

    // ── K-P break tests ───────────────────────────────────────────────────────

    [Fact]
    public void Break_SingleShortWord_ReturnsSingleLine()
    {
        var measured = BuildMeasured(lineWidth: 200f, wordWidths: 50f);
        var result   = _sut.Break(measured);

        result.Lines.Should().HaveCount(1);
        result.Lines[0].IsLastLine.Should().BeTrue();
    }

    [Fact]
    public void Break_TwoWordsFit_ReturnsSingleLine()
    {
        // Both words (40 + 6glue + 40 = 86) fit in 200pt line
        var measured = BuildMeasured(lineWidth: 200f, 40f, 40f);
        var result   = _sut.Break(measured);

        result.Lines.Should().HaveCount(1);
        result.Lines[0].IsLastLine.Should().BeTrue();
    }

    [Fact]
    public void Break_TwoWordsDontFit_ReturnsTwoLines()
    {
        // Two 80pt words + 6pt glue = 166pt > 100pt line → must split
        var measured = BuildMeasured(lineWidth: 100f, 80f, 80f);
        var result   = _sut.Break(measured);

        result.Lines.Should().HaveCount(2);
        result.Lines[^1].IsLastLine.Should().BeTrue();
    }

    [Fact]
    public void Break_ForcedBreak_SplitsAtPenalty()
    {
        // Force a break between "Hello" (50pt) and "World" (50pt) on a wide line
        var measured = BuildWithForcedBreak(500f,
            beforeBreak: new[] { 50f },
            afterBreak:  new[] { 50f });
        var result = _sut.Break(measured);

        result.Lines.Should().HaveCountGreaterThanOrEqualTo(2,
            "forced penalty must always produce a line break");

        var hasForced = result.Lines.Any(l => l.IsForcedBreak);
        hasForced.Should().BeTrue("at least one line should be flagged as forced break");
    }

    [Fact]
    public void Break_ManyWords_ProducesOptimalBreaks()
    {
        // 9 words of 20pt each; Glue = 6pt; lineWidth = 72pt
        // Ideal fill: 20+6+20+6+20 = 72 (exactly 3 words per line, r=0)
        // 9 = 3+3+3 → K-P produces 3 equal lines; no line has more than 2× words of another.
        var widths = Enumerable.Repeat(20f, 9).ToArray();
        var measured = BuildMeasured(lineWidth: 72f, wordWidths: widths);
        var result = _sut.Break(measured);

        result.Lines.Should().HaveCountGreaterThan(1);

        // Count boxes per line (excluding glue/penalty)
        var wordCounts = result.Lines
            .Select(l => l.Items.OfType<BoxItem>().Count())
            .Where(c => c > 0)
            .ToList();

        if (wordCounts.Count > 1)
        {
            var maxWords = wordCounts.Max();
            var minWords = wordCounts.Min();
            maxWords.Should().BeLessOrEqualTo(minWords * 2,
                "K-P should produce balanced lines");
        }
    }

    [Fact]
    public void Break_EmptyParagraph_ReturnsSingleEmptyLine()
    {
        // Finishing sequence only → at minimum one BrokenLine with IsLastLine
        var measured = BuildMeasured(lineWidth: 400f); // no words
        var result   = _sut.Break(measured);

        result.Lines.Should().HaveCountGreaterThanOrEqualTo(1);
        result.Lines[^1].IsLastLine.Should().BeTrue();
    }

    [Fact]
    public void Break_Tolerance_TightLineFallsBack()
    {
        // Words are wider than the line — K-P has no feasible breaks, greedy fires
        // Each word is 150pt on a 100pt line — cannot fit any word
        var measured = BuildMeasured(lineWidth: 100f, 150f, 150f, 150f);
        var result   = _sut.Break(measured);

        // Should not throw and must produce at least one line
        result.Lines.Should().HaveCountGreaterThan(0);
    }

    // ── AdjustmentRatio unit tests ────────────────────────────────────────────

    [Fact]
    public void AdjustmentRatio_IdealLine_ReturnsZero()
    {
        var r = KnuthPlassBreaker.AdjustmentRatio(100f, 100f, 30f, 10f);
        r.Should().Be(0f);
    }

    [Fact]
    public void AdjustmentRatio_LooseLine_ReturnsPositive()
    {
        // lineWidth < available → needs stretching → r > 0
        var r = KnuthPlassBreaker.AdjustmentRatio(80f, 100f, 20f, 10f);
        r.Should().BeGreaterThan(0f);
    }

    [Fact]
    public void AdjustmentRatio_TightLine_ReturnsNegative()
    {
        // lineWidth > available → needs shrinking → r < 0
        var r = KnuthPlassBreaker.AdjustmentRatio(120f, 100f, 20f, 10f);
        r.Should().BeLessThan(0f);
    }
}
