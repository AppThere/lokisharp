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
//          Bullet glyphs are prepended before the first line of list-item paragraphs.
// DEPENDS: PageLayout, PaintScene, PaintBand, GlyphRunNode, GlyphRun, TextPaint,
//          RectF, PointF, PageStyle, ParagraphStyle, IFontManager, LokiTextShaper
// USED BY: LayoutEngine (Stage 4)
// PHASE:   3
// ADR:     ADR-008

using System.Collections.Immutable;
using AppThere.Loki.Kernel.Fonts;
using AppThere.Loki.Kernel.Geometry;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.Skia.Fonts;
using AppThere.Loki.Skia.Scene;
using AppThere.Loki.Skia.Scene.Nodes;
using AppThere.Loki.Writer.Model;
using AppThere.Loki.Writer.Model.Styles;

namespace AppThere.Loki.Writer.Layout;

internal sealed class PaintSceneBuilder
{
    private readonly LokiTextShaper _shaper;

    public PaintSceneBuilder(IFontManager fontManager, ILokiLogger logger)
    {
        _shaper = new LokiTextShaper(fontManager, logger);
    }

    public PaintScene Build(PageLayout page, PageStyle pageStyle)
    {
        var builder = PaintScene.CreateBuilder(page.PageIndex)
            .WithSize(pageStyle.WidthPts, pageStyle.HeightPts);

        foreach (var placed in page.Paragraphs)
            BuildParaBands(builder, placed, pageStyle);

        return builder.Build();
    }

    // ── Per-paragraph ─────────────────────────────────────────────────────────

    private void BuildParaBands(
        PaintScene.Builder builder,
        PlacedParagraph   placed,
        PageStyle         pageStyle)
    {
        var para  = placed.Paragraph;
        var style = para.Style;
        float yBase = pageStyle.MarginTopPts + placed.YOffsetPts;

        for (var li = 0; li < para.Lines.Length; li++)
        {
            var line  = para.Lines[li];
            float yLine = yBase + li * style.LineHeightPts;

            var nodes = BuildLineNodes(line, style, pageStyle, yLine, isFirstLine: li == 0);
            builder.AddBand(new PaintBand(yLine, style.LineHeightPts, nodes, 0L));
        }
    }

    // ── Per-line ──────────────────────────────────────────────────────────────

    private ImmutableArray<PaintNode> BuildLineNodes(
        BrokenLine      line,
        ParagraphStyle  style,
        PageStyle       pageStyle,
        float           yLine,
        bool            isFirstLine)
    {
        var nodes    = ImmutableArray.CreateBuilder<PaintNode>();
        float baseline = yLine + style.LineHeightPts * 0.8f;

        // Do not justify last or forced-break lines
        float r = (line.IsLastLine || line.IsForcedBreak) ? 0f : line.AdjustmentRatio;

        // For the first line of a list paragraph, emit a bullet glyph and indent text.
        float textX = pageStyle.MarginStartPts;
        if (isFirstLine && style.ListStyleId is not null)
            textX = EmitBullet(nodes, style, pageStyle, yLine, baseline);

        float x = textX;

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
                    ImmutableArray.Create<GlyphRun>(glyphRun), paint));

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

    // ── Bullet emission ───────────────────────────────────────────────────────

    /// <summary>
    /// Shapes and emits a bullet GlyphRunNode for the current list level.
    /// Returns the x coordinate where text content should begin.
    /// </summary>
    private float EmitBullet(
        ImmutableArray<PaintNode>.Builder nodes,
        ParagraphStyle style,
        PageStyle      pageStyle,
        float          yLine,
        float          baseline)
    {
        var bulletChar = style.ListLevel switch
        {
            0 => "•",   // U+2022
            1 => "◦",   // U+25E6
            2 => "▪",   // U+25AA
            _ => "•",
        };

        float bulletX = pageStyle.MarginStartPts + (style.ListLevel * 18f);
        var fontDesc  = style.Font with { SizeInPoints = style.FontSizePts };

        IReadOnlyList<GlyphRun> runs;
        try { runs = _shaper.Shape(bulletChar, fontDesc); }
        catch { runs = ImmutableArray<GlyphRun>.Empty; }

        if (runs.Count == 0)
            return bulletX + style.FontSizePts * 0.6f + 6f;   // fallback: estimated width

        float bulletWidth = runs.Sum(r => r.TotalAdvance);
        var bRun = runs[0];

        var glyphRun = new GlyphRun(
            bRun.Typeface,
            bRun.SizeInPoints,
            bRun.GlyphIds,
            bRun.Advances,
            null, null);

        var bounds = new RectF(bulletX, yLine, bulletWidth, style.LineHeightPts);
        var origin = new PointF(bulletX, baseline);
        nodes.Add(new GlyphRunNode(bounds, origin,
            ImmutableArray.Create<GlyphRun>(glyphRun), new TextPaint(style.Color)));

        return bulletX + bulletWidth + 6f;
    }
}
