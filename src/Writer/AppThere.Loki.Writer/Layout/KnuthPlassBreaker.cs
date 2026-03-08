// LAYER:   AppThere.Loki.Writer — Layout Engine
// KIND:    Implementation
// PURPOSE: Stage 2 of the layout pipeline (ADR-008 §2.3). Implements simplified
//          Knuth-Plass line breaking: finds the globally-optimal set of break
//          points for a MeasuredParagraph by minimising total demerits.
//          Simplified: no looseness parameter, no fitness classes.
//          Falls back to greedy breaking if no feasible K-P solution exists.
//          Pure algorithm — no I/O, no font access, no SkiaSharp dependency.
// DEPENDS: MeasuredParagraph, BrokenParagraph, BrokenLine, LayoutItem hierarchy
// USED BY: LayoutEngine (Stage 2)
// PHASE:   3
// ADR:     ADR-008

using System.Collections.Immutable;
using AppThere.Loki.Writer.Model.Styles;

namespace AppThere.Loki.Writer.Layout;

internal sealed class KnuthPlassBreaker
{
    public const float DefaultTolerance = 2.0f;

    // Width added when a flagged (hyphenated) break is taken.
    private const float HyphenWidth = 0f; // hyphen glyph emitted by Stage 4

    // ── Active node (internal dynamic-programming state) ──────────────────────

    private sealed record ActiveNode(
        int        BreakIndex,    // item index of the break Penalty (-1 = start)
        float      TotalWidth,
        float      TotalStretch,
        float      TotalShrink,
        float      TotalDemerits,
        bool       BreakFlagged,  // true if this break is a flagged (hyphen) break
        float      BreakR,        // adjustment ratio at this break (for BrokenLine)
        ActiveNode? Previous);

    // ── Main entry point ──────────────────────────────────────────────────────

    public BrokenParagraph Break(MeasuredParagraph measured)
    {
        var items = measured.Items;

        // Running totals accumulated while scanning items
        float sumWidth = 0f, sumStretch = 0f, sumShrink = 0f;

        // Active nodes start with a single virtual node before all content
        var activeNodes = new List<ActiveNode>
        {
            new ActiveNode(-1, 0f, 0f, 0f, 0f, false, 0f, null)
        };

        for (var i = 0; i < items.Length; i++)
        {
            var item = items[i];

            if (item is BoxItem box)
            {
                sumWidth += box.Box.Width;
            }
            else if (item is GlueItem glue)
            {
                sumWidth   += glue.Glue.Width;
                sumStretch += glue.Glue.Stretch;
                sumShrink  += glue.Glue.Shrink;
            }
            else if (item is PenaltyItem pen && !float.IsPositiveInfinity(pen.Pen.Cost))
            {
                var countBefore = activeNodes.Count;
                TryBreak(i, pen, measured.LineWidthPts,
                         sumWidth, sumStretch, sumShrink,
                         activeNodes);

                // Forced breaks must be taken by ALL active lines.
                // Deactivate every node that predates this break.
                if (float.IsNegativeInfinity(pen.Pen.Cost) && activeNodes.Count > countBefore)
                {
                    var newNodes = activeNodes.GetRange(countBefore, activeNodes.Count - countBefore);
                    activeNodes.Clear();
                    activeNodes.AddRange(newNodes);
                }
            }
        }

        if (activeNodes.Count == 0)
            return GreedyBreak(measured);

        // Select active node with lowest total demerits
        var optimal = activeNodes[0];
        foreach (var n in activeNodes)
            if (n.TotalDemerits < optimal.TotalDemerits) optimal = n;

        return AssembleLines(measured, optimal);
    }

    // ── Break-point evaluation ────────────────────────────────────────────────

    private static void TryBreak(
        int           breakIndex,
        PenaltyItem   pen,
        float         lineWidthPts,
        float         sumWidth,
        float         sumStretch,
        float         sumShrink,
        List<ActiveNode> activeNodes)
    {
        var penaltyWidth = pen.Pen.Flagged ? HyphenWidth : 0f;
        ActiveNode? bestPrev = null;
        float bestDemerits   = float.PositiveInfinity;

        var toDeactivate = new List<ActiveNode>();

        foreach (var a in activeNodes)
        {
            var lineWidth = sumWidth - a.TotalWidth + penaltyWidth;
            var r = AdjustmentRatio(
                lineWidth, lineWidthPts,
                sumStretch - a.TotalStretch,
                sumShrink  - a.TotalShrink);

            if (r < -1f)
            {
                toDeactivate.Add(a); // line too tight — discard active node
                continue;
            }

            if (r > DefaultTolerance && !float.IsNegativeInfinity(pen.Pen.Cost))
                continue; // line too loose (skip unless forced break)

            var d = Demerits(r, pen.Pen.Cost, pen.Pen.Flagged, a.BreakFlagged);
            if (d < bestDemerits)
            {
                bestDemerits = d;
                bestPrev     = a;
            }
        }

        foreach (var dead in toDeactivate)
            activeNodes.Remove(dead);

        if (bestPrev is null) return;

        var lineWidth2 = sumWidth - bestPrev.TotalWidth + penaltyWidth;
        var rBest = AdjustmentRatio(
            lineWidth2, lineWidthPts,
            sumStretch - bestPrev.TotalStretch,
            sumShrink  - bestPrev.TotalShrink);

        activeNodes.Add(new ActiveNode(
            breakIndex,
            sumWidth, sumStretch, sumShrink,
            bestPrev.TotalDemerits + bestDemerits,
            pen.Pen.Flagged,
            rBest,
            bestPrev));
    }

