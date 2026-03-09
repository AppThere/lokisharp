// LAYER:   AppThere.Loki.Writer — Layout Engine
// KIND:    Records (K-P primitive types and intermediate layout results)
// PURPOSE: Data types used by the four-stage layout pipeline (ADR-008).
//          Stage 1 produces MeasuredParagraph (sequences of LayoutBox).
//          Stage 2 (K-P) produces BrokenParagraph (lines with break points).
//          Stage 3 produces PageLayout (paragraphs assigned to pages).
// DEPENDS: GlyphCluster, ParagraphStyle
// USED BY: LayoutEngine, KnuthPlassBreaker, PageBreaker, PaintSceneBuilder
// PHASE:   3
// ADR:     ADR-008

using System.Collections.Immutable;
using AppThere.Loki.Writer.Model.Styles;
using AppThere.Loki.Skia.Fonts;

namespace AppThere.Loki.Writer.Layout;

// ── K-P primitives ───────────────────────────────────────────────────────────

/// <summary>Fixed-width content — cannot break.</summary>
public sealed record Box(
    float        Width,
    GlyphCluster Glyphs,
    int          RunIndex,
    int          RunOffset,
    string       Text);

/// <summary>
/// Flexible space. Ideal width with stretch/shrink tolerance.
/// Inter-word spaces and tab stops are Glues.
/// </summary>
public sealed record Glue(
    float Width,
    float Stretch,
    float Shrink);

/// <summary>
/// Break opportunity with cost.
/// Cost = 0: neutral. &lt; 0: encouraged. &gt; 0: discouraged.
/// Cost = float.PositiveInfinity: forbidden. float.NegativeInfinity: forced.
/// Flagged = true: hyphenated break (adjacent flagged breaks incur extra demerits).
/// </summary>
public sealed record Penalty(
    float Cost,
    bool  Flagged);

/// <summary>Union of Box, Glue, and Penalty for the item sequence.</summary>
public abstract record LayoutItem;
public sealed record BoxItem(Box Box)         : LayoutItem;
public sealed record GlueItem(Glue Glue)      : LayoutItem;
public sealed record PenaltyItem(Penalty Pen) : LayoutItem;

// ── Stage 1 output ───────────────────────────────────────────────────────────

/// <summary>
/// A paragraph after inline measurement (Stage 1).
/// Contains the full LayoutItem sequence ready for K-P.
/// </summary>
public sealed record MeasuredParagraph(
    int                        ParagraphIndex,
    ParagraphStyle             Style,
    ImmutableArray<LayoutItem> Items,
    float                      LineWidthPts);   // available width for this paragraph

// ── Stage 2 output ───────────────────────────────────────────────────────────

/// <summary>
/// A paragraph after line breaking (Stage 2).
/// Contains the optimal break sequence and per-line adjustment ratios.
/// </summary>
public sealed record BrokenParagraph(
    int                      ParagraphIndex,
    ParagraphStyle           Style,
    ImmutableArray<BrokenLine> Lines);

/// <summary>One line within a broken paragraph.</summary>
public sealed record BrokenLine(
    ImmutableArray<LayoutItem> Items,
    float                      AdjustmentRatio,  // r: negative=tight, 0=ideal, positive=loose
    bool                       IsForcedBreak,
    bool                       IsLastLine);

// ── Stage 3 output ───────────────────────────────────────────────────────────

/// <summary>All paragraphs assigned to pages with y-offsets.</summary>
public sealed record PageLayout(
    int                           PageIndex,
    ImmutableArray<PlacedParagraph> Paragraphs);

/// <summary>A paragraph placed at a specific y-offset on a page.</summary>
public sealed record PlacedParagraph(
    BrokenParagraph Paragraph,
    float           YOffsetPts);   // from page content top (after margin)

// ── Glyph cluster ────────────────────────────────────────────────────────────

/// <summary>
/// A shaped sequence of glyphs for one Box.
/// Carries the glyph IDs, advances, and the typeface needed for rendering.
/// Produced by LokiTextShaper in Stage 1.
/// </summary>
public sealed record GlyphCluster(
    ushort[]          GlyphIds,
    float[]           Advances,
    ILokiTypeface     Typeface,
    float             FontSizePts);
