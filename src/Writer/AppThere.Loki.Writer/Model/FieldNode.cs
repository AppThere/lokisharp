// LAYER:   AppThere.Loki.Writer — Model/Inlines
// KIND:    Record + enum (ODF field inline node)
// PURPOSE: Represents an ODF field element (text:title, text:page-number,
//          etc.) as an inline node in the document model. Carries both the
//          FieldKind for Phase 6 live evaluation and the StaticText saved
//          by LibreOffice for Phase 5 static display.
//          Added to the existing InlineNode hierarchy alongside RunNode,
//          HardLineBreakNode, and TabNode.
// DEPENDS: CharacterStyle, InlineNode
// USED BY: BodyParser (ODF import), InlineMeasurer (layout), IFieldEvaluator
// PHASE:   5
// ADR:     ADR-016

using AppThere.Loki.Writer.Model.Styles;

namespace AppThere.Loki.Writer.Model.Inlines;

/// <summary>
/// An ODF field element rendered using its saved static text.
/// Phase 6: IFieldEvaluator replaces StaticText with a live computed value.
/// </summary>
public sealed record FieldNode(
    FieldKind      Kind,
    string         StaticText,   // text LibreOffice last saved into the field
    CharacterStyle Style,
    string?        StyleName)
    : InlineNode;

/// <summary>ODF field element kinds supported in Phase 5.</summary>
public enum FieldKind
{
    Unknown,        // any unrecognised text:* field — StaticText preserved
    Title,          // text:title
    Description,    // text:description
    Subject,        // text:subject
    Author,         // text:initial-creator, text:author-name
    PageNumber,     // text:page-number
    PageCount,      // text:page-count
    Date,           // text:date
    Time,           // text:time
    FileName,       // text:file-name
    Chapter,        // text:chapter
}
