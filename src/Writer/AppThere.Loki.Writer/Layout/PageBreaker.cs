// LAYER:   AppThere.Loki.Writer — Layout Engine
// KIND:    Implementation
// PURPOSE: Stage 3 of the layout pipeline (ADR-008 §2.4). Greedy page-break
//          algorithm. Assigns each BrokenParagraph to a page by accumulating
//          y-offsets and starting a new page whenever the next paragraph would
//          overflow the content area. SpaceBefore of the first paragraph on a
//          page is ignored (top of page). Always produces at least one page.
//          Pure algorithm — no I/O, no font access, no SkiaSharp dependency.
// DEPENDS: BrokenParagraph, PageLayout, PlacedParagraph, PageStyle
// USED BY: LayoutEngine (Stage 3)
// PHASE:   3
// ADR:     ADR-008

using System.Collections.Immutable;
using AppThere.Loki.Writer.Model;

namespace AppThere.Loki.Writer.Layout;

internal sealed class PageBreaker
{
    public ImmutableArray<PageLayout> Break(
        IReadOnlyList<BrokenParagraph> paragraphs,
        PageStyle pageStyle)
    {
        var pages   = ImmutableArray.CreateBuilder<PageLayout>();
        var current = ImmutableArray.CreateBuilder<PlacedParagraph>();
        var pageIndex  = 0;
        float yOffset  = 0f;
        float content  = pageStyle.ContentHeightPts;
        bool  firstOnPage = true;

        foreach (var para in paragraphs)
        {
            var height = ParaHeight(para);
            var spaceB = firstOnPage ? 0f : para.Style.SpaceBeforePts;

            if (!firstOnPage && yOffset + spaceB + height > content)
            {
                // Flush current page
                pages.Add(new PageLayout(pageIndex++, current.ToImmutable()));
                current.Clear();
                yOffset = 0f;
                firstOnPage = true;
                spaceB = 0f;
            }

            current.Add(new PlacedParagraph(para, yOffset + spaceB));
            yOffset += spaceB + height + para.Style.SpaceAfterPts;
            firstOnPage = false;
        }

        // Always emit the last page (even if empty)
        pages.Add(new PageLayout(pageIndex, current.ToImmutable()));

        return pages.ToImmutable();
    }

    private static float ParaHeight(BrokenParagraph para) =>
        para.Lines.Length * para.Style.LineHeightPts;
}
