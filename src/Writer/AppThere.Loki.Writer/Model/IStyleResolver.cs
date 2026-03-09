// LAYER:   AppThere.Loki.Writer — Document Model
// KIND:    Interface + implementation contract
// PURPOSE: Resolves the full CSS-style cascade for a paragraph or run,
//          producing computed ParagraphStyle / CharacterStyle values.
//          Called by OdfImporter during Pass 3 — never by the layout engine.
//          The cascade order: document default → named style chain → direct formatting.
// DEPENDS: StyleRegistry, ParagraphStyleDef, CharacterStyleDef, IFontManager
// USED BY: OdfImporter
// PHASE:   3
// ADR:     ADR-007

using AppThere.Loki.Skia.Fonts;
using AppThere.Loki.LokiKit.Document;

namespace AppThere.Loki.Writer.Model.Styles;

public interface IStyleResolver
{
    /// <summary>
    /// Resolve the full cascade for a paragraph with the given style name
    /// and optional direct formatting overrides.
    /// styleId: the style:name attribute on text:p (may be null → use default).
    /// directFormatting: properties set directly on the element, overriding the style.
    /// </summary>
    ParagraphStyle ResolveParagraph(
        string?           styleId,
        ParagraphStyleDef? directFormatting);

    /// <summary>
    /// Resolve the full cascade for a character run with the given style name
    /// and optional direct formatting, inheriting from the containing paragraph.
    /// </summary>
    CharacterStyle ResolveCharacter(
        string?           styleId,
        CharacterStyleDef? directFormatting,
        ParagraphStyle    containingParagraph);
}