    // ── K-P formulae ──────────────────────────────────────────────────────────

    internal static float AdjustmentRatio(
        float lineWidth, float available, float stretch, float shrink)
    {
        if (lineWidth == available) return 0f;
        if (lineWidth < available)
            return stretch > 0f ? (available - lineWidth) / stretch : float.PositiveInfinity;
        return shrink > 0f ? (available - lineWidth) / shrink : float.NegativeInfinity;
    }

    private static float Demerits(float r, float penaltyCost, bool isFlagged, bool prevFlagged)
    {
        var badness = 1f + 100f * MathF.Abs(r) * MathF.Abs(r) * MathF.Abs(r);
        var d = badness * badness;
        d += penaltyCost >= 0f ? penaltyCost * penaltyCost : -(penaltyCost * penaltyCost);
        if (isFlagged && prevFlagged) d += 3000f;
        return d;
    }

    // ── Line assembly from break-point chain ──────────────────────────────────

    private static BrokenParagraph AssembleLines(MeasuredParagraph measured, ActiveNode optimal)
    {
        // Trace back through Previous links to collect break indices in reverse
        var breakChain = new List<ActiveNode>();
        var cur = optimal;
        while (cur.Previous is not null)
        {
            breakChain.Add(cur);
            cur = cur.Previous;
        }
        breakChain.Reverse();

        var lines = ImmutableArray.CreateBuilder<BrokenLine>(breakChain.Count);
        var items = measured.Items;
        var prevBreakIdx = -1; // initial virtual break is at -1

        for (var li = 0; li < breakChain.Count; li++)
        {
            var node   = breakChain[li];
            var isLast = li == breakChain.Count - 1;
            var isForcedBreak = float.IsNegativeInfinity(
                items[node.BreakIndex] is PenaltyItem p ? p.Pen.Cost : 0f);

            // Line content: items between prevBreakIdx+1 and node.BreakIndex-1 (excl. Penalty)
            var start = prevBreakIdx + 1;
            var end   = node.BreakIndex; // exclusive (penalty not included)
            var lineItems = ImmutableArray.CreateBuilder<LayoutItem>(Math.Max(0, end - start));
            for (var j = start; j < end && j < items.Length; j++)
                lineItems.Add(items[j]);

            lines.Add(new BrokenLine(
                lineItems.ToImmutable(),
                node.BreakR,
                isForcedBreak,
                isLast));

            prevBreakIdx = node.BreakIndex;
        }

        if (lines.Count == 0)
            lines.Add(new BrokenLine(ImmutableArray<LayoutItem>.Empty, 0f, false, true));

        return new BrokenParagraph(measured.ParagraphIndex, measured.Style, lines.ToImmutable());
    }

    // ── Greedy fallback ───────────────────────────────────────────────────────

    private static BrokenParagraph GreedyBreak(MeasuredParagraph measured)
    {
        var lines      = ImmutableArray.CreateBuilder<BrokenLine>();
        var lineItems  = ImmutableArray.CreateBuilder<LayoutItem>();
        var stagingIdx = 0;   // index into measured.Items for start of current line
        float lineWidth = 0f;
        int   lastBreakableRelative = -1; // relative to stagingIdx
        float widthAtLastBreakable  = 0f;
        var   items = measured.Items;

        for (var i = 0; i < items.Length; i++)
        {
            var item = items[i];

            if (item is PenaltyItem pp)
            {
                if (float.IsNegativeInfinity(pp.Pen.Cost))
                {
                    // Forced break: flush current line
                    lines.Add(new BrokenLine(lineItems.ToImmutable(), 0f, true, false));
                    lineItems.Clear();
                    lineWidth = 0f;
                    lastBreakableRelative = -1;
                    widthAtLastBreakable  = 0f;
                    continue;
                }

                if (!float.IsPositiveInfinity(pp.Pen.Cost))
                {
                    lastBreakableRelative = lineItems.Count;
                    widthAtLastBreakable  = lineWidth;
                }
                continue; // penalties don't add to visual line content
            }

            if (item is BoxItem bx) lineWidth += bx.Box.Width;
            else if (item is GlueItem gl) lineWidth += gl.Glue.Width;
            lineItems.Add(item);

            if (lineWidth > measured.LineWidthPts && lastBreakableRelative > 0)
            {
                var lineContent = lineItems.ToImmutable().RemoveRange(
                    lastBreakableRelative, lineItems.Count - lastBreakableRelative);
                lines.Add(new BrokenLine(lineContent, 0f, false, false));

                var remaining = lineItems.ToImmutable().RemoveRange(0, lastBreakableRelative);
                lineItems.Clear();
                lineItems.AddRange(remaining);
                lineWidth -= widthAtLastBreakable;
                lastBreakableRelative = -1;
                widthAtLastBreakable  = 0f;
            }
        }

        if (lineItems.Count > 0)
            lines.Add(new BrokenLine(lineItems.ToImmutable(), 0f, false, true));

        if (lines.Count == 0)
            lines.Add(new BrokenLine(ImmutableArray<LayoutItem>.Empty, 0f, false, true));
        else
        {
            var last = lines[^1];
            if (!last.IsLastLine)
                lines[^1] = last with { IsLastLine = true };
        }

        return new BrokenParagraph(measured.ParagraphIndex, measured.Style, lines.ToImmutable());
    }
}
