// LAYER:   AppThere.Loki.Writer — Document Model
// KIND:    Records (immutable inline node hierarchy)
// PURPOSE: Inline-level nodes within a ParagraphNode. Mirrors the CSS
//          inline formatting context. All are sealed records.
// DEPENDS: CharacterStyle
// USED BY: ParagraphNode, LayoutEngine (Stage 1 measurement)
// PHASE:   3
// ADR:     ADR-007

using AppThere.Loki.Writer.Model.Styles;

namespace AppThere.Loki.Writer.Model.Inlines;

/// <summary>Abstract base for all inline nodes.</summary>
public abstract record InlineNode;

/// <summary>
/// A text run with a fully-resolved character style.
/// Text is the raw Unicode string — shaping is done by the layout engine.
/// Never empty: ODF importer skips zero-length runs.
/// </summary>
public sealed record RunNode(
    string         Text,
    CharacterStyle Style,
    string?        StyleName)   // original style name, for diagnostics
    : InlineNode;

/// <summary>
/// Hard line break (text:line-break in ODF, w:br in OOXML).
/// Forces a new line within the same paragraph.
/// </summary>
public sealed record HardLineBreakNode : InlineNode;

/// <summary>
/// Tab character. Width resolved to points by the layout engine
/// using the paragraph's tab stop list.
/// Phase 3: treated as a fixed-width space using the default tab width (36pt).
/// </summary>
public sealed record TabNode : InlineNode;
