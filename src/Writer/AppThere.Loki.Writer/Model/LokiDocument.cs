// LAYER:   AppThere.Loki.Writer — Document Model
// KIND:    Records (immutable document tree)
// PURPOSE: Root document type and block-level node hierarchy.
//          LokiDocument is the immutable snapshot produced by ODF/OOXML import
//          and consumed by the layout engine. All nodes are sealed records.
//          Mutations produce new trees via record with-expressions.
// DEPENDS: ParagraphStyle, CharacterStyle, StyleRegistry, InlineNode hierarchy
// USED BY: LayoutEngine, WriterEngine, OdfImporter, tests
// PHASE:   3
// ADR:     ADR-007

using System.Collections.Immutable;
using AppThere.Loki.Writer.Model.Styles;
using AppThere.Loki.Writer.Model.Inlines;

namespace AppThere.Loki.Writer.Model;

/// <summary>
/// Immutable snapshot of a Writer document. Produced by OdfImporter;
/// consumed by LayoutEngine. Never mutated after construction —
/// WriterEngine wraps it in DocumentState for version tracking.
/// </summary>
public sealed record LokiDocument(
    ImmutableList<BlockNode>  Body,
    StyleRegistry             Styles,
    PageStyle                 DefaultPageStyle,
    string?                   Title,
    string?                   Language)
{
    /// <summary>
    /// Monotonic counter stamped by WriterEngine before each layout pass.
    /// LayoutEngine uses this as the docVersion key in LayoutCache.
    /// 0 = initial (never laid out).
    /// </summary>
    internal int LayoutVersion { get; init; } = 0;

    public static LokiDocument Empty => new(
        ImmutableList<BlockNode>.Empty,
        StyleRegistry.Empty,
        PageStyle.A4,
        null, null);
}

/// <summary>Abstract base for all block-level nodes.</summary>
public abstract record BlockNode;

/// <summary>
/// A paragraph — the primary block container. Holds an ordered list
/// of inline nodes and a fully-resolved ParagraphStyle.
/// </summary>
public sealed record ParagraphNode(
    ImmutableList<InlineNode> Inlines,
    ParagraphStyle            Style,
    string?                   StyleName)     // original style name, for diagnostics
    : BlockNode;

/// <summary>
/// Page geometry derived from ODF page style.
/// Phase 3: all pages use DefaultPageStyle. Variable page styles Phase 4+.
/// </summary>
public sealed record PageStyle(
    float  WidthPts,
    float  HeightPts,
    float  MarginTopPts,
    float  MarginBottomPts,
    float  MarginStartPts,
    float  MarginEndPts)
{
    public float ContentWidthPts  => WidthPts  - MarginStartPts - MarginEndPts;
    public float ContentHeightPts => HeightPts - MarginTopPts   - MarginBottomPts;

    public static readonly PageStyle A4 = new(595.28f, 841.89f, 56.7f, 56.7f, 56.7f, 56.7f);
}
