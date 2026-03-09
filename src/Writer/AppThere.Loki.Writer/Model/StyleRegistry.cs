// LAYER:   AppThere.Loki.Writer — Document Model
// KIND:    Classes (style registry)
// PURPOSE: Holds the raw (pre-cascade) style definitions from the source
//          document. Used by StyleResolver at import time only.
//          The layout engine never reads StyleRegistry — it reads only
//          the computed ParagraphStyle/CharacterStyle on each node.
// DEPENDS: StyleDefs (LokiKit)
// USED BY: OdfImporter (populates), StyleResolver (reads), LokiDocument
// PHASE:   3
// ADR:     ADR-007

using AppThere.Loki.LokiKit.Document;

namespace AppThere.Loki.Writer.Model.Styles;

/// <summary>
/// Immutable registry of all named and automatic styles in the document.
/// Populated by the importer; read by StyleResolver.
/// </summary>
public sealed class StyleRegistry
{
    public IReadOnlyDictionary<string, ParagraphStyleDef> ParagraphStyles { get; }
    public IReadOnlyDictionary<string, CharacterStyleDef> CharacterStyles { get; }
    public string? DefaultParagraphStyleId { get; }
    public string? DefaultCharacterStyleId { get; }

    public StyleRegistry(
        IReadOnlyDictionary<string, ParagraphStyleDef> paragraphStyles,
        IReadOnlyDictionary<string, CharacterStyleDef> characterStyles,
        string? defaultParagraphStyleId,
        string? defaultCharacterStyleId)
    {
        ParagraphStyles        = paragraphStyles;
        CharacterStyles        = characterStyles;
        DefaultParagraphStyleId = defaultParagraphStyleId;
        DefaultCharacterStyleId = defaultCharacterStyleId;
    }

    public static readonly StyleRegistry Empty = new(
        new Dictionary<string, ParagraphStyleDef>(),
        new Dictionary<string, CharacterStyleDef>(),
        null, null);
}

