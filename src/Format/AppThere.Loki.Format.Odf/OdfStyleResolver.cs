// LAYER:   AppThere.Loki.Format.Odf — ODF Import
// KIND:    Implementation
// PURPOSE: Implements IStyleResolver for ODF documents. Cascades named styles
//          through their parent chain, then applies direct formatting overrides,
//          to produce fully-resolved ParagraphStyle and CharacterStyle values.
//          Cascade order: document default → parent chain (root first) → direct.
//          Only reads StyleRegistry — never writes to it.
// DEPENDS: IStyleResolver, StyleRegistry, ParagraphStyleDef, CharacterStyleDef,
//          ParagraphStyle, CharacterStyle, FontDescriptor, LokiColor, Thickness
// USED BY: OdfDocumentParser (Pass 3)
// PHASE:   3
// ADR:     ADR-007, ADR-009

using AppThere.Loki.Kernel.Color;
using AppThere.Loki.Kernel.Fonts;
using AppThere.Loki.Kernel.Geometry;
using AppThere.Loki.Writer.Model.Styles;

namespace AppThere.Loki.Format.Odf;

internal sealed class OdfStyleResolver : IStyleResolver
{
    private readonly StyleRegistry _registry;

    public OdfStyleResolver(StyleRegistry registry) => _registry = registry;

    // ── IStyleResolver ────────────────────────────────────────────────────────

    public ParagraphStyle ResolveParagraph(
        string? styleId, ParagraphStyleDef? directFormatting)
    {
        var chain = BuildParaChain(styleId);
        var style = ParagraphStyle.Default;
        foreach (var def in chain) style = ApplyPara(style, def);
        if (directFormatting is not null) style = ApplyPara(style, directFormatting);
        return style;
    }

    public CharacterStyle ResolveCharacter(
        string? styleId, CharacterStyleDef? directFormatting, ParagraphStyle containingParagraph)
    {
        // Inherit base character properties from containing paragraph
        var @base = new CharacterStyle(
            Font:            containingParagraph.Font,
            FontSizePts:     containingParagraph.FontSizePts,
            Color:           containingParagraph.Color,
            BackgroundColor: null,
            Bold:            containingParagraph.Font.Weight >= FontWeight.Bold,
            Italic:          containingParagraph.Font.Slant  != FontSlant.Upright,
            Underline:       false,
            Strikethrough:   false,
            Baseline:        TextBaseline.Normal);

        var chain = BuildCharChain(styleId);
        foreach (var def in chain) @base = ApplyChar(@base, def);
        if (directFormatting is not null) @base = ApplyChar(@base, directFormatting);

        // Synchronise Font.Weight with the resolved Bold flag so IFontManager
        // receives a descriptor that correctly requests the bold variant.
        if (@base.Bold && @base.Font.Weight < FontWeight.Bold)
            @base = @base with { Font = @base.Font with { Weight = FontWeight.Bold } };
        else if (!@base.Bold && @base.Font.Weight >= FontWeight.Bold)
            @base = @base with { Font = @base.Font with { Weight = FontWeight.Regular } };

        return @base;
    }

    /// <summary>
    /// Returns the first non-automatic style ID in the parent chain for the given
    /// style ID. Used by the parser to store a human-readable style name on nodes.
    /// </summary>
    public string? ResolveNamedStyleId(string? styleId)
    {
        if (styleId is null) return null;
        var current = styleId;
        while (current is not null)
        {
            if (_registry.ParagraphStyles.TryGetValue(current, out var def))
            {
                if (!def.IsAutomatic) return current;
                current = def.ParentId;
            }
            else break;
        }
        return styleId;
    }

    // ── Para cascade ──────────────────────────────────────────────────────────

    private List<ParagraphStyleDef> BuildParaChain(string? styleId)
    {
        var chain = new List<ParagraphStyleDef>();
        if (styleId is null) return chain;

        // Walk parent chain up to root (prevent loops)
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var current = styleId;
        while (current is not null && visited.Add(current) && chain.Count < 30)
        {
            if (_registry.ParagraphStyles.TryGetValue(current, out var def))
            {
                chain.Add(def);
                current = def.ParentId;
            }
            else break;
        }
        chain.Reverse(); // root-first
        return chain;
    }

