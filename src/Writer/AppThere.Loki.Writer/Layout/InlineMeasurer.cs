// LAYER:   AppThere.Loki.Writer — Layout Engine
// KIND:    Implementation
// PURPOSE: Stage 1 of the layout pipeline (ADR-008). Walks a ParagraphNode's
//          inline content and converts it into a sequence of LayoutItems
//          (Box/Glue/Penalty) ready for Knuth-Plass line breaking.
//          Uses LokiTextShaper for glyph shaping and advance measurement.
//          Does NOT break lines — that is KnuthPlassBreaker's responsibility.
// DEPENDS: IFontManager, ILokiLogger, LokiTextShaper, ParagraphNode, RunNode,
//          HardLineBreakNode, TabNode, MeasuredParagraph, LayoutItem hierarchy,
//          GlyphCluster, FontDescriptor, GlyphRun
// USED BY: LayoutEngine (Stage 1)
// PHASE:   3
// ADR:     ADR-008

using System.Collections.Immutable;
using System.Text;
using AppThere.Loki.Kernel.Fonts;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.Skia.Fonts;
using AppThere.Loki.Skia.Scene;
using AppThere.Loki.Writer.Model;
using AppThere.Loki.Writer.Model.Inlines;

namespace AppThere.Loki.Writer.Layout;

internal sealed class InlineMeasurer
{
    private readonly IFontManager    _fontManager;
    private readonly ILokiLogger     _logger;
    private readonly LokiTextShaper  _shaper;

    public InlineMeasurer(IFontManager fontManager, ILokiLogger logger)
    {
        _fontManager = fontManager ?? throw new ArgumentNullException(nameof(fontManager));
        _logger      = logger      ?? throw new ArgumentNullException(nameof(logger));
        _shaper      = new LokiTextShaper(fontManager, logger);
    }

    /// <summary>
    /// Converts a ParagraphNode into a MeasuredParagraph containing the
    /// Box/Glue/Penalty item sequence required by Knuth-Plass (ADR-008 §2.2).
    /// </summary>
    public MeasuredParagraph Measure(
        ParagraphNode para,
        float         lineWidthPts,
        int           paragraphIndex = 0)
    {
        var items = new List<LayoutItem>();

        foreach (var inline in para.Inlines)
        {
            switch (inline)
            {
                case RunNode run:
                    ProcessRun(run, items);
                    break;

                case HardLineBreakNode:
                    // Forced break within paragraph
                    items.Add(new PenaltyItem(new Penalty(float.NegativeInfinity, false)));
                    break;

                case TabNode:
                    // Fixed 0.5-inch tab; no flex
                    items.Add(new GlueItem(new Glue(36f, 0f, 0f)));
                    break;
            }
        }

        // K-P finishing sequence: infinite-stretch glue + forced end-of-paragraph break
        items.Add(new GlueItem(new Glue(0f, float.PositiveInfinity, 0f)));
        items.Add(new PenaltyItem(new Penalty(float.NegativeInfinity, false)));

        return new MeasuredParagraph(
            paragraphIndex,
            para.Style,
            items.ToImmutableArray(),
            lineWidthPts);
    }

    // ── Run processing ────────────────────────────────────────────────────────

    private void ProcessRun(RunNode run, List<LayoutItem> items)
    {
        // Build a FontDescriptor that reflects the run's resolved size
        var fontDesc = run.Style.Font with { SizeInPoints = run.Style.FontSizePts };

        // Measure one space to set inter-word glue width
        var spaceRuns = ShapeQuiet(" ", fontDesc);
        var spaceWidth = spaceRuns.Count > 0
            ? spaceRuns[0].TotalAdvance
            : run.Style.FontSizePts * 0.25f;   // fallback: ¼ em

        ProcessText(run.Text, fontDesc, spaceWidth, items);
    }

    // ── Text tokenisation ─────────────────────────────────────────────────────

    private void ProcessText(
        string           text,
        FontDescriptor   fontDesc,
        float            spaceWidth,
        List<LayoutItem> items)
    {
        var wordBuffer = new StringBuilder();

        foreach (var ch in text)
        {
            if (ch == ' ')
            {
                if (wordBuffer.Length > 0)
                {
                    EmitWordTokens(wordBuffer.ToString(), fontDesc, items);
                    wordBuffer.Clear();
                }
                // Inter-word glue + neutral break opportunity
                items.Add(new GlueItem(new Glue(spaceWidth, spaceWidth * 0.5f, spaceWidth * 0.3f)));
                items.Add(new PenaltyItem(new Penalty(0f, false)));
            }
            else
            {
                wordBuffer.Append(ch);
            }
        }

        if (wordBuffer.Length > 0)
            EmitWordTokens(wordBuffer.ToString(), fontDesc, items);
    }

    // ── Soft-hyphen splitting ─────────────────────────────────────────────────

    private void EmitWordTokens(
        string           word,
        FontDescriptor   fontDesc,
        List<LayoutItem> items)
    {
        // U+00AD SOFT HYPHEN marks a discouraged-but-allowed break opportunity
        var segments = word.Split('\u00AD');
        for (var i = 0; i < segments.Length; i++)
        {
            if (segments[i].Length > 0)
                EmitBox(segments[i], fontDesc, items);

            if (i < segments.Length - 1)
                items.Add(new PenaltyItem(new Penalty(50f, true)));
        }
    }

    // ── Glyph box emission ────────────────────────────────────────────────────

    private void EmitBox(
        string           text,
        FontDescriptor   fontDesc,
        List<LayoutItem> items)
    {
        var runs = ShapeQuiet(text, fontDesc);
        if (runs.Count == 0) return;

        // Merge all sub-runs (typeface splits) into a single GlyphCluster
        var glyphIdList  = new List<ushort>();
        var advanceList  = new List<float>();
        float totalWidth = 0f;

        foreach (var run in runs)
        {
            foreach (var id in run.GlyphIds)   glyphIdList.Add(id);
            foreach (var adv in run.Advances)   advanceList.Add(adv);
            totalWidth += run.TotalAdvance;
        }

        var cluster = new GlyphCluster(
            glyphIdList.ToArray(),
            advanceList.ToArray(),
            runs[0].Typeface,       // renderer uses first run's typeface
            fontDesc.SizeInPoints);

        items.Add(new BoxItem(new Box(totalWidth, cluster)));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Shape text, returning empty on any failure (never throws).</summary>
    private IReadOnlyList<GlyphRun> ShapeQuiet(string text, FontDescriptor fontDesc)
    {
        try
        {
            return _shaper.Shape(text, fontDesc);
        }
        catch (Exception ex)
        {
            _logger.Warn("InlineMeasurer: shaping failed for '{0}': {1}", text, ex.Message);
            return ImmutableArray<GlyphRun>.Empty;
        }
    }
}
