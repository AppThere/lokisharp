// LAYER:   AppThere.Loki.Writer — Document Model
// KIND:    Implementation
// PURPOSE: Resolves the CSS-style cascade for paragraph and character styles,
//          producing computed ParagraphStyle / CharacterStyle values.
//          Walks named-style parent chains from root→leaf, then applies
//          direct formatting on top. Does NOT perform layout or painting.
// DEPENDS: IStyleResolver, StyleRegistry, ParagraphStyleDef, CharacterStyleDef,
//          ParagraphStyle, CharacterStyle, IFontManager, ILokiLogger,
//          FontDescriptor, LokiColor, Thickness, ColorParser
// USED BY: OdfImporter
// PHASE:   3
// ADR:     ADR-007

using AppThere.Loki.Kernel.Color;
using AppThere.Loki.Kernel.Fonts;
using AppThere.Loki.Kernel.Geometry;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.Skia.Fonts;

namespace AppThere.Loki.Writer.Model.Styles;

public sealed class StyleResolver : IStyleResolver
{
    private const int MaxChainDepth = 32;

    private readonly StyleRegistry _registry;
    private readonly IFontManager  _fontManager;
    private readonly ILokiLogger   _logger;

    public StyleResolver(StyleRegistry registry, IFontManager fontManager, ILokiLogger logger)
    {
        _registry    = registry;
        _fontManager = fontManager;
        _logger      = logger;
    }

    public ParagraphStyle ResolveParagraph(string? styleId, ParagraphStyleDef? directFormatting)
    {
        var chain = BuildParagraphChain(styleId);

        if (chain.Count == 0 && directFormatting is null)
            return ParagraphStyle.Default;

        var defaults = ParagraphStyle.Default;

        var fontFamily   = FirstStr(directFormatting?.FontFamily,  chain, d => d.FontFamily)
                           ?? defaults.Font.FamilyName;
        var fontSize     = FirstFloat(directFormatting?.FontSizePts, chain, d => d.FontSizePts)
                           ?? defaults.FontSizePts;
        var bold         = FirstBool(directFormatting?.Bold,         chain, d => d.Bold)
                           ?? false;
        var italic       = FirstBool(directFormatting?.Italic,       chain, d => d.Italic)
                           ?? false;

        var font = ResolveFont(fontFamily, fontSize, bold, italic);

        var colorHex  = FirstStr(directFormatting?.Color, chain, d => d.Color);
        var color     = (colorHex is not null ? ColorParser.ParseHex(colorHex) : null)
                        ?? defaults.Color;

        var alignment = ParseAlignment(
            FirstStr(directFormatting?.Alignment, chain, d => d.Alignment))
            ?? defaults.Alignment;

        var marginTop    = FirstFloat(directFormatting?.MarginTopPts,    chain, d => d.MarginTopPts)    ?? defaults.MarginPts.Top;
        var marginBottom = FirstFloat(directFormatting?.MarginBottomPts, chain, d => d.MarginBottomPts) ?? defaults.MarginPts.Bottom;
        var marginStart  = FirstFloat(directFormatting?.MarginStartPts,  chain, d => d.MarginStartPts)  ?? defaults.MarginPts.Left;
        var marginEnd    = FirstFloat(directFormatting?.MarginEndPts,    chain, d => d.MarginEndPts)    ?? defaults.MarginPts.Right;

        var lineHeight       = FirstFloat(directFormatting?.LineHeightPts,      chain, d => d.LineHeightPts)      ?? (fontSize * 1.2f);
        var firstLineIndent  = FirstFloat(directFormatting?.FirstLineIndentPts, chain, d => d.FirstLineIndentPts) ?? defaults.FirstLineIndentPts;
        var hangingIndent    = FirstFloat(directFormatting?.HangingIndentPts,   chain, d => d.HangingIndentPts)   ?? defaults.HangingIndentPts;
        var listStyleId      = FirstStr(directFormatting?.ListStyleId,          chain, d => d.ListStyleId);
        var listLevel        = FirstInt(directFormatting?.ListLevel,            chain, d => d.ListLevel)           ?? defaults.ListLevel;
        var spaceBefore      = FirstFloat(directFormatting?.SpaceBeforePts,     chain, d => d.SpaceBeforePts)     ?? defaults.SpaceBeforePts;
        var spaceAfter       = FirstFloat(directFormatting?.SpaceAfterPts,      chain, d => d.SpaceAfterPts)      ?? defaults.SpaceAfterPts;

        return new ParagraphStyle(
            Font:               font,
            FontSizePts:        fontSize,
            Color:              color,
            Alignment:          alignment,
            MarginPts:          new Thickness(marginStart, marginTop, marginEnd, marginBottom),
            PaddingPts:         defaults.PaddingPts,
            LineHeightPts:      lineHeight,
            FirstLineIndentPts: firstLineIndent,
            HangingIndentPts:   hangingIndent,
            ListStyleId:        listStyleId,
            ListLevel:          listLevel,
            SpaceBeforePts:     spaceBefore,
            SpaceAfterPts:      spaceAfter);
    }

