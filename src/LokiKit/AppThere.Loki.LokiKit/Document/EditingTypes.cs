// LAYER:   AppThere.Loki.LokiKit — Document
// KIND:    Records (caret and version model types)
// PURPOSE: Core value types for the document editing model.
//          Part of the Hourglass Waist to allow ILokiEngine and ILokiView
//          to communicate selection and versions across layers.
// DEPENDS: —
// USED BY: IEditCommand, ILokiEngine, ILokiView
// PHASE:   5
// ADR:     ADR-012, ADR-013

namespace AppThere.Loki.LokiKit.Document;

/// <summary>
/// Monotonically increasing document version.
/// Phase 7: replaced by VectorClock without changing command fields.
/// </summary>
public readonly record struct DocumentVersion(int Value)
{
    public DocumentVersion Next() => new(Value + 1);
    public static readonly DocumentVersion Zero = new(0);
    public override string ToString() => $"v{Value}";
}

/// <summary>
/// A logical position within the document model, independent of layout.
/// Identifies the insertion point between two characters.
/// </summary>
public sealed record CaretPosition(
    int  ParagraphIndex,   // index into LokiDocument.Body
    int  RunIndex,         // index into ParagraphNode.Inlines
    int  CharOffset,       // UTF-16 code unit offset within RunNode.Text
    bool IsAtLineEnd)      // disambiguates soft-wrapped line boundary
{
    /// <summary>Position at the very start of the document.</summary>
    public static CaretPosition DocumentStart => new(0, 0, 0, false);

    /// <summary>True when this is a collapsed selection (caret only).</summary>
    public bool IsCollapsed => true; // always true for a single CaretPosition
}

/// <summary>
/// An anchor–focus selection range. When Anchor == Focus the selection
/// is collapsed (caret only). Either direction is valid.
/// </summary>
public sealed record Selection(
    CaretPosition Anchor,
    CaretPosition Focus)
{
    /// <summary>True if no text is selected (Anchor equals Focus).</summary>
    public bool IsCollapsed =>
        Anchor.ParagraphIndex == Focus.ParagraphIndex &&
        Anchor.RunIndex       == Focus.RunIndex       &&
        Anchor.CharOffset     == Focus.CharOffset;

    public static Selection Collapsed(CaretPosition at) => new(at, at);
    public static Selection DocumentStart =>
        Selection.Collapsed(CaretPosition.DocumentStart);
}
