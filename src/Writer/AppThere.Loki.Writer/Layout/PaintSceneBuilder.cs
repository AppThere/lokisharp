// LAYER:   AppThere.Loki.Writer — Layout Engine
// KIND:    Implementation
// PURPOSE: Stage 4 of the layout pipeline (ADR-008 §2.5). Converts a PageLayout
//          into a PaintScene by building PaintBands with GlyphRunNodes.
//          One band per text line; each BoxItem in a BrokenLine becomes a GlyphRun.
//          Glue widths are adjusted by the line's AdjustmentRatio (r):
//            r > 0 → expand by r × stretch;  r < 0 → shrink by |r| × shrink.
//          Last line and forced-break lines are NOT justified (r clamped to 0).
//          Baseline is at 80% of LineHeightPts (simple approximation).
//          Colour comes from the paragraph's resolved Color (no per-run colour yet).
// DEPENDS: PageLayout, PaintScene, PaintBand, GlyphRunNode, GlyphRun, TextPaint,
//          RectF, PointF, PageStyle, ParagraphStyle
// USED BY: LayoutEngine (Stage 4)
// PHASE:   3
// ADR:     ADR-008

using System.Collections.Immutable;
using AppThere.Loki.Kernel.Geometry;
using AppThere.Loki.Skia.Scene;
using AppThere.Loki.Skia.Scene.Nodes;
using AppThere.Loki.Writer.Model;
using AppThere.Loki.Writer.Model.Styles;

namespace AppThere.Loki.Writer.Layout;

internal sealed class PaintSceneBuilder
{
    public PaintScene Build(PageLayout page, PageStyle pageStyle)
    {
        var builder = PaintScene.CreateBuilder(page.PageIndex)
            .WithSize(pageStyle.WidthPts, pageStyle.HeightPts);

        foreach (var placed in page.Paragraphs)
            BuildParaBands(builder, placed, pageStyle);

        return builder.Build();
    }

    // ── Per-paragraph ─────────────────────────────────────────────────────────

    private static void BuildParaBands(
        PaintScene.Builder builder,
        PlacedParagraph   placed,
        PageStyle         pageStyle)
    {
        var para  = placed.Paragraph;
        var style = para.Style;
        var pIndex = para.ParagraphIndex;
        float yBase = pageStyle.MarginTopPts + placed.YOffsetPts;

        for (var li = 0; li < para.Lines.Length; li++)
        {
            var line  = para.Lines[li];
            float yLine = yBase + li * style.LineHeightPts;

            var nodes = BuildLineNodes(pIndex, line, style, pageStyle, yLine);
            builder.AddBand(new PaintBand(yLine, style.LineHeightPts, nodes, 0L));
        }
    }

    // ── Per-line ──────────────────────────────────────────────────────────────

    private static ImmutableArray<PaintNode> BuildLineNodes(
        int             pIndex,
        BrokenLine      line,
        ParagraphStyle  style,
        PageStyle       pageStyle,
        float           yLine)
    {
        var nodes    = ImmutableArray.CreateBuilder<PaintNode>();
        float x      = pageStyle.MarginStartPts;
        float baseline = yLine + style.LineHeightPts * 0.8f;

        // Do not justify last or forced-break lines
        float r = (line.IsLastLine || line.IsForcedBreak) ? 0f : line.AdjustmentRatio;

        foreach (var item in line.Items)
        {
            if (item is BoxItem box)
            {
                var cluster = box.Box.Glyphs;
                if (cluster.GlyphIds.Length == 0) continue;

                float runWidth = cluster.Advances.Sum();
                if (runWidth < 0f) runWidth = 0f;

                var bounds = new RectF(x, yLine, runWidth, style.LineHeightPts);
                var origin = new PointF(x, baseline);

                var glyphRun = new GlyphRun(
                    cluster.Typeface,
                    cluster.FontSizePts,
                    cluster.GlyphIds.ToImmutableArray(),
                    cluster.Advances.ToImmutableArray(),
                    null, null);

                var paint = new TextPaint(style.Color);
                nodes.Add(new GlyphRunNode(bounds, origin,
                    ImmutableArray.Create<GlyphRun>(glyphRun), paint,
                    pIndex, box.Box.RunIndex, box.Box.RunOffset, box.Box.Text));

                x += runWidth;
            }
            else if (item is GlueItem glue)
            {
                float w = glue.Glue.Width;
                if (r > 0f) w += r * glue.Glue.Stretch;
                else if (r < 0f) w += r * glue.Glue.Shrink;
                x += w;
            }
            // PenaltyItems are not rendered
        }

        return nodes.ToImmutable();
    }
}