    public CharacterStyle ResolveCharacter(
        string?            styleId,
        CharacterStyleDef? directFormatting,
        ParagraphStyle     containingParagraph)
    {
        var chain = BuildCharacterChain(styleId);

        var fontFamily = FirstStr(directFormatting?.FontFamily, chain, d => d.FontFamily)
                         ?? containingParagraph.Font.FamilyName;
        var fontSize   = FirstFloat(directFormatting?.FontSizePts, chain, d => d.FontSizePts)
                         ?? containingParagraph.FontSizePts;
        var bold       = FirstBool(directFormatting?.Bold,          chain, d => d.Bold)
                         ?? (containingParagraph.Font.Weight >= FontWeight.Bold);
        var italic     = FirstBool(directFormatting?.Italic,        chain, d => d.Italic)
                         ?? (containingParagraph.Font.Slant == FontSlant.Italic);

        var font = ResolveFont(fontFamily, fontSize, bold, italic);

        var colorHex   = FirstStr(directFormatting?.Color, chain, d => d.Color);
        var color      = (colorHex is not null ? ColorParser.ParseHex(colorHex) : null)
                         ?? containingParagraph.Color;

        var bgColorHex = FirstStr(directFormatting?.BackgroundColor, chain, d => d.BackgroundColor);
        var bgColor    = bgColorHex is not null ? ColorParser.ParseHex(bgColorHex) : null;

        var underline      = FirstBool(directFormatting?.Underline,     chain, d => d.Underline)     ?? false;
        var strikethrough  = FirstBool(directFormatting?.Strikethrough, chain, d => d.Strikethrough) ?? false;
        var baseline       = ParseBaseline(FirstStr(directFormatting?.Baseline, chain, d => d.Baseline))
                             ?? TextBaseline.Normal;

        return new CharacterStyle(
            Font:            font,
            FontSizePts:     fontSize,
            Color:           color,
            BackgroundColor: bgColor,
            Bold:            bold,
            Italic:          italic,
            Underline:       underline,
            Strikethrough:   strikethrough,
            Baseline:        baseline);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private List<ParagraphStyleDef> BuildParagraphChain(string? styleId)
    {
        var effectiveId = styleId;
        if (effectiveId is null || !_registry.ParagraphStyles.ContainsKey(effectiveId))
            effectiveId = _registry.DefaultParagraphStyleId;

        if (effectiveId is null || !_registry.ParagraphStyles.TryGetValue(effectiveId, out _))
            return [];

        var chain = new List<ParagraphStyleDef>();
        var seen  = new HashSet<string>(StringComparer.Ordinal);
        var current = effectiveId;

        while (current is not null && _registry.ParagraphStyles.TryGetValue(current, out var def))
        {
            if (!seen.Add(current) || chain.Count >= MaxChainDepth)
            {
                _logger.Warn("Paragraph style cycle or depth limit reached at style '{0}'", current);
                break;
            }
            chain.Add(def);
            current = def.ParentId;
        }

        return chain; // chain[0]=leaf, chain[^1]=root
    }

    private List<CharacterStyleDef> BuildCharacterChain(string? styleId)
    {
        var effectiveId = styleId;
        if (effectiveId is null || !_registry.CharacterStyles.ContainsKey(effectiveId))
            effectiveId = _registry.DefaultCharacterStyleId;

        if (effectiveId is null || !_registry.CharacterStyles.TryGetValue(effectiveId, out _))
            return [];

        var chain = new List<CharacterStyleDef>();
        var seen  = new HashSet<string>(StringComparer.Ordinal);
        var current = effectiveId;

        while (current is not null && _registry.CharacterStyles.TryGetValue(current, out var def))
        {
            if (!seen.Add(current) || chain.Count >= MaxChainDepth)
            {
                _logger.Warn("Character style cycle or depth limit reached at style '{0}'", current);
                break;
            }
            chain.Add(def);
            current = def.ParentId;
        }

        return chain;
    }

    private FontDescriptor ResolveFont(string familyName, float sizePts, bool bold, bool italic)
    {
        var weight     = bold   ? FontWeight.Bold  : FontWeight.Regular;
        var slant      = italic ? FontSlant.Italic : FontSlant.Upright;
        var descriptor = new FontDescriptor(familyName, weight, slant, FontStretch.Normal, sizePts);

        if (!_fontManager.TryGetTypeface(descriptor, out _))
            return FontDescriptor.Default with { SizeInPoints = sizePts };

        return descriptor;
    }

    private static string? FirstStr(string? direct, List<ParagraphStyleDef> chain, Func<ParagraphStyleDef, string?> sel)
    {
        if (direct is not null) return direct;
        foreach (var d in chain) { var v = sel(d); if (v is not null) return v; }
        return null;
    }

    private static string? FirstStr(string? direct, List<CharacterStyleDef> chain, Func<CharacterStyleDef, string?> sel)
    {
        if (direct is not null) return direct;
        foreach (var d in chain) { var v = sel(d); if (v is not null) return v; }
        return null;
    }

    private static float? FirstFloat(float? direct, List<ParagraphStyleDef> chain, Func<ParagraphStyleDef, float?> sel)
    {
        if (direct.HasValue) return direct;
        foreach (var d in chain) { var v = sel(d); if (v.HasValue) return v; }
        return null;
    }

    private static float? FirstFloat(float? direct, List<CharacterStyleDef> chain, Func<CharacterStyleDef, float?> sel)
    {
        if (direct.HasValue) return direct;
        foreach (var d in chain) { var v = sel(d); if (v.HasValue) return v; }
        return null;
    }

    private static bool? FirstBool(bool? direct, List<ParagraphStyleDef> chain, Func<ParagraphStyleDef, bool?> sel)
    {
        if (direct.HasValue) return direct;
        foreach (var d in chain) { var v = sel(d); if (v.HasValue) return v; }
        return null;
    }

    private static bool? FirstBool(bool? direct, List<CharacterStyleDef> chain, Func<CharacterStyleDef, bool?> sel)
    {
        if (direct.HasValue) return direct;
        foreach (var d in chain) { var v = sel(d); if (v.HasValue) return v; }
        return null;
    }

    private static int? FirstInt(int? direct, List<ParagraphStyleDef> chain, Func<ParagraphStyleDef, int?> sel)
    {
        if (direct.HasValue) return direct;
        foreach (var d in chain) { var v = sel(d); if (v.HasValue) return v; }
        return null;
    }

    private static TextAlignment? ParseAlignment(string? value) =>
        value?.ToLowerInvariant() switch
        {
            "left"    => TextAlignment.Left,
            "right"   => TextAlignment.Right,
            "center"  => TextAlignment.Centre,
            "centre"  => TextAlignment.Centre,
            "justify" => TextAlignment.Justify,
            _         => null
        };

    private static TextBaseline? ParseBaseline(string? value) =>
        value?.ToLowerInvariant() switch
        {
            "super"  => TextBaseline.Superscript,
            "sub"    => TextBaseline.Subscript,
            "normal" => TextBaseline.Normal,
            _        => null
        };
}
