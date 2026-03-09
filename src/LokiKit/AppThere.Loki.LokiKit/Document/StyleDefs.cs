// LAYER:   AppThere.Loki.LokiKit — Document
// KIND:    Classes (raw style definitions)
// PURPOSE: Raw property definitions for styles. Part of the core model
//          to allow EditCommands to carry style overrides.
// DEPENDS: —
// USED BY: IStyleResolver, StyleRegistry, SetCharacterStyleCommand
// PHASE:   5

namespace AppThere.Loki.LokiKit.Document;

/// <summary>
/// Raw paragraph style definition with optional parent for inheritance.
/// All properties are nullable — null means "inherit from parent".
/// </summary>
public sealed class ParagraphStyleDef
{
    public string   Id       { get; init; } = "";
    public string?  ParentId { get; init; }
    public bool     IsAutomatic { get; init; }

    // Raw property values — all nullable (null = inherit)
    public string?  FontFamily          { get; init; }
    public float?   FontSizePts         { get; init; }
    public string?  Color               { get; init; }
    public string?  Alignment           { get; init; }
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
    public string?  Baseline        { get; init; }
}
