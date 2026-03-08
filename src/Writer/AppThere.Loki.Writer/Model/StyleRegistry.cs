// LAYER:   AppThere.Loki.Writer — Document Model
// KIND:    Classes (style registry and raw style definitions)
// PURPOSE: Holds the raw (pre-cascade) style definitions from the source
//          document. Used by StyleResolver at import time only.
//          The layout engine never reads StyleRegistry — it reads only
//          the computed ParagraphStyle/CharacterStyle on each node.
// DEPENDS: —
// USED BY: OdfImporter (populates), StyleResolver (reads), LokiDocument
// PHASE:   3
// ADR:     ADR-007

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

/// <summary>
/// Raw paragraph style definition with optional parent for inheritance.
/// All properties are nullable — null means "inherit from parent".
/// </summary>
public sealed class ParagraphStyleDef
{
    public string   Id       { get; init; } = "";
    public string?  ParentId { get; init; }
    public bool     IsAutomatic { get; init; }  // automatic style (per-paragraph override)

    // Raw property values — all nullable (null = inherit)
    public string?  FontFamily          { get; init; }
    public float?   FontSizePts         { get; init; }
    public float?   FontSizePercentage  { get; init; }  // set when fo:font-size is e.g. "150%"
    public string?  Color               { get; init; }  // hex string e.g. "000000"
    public string?  Alignment           { get; init; }  // "left","right","center","justify"
    public float?   MarginTopPts        { get; init; }
    public float?   MarginBottomPts     { get; init; }
    public float?   MarginStartPts      { get; init; }
    public float?   MarginEndPts        { get; init; }
    public float?   LineHeightPts       { get; init; }
    public float?   FirstLineIndentPts  { get; init; }
    public float?   HangingIndentPts    { get; init; }
    public string?  ListStyleId         { get; init; }
    public int?     ListLevel           { get; init; }
    public float?   SpaceBeforePts      { get; init; }
    public float?   SpaceAfterPts       { get; init; }
    public bool?    Bold                { get; init; }
    public bool?    Italic              { get; init; }
}

/// <summary>
/// Raw character style definition with optional parent for inheritance.
/// </summary>
public sealed class CharacterStyleDef
{
    public string   Id       { get; init; } = "";
    public string?  ParentId { get; init; }
    public bool     IsAutomatic { get; init; }

    public string?  FontFamily      { get; init; }
    public float?   FontSizePts     { get; init; }
    public string?  Color           { get; init; }
    public string?  BackgroundColor { get; init; }
    public bool?    Bold            { get; init; }
    public bool?    Italic          { get; init; }
    public bool?    Underline       { get; init; }
    public bool?    Strikethrough   { get; init; }
    public string?  Baseline        { get; init; }  // "normal","super","sub"
}