    private static ParagraphStyle ApplyPara(ParagraphStyle s, ParagraphStyleDef d)
    {
        var font = s.Font;
        if (d.FontFamily is not null)
            font = font with { FamilyName = d.FontFamily };
        if (d.Bold.HasValue || d.Italic.HasValue)
        {
            var weight  = d.Bold  == true ? FontWeight.Bold    : font.Weight;
            var slant   = d.Italic == true ? FontSlant.Italic  : font.Slant;
            font = font with { Weight = weight, Slant = slant };
        }

        // Resolve font size: percentage is relative to the running inherited size (default 12pt).
        float fontSize;
        if (d.FontSizePercentage.HasValue)
            fontSize = s.FontSizePts * (d.FontSizePercentage.Value / 100f);
        else
            fontSize = d.FontSizePts ?? s.FontSizePts;

        return s with
        {
            Font              = font,
            FontSizePts       = fontSize,
            Color             = ParseColor(d.Color) ?? s.Color,
            Alignment         = ParseAlignment(d.Alignment) ?? s.Alignment,
            MarginPts         = ApplyThickness(s.MarginPts,
                                    d.MarginTopPts, d.MarginBottomPts,
                                    d.MarginStartPts, d.MarginEndPts),
            LineHeightPts     = d.LineHeightPts  ?? s.LineHeightPts,
            FirstLineIndentPts = d.FirstLineIndentPts ?? s.FirstLineIndentPts,
            HangingIndentPts  = d.HangingIndentPts   ?? s.HangingIndentPts,
            ListStyleId       = d.ListStyleId    ?? s.ListStyleId,
            ListLevel         = d.ListLevel      ?? s.ListLevel,
            SpaceBeforePts    = d.SpaceBeforePts ?? s.SpaceBeforePts,
            SpaceAfterPts     = d.SpaceAfterPts  ?? s.SpaceAfterPts,
        };
    }

    // ── Char cascade ──────────────────────────────────────────────────────────

    private List<CharacterStyleDef> BuildCharChain(string? styleId)
    {
        var chain = new List<CharacterStyleDef>();
        if (styleId is null) return chain;

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var current = styleId;
        while (current is not null && visited.Add(current) && chain.Count < 30)
        {
            if (_registry.CharacterStyles.TryGetValue(current, out var def))
            {
                chain.Add(def);
                current = def.ParentId;
            }
            else break;
        }
        chain.Reverse();
        return chain;
    }

    private static CharacterStyle ApplyChar(CharacterStyle s, CharacterStyleDef d)
    {
        var font = s.Font;
        if (d.FontFamily is not null) font = font with { FamilyName = d.FontFamily };
        var bold   = d.Bold   ?? s.Bold;
        var italic = d.Italic ?? s.Italic;
        if (d.Bold.HasValue || d.Italic.HasValue)
        {
            font = font with
            {
                Weight = bold   ? FontWeight.Bold   : FontWeight.Regular,
                Slant  = italic ? FontSlant.Italic  : FontSlant.Upright,
            };
        }

        return s with
        {
            Font            = font,
            FontSizePts     = d.FontSizePts     ?? s.FontSizePts,
            Color           = ParseColor(d.Color)           ?? s.Color,
            BackgroundColor = ParseColor(d.BackgroundColor) ?? s.BackgroundColor,
            Bold            = bold,
            Italic          = italic,
            Underline       = d.Underline       ?? s.Underline,
            Strikethrough   = d.Strikethrough   ?? s.Strikethrough,
            Baseline        = ParseBaseline(d.Baseline) ?? s.Baseline,
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static LokiColor? ParseColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return null;
        try { return LokiColor.FromHex(hex); }
        catch { return null; }
    }

    private static TextAlignment? ParseAlignment(string? value) =>
        value?.ToLowerInvariant() switch
        {
            "justify" => TextAlignment.Justify,
            "right"   => TextAlignment.Right,
            "center"  => TextAlignment.Centre,
            "centre"  => TextAlignment.Centre,
            "left"    => TextAlignment.Left,
            _         => null,
        };

    private static TextBaseline? ParseBaseline(string? value) =>
        value?.ToLowerInvariant() switch
        {
            "super" => TextBaseline.Superscript,
            "sub"   => TextBaseline.Subscript,
            "normal"=> TextBaseline.Normal,
            _       => null,
        };

    private static Thickness ApplyThickness(
        Thickness current,
        float? top, float? bottom, float? start, float? end) =>
        new(start  ?? current.Left,
            top    ?? current.Top,
            end    ?? current.Right,
            bottom ?? current.Bottom);
}
