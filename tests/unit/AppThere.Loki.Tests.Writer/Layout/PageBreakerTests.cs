// LAYER:   AppThere.Loki.Tests.Writer — Tests
// KIND:    Tests
// PURPOSE: Unit tests for PageBreaker (Layout Stage 3).
//          Verifies greedy page assignment: single-page fit, multi-page
//          overflow, forced para-too-tall pass-through, and empty input.
// DEPENDS: PageBreaker, BrokenParagraph, PageLayout, PageStyle, BrokenLine
// USED BY: CI test runner
// PHASE:   3
// ADR:     ADR-008

namespace AppThere.Loki.Tests.Writer.Layout;

public sealed class PageBreakerTests
{
    // ── Fixture helpers ────────────────────────────────────────────────────────

    private static readonly PageStyle TestPage = new(
        WidthPts:        612f,  // US Letter
        HeightPts:       792f,
        MarginTopPts:    56.7f,
        MarginBottomPts: 56.7f,
        MarginStartPts:  56.7f,
        MarginEndPts:    56.7f);

    // TestPage.ContentHeightPts = 792 - 56.7 - 56.7 = 678.6pt

    private static BrokenParagraph MakePara(
        int index, float lineHeightPts, int lineCount,
        float spaceAfter = 0f, float spaceBefore = 0f)
    {
        var style = ParagraphStyle.Default with
        {
            LineHeightPts  = lineHeightPts,
            SpaceAfterPts  = spaceAfter,
            SpaceBeforePts = spaceBefore,
        };
        var lines = Enumerable.Range(0, lineCount)
            .Select(_ => new BrokenLine(
                ImmutableArray<LayoutItem>.Empty, 0f, false, false))
            .ToImmutableArray();
        return new BrokenParagraph(index, style, lines);
    }

    private static PageBreaker CreateSut() => new();

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Break_EmptyInput_ReturnsSingleEmptyPage()
    {
        var sut    = CreateSut();
        var result = sut.Break([], TestPage);

        result.Should().HaveCount(1);
        result[0].PageIndex.Should().Be(0);
        result[0].Paragraphs.Should().BeEmpty();
    }

    [Fact]
    public void Break_SingleParaFitsOnOnePage_ReturnsSinglePage()
    {
        var sut  = CreateSut();
        var para = MakePara(0, lineHeightPts: 14f, lineCount: 3);
        // Height = 3 × 14 = 42pt — well within 678.6pt content

        var result = sut.Break([para], TestPage);

        result.Should().HaveCount(1);
        result[0].Paragraphs.Should().HaveCount(1);
        result[0].Paragraphs[0].YOffsetPts.Should().Be(0f);
    }

    [Fact]
    public void Break_ManyShortParas_AllOnOnePage_ReturnsSinglePage()
    {
        // 30 paras × 14pt = 420pt < 678.6pt content height
        var sut    = CreateSut();
        var paras  = Enumerable.Range(0, 30)
            .Select(i => MakePara(i, lineHeightPts: 14f, lineCount: 1))
            .ToList();

        var result = sut.Break(paras, TestPage);

        result.Should().HaveCount(1);
        result[0].Paragraphs.Should().HaveCount(30);
    }

    [Fact]
    public void Break_ParasOverflowPage_ReturnsMultiplePages()
    {
        // 50 paras × 14pt = 700pt > 678.6pt → must span at least 2 pages
        var sut   = CreateSut();
        var paras = Enumerable.Range(0, 50)
            .Select(i => MakePara(i, lineHeightPts: 14f, lineCount: 1))
            .ToList();

        var result = sut.Break(paras, TestPage);

        result.Should().HaveCountGreaterThan(1);
        result.Sum(p => p.Paragraphs.Length).Should().Be(50,
            "all paragraphs must appear on some page");
    }

    [Fact]
    public void Break_SecondPage_StartsAtYOffsetZero()
    {
        // Force overflow: 60 paras × 14pt > 678pt
        var sut   = CreateSut();
        var paras = Enumerable.Range(0, 60)
            .Select(i => MakePara(i, lineHeightPts: 14f, lineCount: 1))
            .ToList();

        var result = sut.Break(paras, TestPage);

        result.Should().HaveCountGreaterThan(1);
        result[1].Paragraphs[0].YOffsetPts.Should().Be(0f,
            "first paragraph on page 2 starts at y=0 (top of content area)");
    }

    [Fact]
    public void Break_TallParagraphAlone_FitsOnItsOwnPage()
    {
        // A single very tall paragraph should still appear (not dropped)
        var sut  = CreateSut();
        var para = MakePara(0, lineHeightPts: 800f, lineCount: 1);

        var result = sut.Break([para], TestPage);

        result.Should().HaveCount(1);
        result[0].Paragraphs.Should().HaveCount(1);
    }

    [Fact]
    public void Break_PageIndicesAreSequential()
    {
        var sut   = CreateSut();
        var paras = Enumerable.Range(0, 60)
            .Select(i => MakePara(i, lineHeightPts: 14f, lineCount: 1))
            .ToList();

        var result = sut.Break(paras, TestPage);

        for (var i = 0; i < result.Length; i++)
            result[i].PageIndex.Should().Be(i);
    }
}
