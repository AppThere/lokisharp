// LAYER:   AppThere.Loki.Writer — Layout Engine
// KIND:    Implementation
// PURPOSE: Stage 1 of the layout pipeline (ADR-008). Walks a ParagraphNode's
//          inline content and converts it into a sequence of LayoutItems
//          (Box/Glue/Penalty) ready for Knuth-Plass line breaking.
//          Does NOT break lines — that is KnuthPlassBreaker's responsibility.
//          Does NOT use HarfBuzz shaping — uses SKFont glyph metrics directly.
// DEPENDS: IFontManager, ILokiLogger, ParagraphNode, RunNode, HardLineBreakNode,
//          TabNode, MeasuredParagraph, LayoutItem hierarchy, GlyphCluster
// USED BY: LayoutEngine (Stage 1)
// PHASE:   3
// ADR:     ADR-008

using System.Collections.Immutable;
using System.Text;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.Skia.Fonts;
using AppThere.Loki.Writer.Model;
using AppThere.Loki.Writer.Model.Inlines;
using SkiaSharp;

namespace AppThere.Loki.Writer.Layout;

internal sealed class InlineMeasurer
{
    private readonly IFontManager _fontManager;
    private readonly ILokiLogger  _logger;

    public InlineMeasurer(IFontManager fontManager, ILokiLogger logger)
    {
        _fontManager = fontManager ?? throw new ArgumentNullException(nameof(fontManager));
        _logger      = logger      ?? throw new ArgumentNullException(nameof(logger));
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
        if (!_fontManager.TryGetTypeface(run.Style.Font, out var lokiTypeface)
            || lokiTypeface is null)
        {
            _logger.Warn("Font '{0}' not resolved; falling back to Latin fallback.",
                run.Style.Font.FamilyName);
            lokiTypeface = _fontManager.GetFallbackForScript(UnicodeScript.Latin);
        }

        // Resolve SKTypeface from ILokiTypeface by family name.
        // Avoids InternalsVisibleTo dependency on SkiaTypeface.Inner.
        var matchedSk = SKFontManager.Default.MatchFamily(lokiTypeface.FamilyName);
        var skTypeface = matchedSk ?? SKTypeface.Default;
        try
        {
            using var skFont = new SKFont(skTypeface, run.Style.FontSizePts);
            var spaceWidth = skFont.MeasureText(" ");
            ProcessText(run.Text, lokiTypeface, run.Style.FontSizePts,
                        skFont, spaceWidth, items);
        }
        finally
        {
            matchedSk?.Dispose();
        }
    }

    // ── Text tokenisation ─────────────────────────────────────────────────────

    private static void ProcessText(
        string         text,
        ILokiTypeface  typeface,
        float          fontSize,
        SKFont         skFont,
        float          spaceWidth,
        List<LayoutItem> items)
    {
        var wordBuffer = new StringBuilder();

        foreach (var ch in text)
        {
            if (ch == ' ')
            {
                if (wordBuffer.Length > 0)
                {
                    EmitWordTokens(wordBuffer.ToString(), typeface, fontSize, skFont, items);
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
            EmitWordTokens(wordBuffer.ToString(), typeface, fontSize, skFont, items);
    }

    // ── Soft-hyphen splitting ─────────────────────────────────────────────────

    private static void EmitWordTokens(
        string         word,
        ILokiTypeface  typeface,
        float          fontSize,
        SKFont         skFont,
        List<LayoutItem> items)
    {
        // U+00AD SOFT HYPHEN marks a discouraged-but-allowed break opportunity
        var segments = word.Split('\u00AD');
        for (var i = 0; i < segments.Length; i++)
        {
            if (segments[i].Length > 0)
                EmitBox(segments[i], typeface, fontSize, skFont, items);

            if (i < segments.Length - 1)
                items.Add(new PenaltyItem(new Penalty(50f, true)));
        }
    }

    // ── Glyph box emission ────────────────────────────────────────────────────

    private static void EmitBox(
        string         text,
        ILokiTypeface  typeface,
        float          fontSize,
        SKFont         skFont,
        List<LayoutItem> items)
    {
        var glyphIds = skFont.GetGlyphs(text);
        var advances = skFont.GetGlyphWidths(glyphIds);
        var totalWidth = 0f;
        foreach (var a in advances) totalWidth += a;

        var cluster = new GlyphCluster(glyphIds, advances, typeface, fontSize);
        items.Add(new BoxItem(new Box(totalWidth, cluster)));
    }
}
