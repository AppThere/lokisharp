// LAYER:   AppThere.Loki.Format.Odf — ODF Import
// KIND:    Implementation
// PURPOSE: ODF import Passes 1 and 2 — parses style:style and
//          style:default-style elements from office:styles (named styles)
//          and office:automatic-styles (per-paragraph overrides), producing
//          an immutable StyleRegistry consumed by StyleResolver in Pass 3.
//          Also resolves font face declarations (office:font-face-decls).
//          Does NOT perform cascade resolution or body parsing.
// DEPENDS: OdfNamespaces, OdfUnits, StyleRegistry, ParagraphStyleDef,
//          CharacterStyleDef, ILokiLogger
// USED BY: OdfImporter
// PHASE:   3
// ADR:     ADR-009

using System.Xml.Linq;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.Writer.Model;
using AppThere.Loki.Writer.Model.Styles;

namespace AppThere.Loki.Format.Odf;

internal sealed class StyleParser
{
    private readonly ILokiLogger _logger;
    private Dictionary<string, string> _fontFaces = new(StringComparer.Ordinal);

    public StyleParser(ILokiLogger logger) => _logger = logger;

    /// <summary>
    /// Build a StyleRegistry from contentDoc (content.xml or full FODT doc)
    /// and optional stylesDoc (styles.xml — null for FODT flat files).
    /// Pass 1 reads named styles; Pass 2 reads automatic styles.
    /// </summary>
    public StyleRegistry ParseStyleRegistry(XDocument contentDoc, XDocument? stylesDoc)
    {
        _fontFaces = new Dictionary<string, string>(StringComparer.Ordinal);

        var paraStyles = new Dictionary<string, ParagraphStyleDef>(StringComparer.Ordinal);
        var charStyles = new Dictionary<string, CharacterStyleDef>(StringComparer.Ordinal);
        string? defaultParaId = null;
        string? defaultCharId  = null;

        // Collect font face declarations from both documents
        CollectFontFaces(contentDoc.Root);
        if (stylesDoc != null) CollectFontFaces(stylesDoc.Root);

        // Pass 1 — named styles (from stylesDoc if present, else contentDoc)
        var namedRoot = stylesDoc?.Root ?? contentDoc.Root;
        foreach (var el in StyleElements(namedRoot, "styles"))
            ParseStyle(el, isAutomatic: false, paraStyles, charStyles);

        // Pass 1b — default styles (from same root as named styles)
        foreach (var el in DefaultStyleElements(namedRoot))
            ParseDefaultStyle(el, paraStyles, charStyles, ref defaultParaId, ref defaultCharId);

        // Pass 2 — automatic styles (always from contentDoc)
        foreach (var el in StyleElements(contentDoc.Root, "automatic-styles"))
            ParseStyle(el, isAutomatic: true, paraStyles, charStyles);

        return new StyleRegistry(paraStyles, charStyles, defaultParaId, defaultCharId);
    }

    // ── XML traversal helpers ─────────────────────────────────────────────────

    private static IEnumerable<XElement> StyleElements(XElement? root, string section) =>
        root?.Element(OdfNamespaces.Office + section)
            ?.Elements(OdfNamespaces.Style + "style")
        ?? [];

    private static IEnumerable<XElement> DefaultStyleElements(XElement? root) =>
        root?.Element(OdfNamespaces.Office + "styles")
            ?.Elements(OdfNamespaces.Style + "default-style")
        ?? [];

    private void CollectFontFaces(XElement? root)
    {
        if (root == null) return;
        var decls = root.Element(OdfNamespaces.Office + "font-face-decls");
        if (decls == null) return;
        foreach (var ff in decls.Elements(OdfNamespaces.Style + "font-face"))
        {
            var n = Attr(ff, OdfNamespaces.Style, "name");
            var f = Attr(ff, OdfNamespaces.Svg,   "font-family")?.Trim('"');
            if (n != null && f != null) _fontFaces[n] = f;
        }
    }

    // ── Style element processing ──────────────────────────────────────────────

    private void ParseStyle(
        XElement el, bool isAutomatic,
        Dictionary<string, ParagraphStyleDef> para,
        Dictionary<string, CharacterStyleDef> chars)
    {
        try
        {
            var name   = Attr(el, OdfNamespaces.Style, "name");
            if (name == null) return;
            var family = Attr(el, OdfNamespaces.Style, "family") ?? "";
            var parent = Attr(el, OdfNamespaces.Style, "parent-style-name");

            if (family == "paragraph")
                para[name] = MakeParaDef(name, parent, isAutomatic, el);
            else if (family == "text")
                chars[name] = MakeCharDef(name, parent, isAutomatic, el);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
        {
            _logger.Warn("Skipping malformed style element: {0}", ex.Message);
        }
    }

    private void ParseDefaultStyle(
        XElement el,
        Dictionary<string, ParagraphStyleDef> para,
        Dictionary<string, CharacterStyleDef> chars,
        ref string? defaultParaId,
        ref string? defaultCharId)
    {
        var family = Attr(el, OdfNamespaces.Style, "family") ?? "";
        var name   = Attr(el, OdfNamespaces.Style, "name")
                     ?? (family == "paragraph" ? "__default_paragraph__" : "__default_text__");

        if (family == "paragraph")
        {
            para[name]   = MakeParaDef(name, null, false, el);
            defaultParaId = name;
        }
        else if (family == "text")
        {
            chars[name]  = MakeCharDef(name, null, false, el);
            defaultCharId = name;
        }
    }

    // ── Def builders ─────────────────────────────────────────────────────────

    private ParagraphStyleDef MakeParaDef(
        string name, string? parent, bool isAuto, XElement el)
    {
        var pp = el.Element(OdfNamespaces.Style + "paragraph-properties");
        var tp = el.Element(OdfNamespaces.Style + "text-properties");

        var rawIndent = ParseLength(pp, OdfNamespaces.FoText, "text-indent");

        return new ParagraphStyleDef
        {
            Id                = name,
            ParentId          = parent,
            IsAutomatic       = isAuto,
            Alignment         = Attr(pp, OdfNamespaces.FoText, "text-align"),
            SpaceBeforePts    = ParseLength(pp, OdfNamespaces.FoText, "margin-top"),
            SpaceAfterPts     = ParseLength(pp, OdfNamespaces.FoText, "margin-bottom"),
            MarginStartPts    = ParseLength(pp, OdfNamespaces.FoText, "margin-left"),
            MarginEndPts      = ParseLength(pp, OdfNamespaces.FoText, "margin-right"),
            LineHeightPts     = ParseLength(pp, OdfNamespaces.FoText, "line-height"),
            FirstLineIndentPts = rawIndent > 0 ? rawIndent : null,
            HangingIndentPts  = rawIndent < 0 ? -rawIndent : null,
            ListStyleId       = Attr(pp, OdfNamespaces.Text, "list-style-name"),
            ListLevel         = ParseInt(Attr(pp, OdfNamespaces.Text, "outline-level"), subtract: 1),
            FontSizePts       = ParseLength(tp, OdfNamespaces.FoText, "font-size"),
            Color             = StripHash(Attr(tp, OdfNamespaces.FoText, "color")),
            Bold              = ParseBold(Attr(tp, OdfNamespaces.FoText, "font-weight")),
            Italic            = ParseItalic(Attr(tp, OdfNamespaces.FoText, "font-style")),
            FontFamily        = ResolveFontFamily(Attr(tp, OdfNamespaces.Style, "font-name")),
        };
    }

    private CharacterStyleDef MakeCharDef(
        string name, string? parent, bool isAuto, XElement el)
    {
        var tp = el.Element(OdfNamespaces.Style + "text-properties");

        return new CharacterStyleDef
        {
            Id              = name,
            ParentId        = parent,
            IsAutomatic     = isAuto,
            FontFamily      = ResolveFontFamily(Attr(tp, OdfNamespaces.Style, "font-name")),
            FontSizePts     = ParseLength(tp, OdfNamespaces.FoText, "font-size"),
            Color           = StripHash(Attr(tp, OdfNamespaces.FoText, "color")),
            BackgroundColor = StripHash(Attr(tp, OdfNamespaces.FoText, "background-color")),
            Bold            = ParseBold(Attr(tp, OdfNamespaces.FoText, "font-weight")),
            Italic          = ParseItalic(Attr(tp, OdfNamespaces.FoText, "font-style")),
            Underline       = ParseNonNone(Attr(tp, OdfNamespaces.Style, "text-underline-style")),
            Strikethrough   = ParseNonNone(Attr(tp, OdfNamespaces.Style, "text-line-through-style")),
            Baseline        = ParseBaseline(Attr(tp, OdfNamespaces.Style, "text-position")),
        };
    }

    // ── Small helpers ─────────────────────────────────────────────────────────

    private static string? Attr(XElement? el, XNamespace ns, string name) =>
        (string?)el?.Attribute(ns + name);

    private static float? ParseLength(XElement? el, XNamespace ns, string attrName)
    {
        var val = (string?)el?.Attribute(ns + attrName);
        return val != null ? OdfUnits.ToPoints(val, 0f) : null;
    }

    private static string? StripHash(string? hex) =>
        hex == null ? null : hex.TrimStart('#');

    private static bool? ParseBold(string? v) =>
        v == null ? null : v.Equals("bold", StringComparison.OrdinalIgnoreCase);

    private static bool? ParseItalic(string? v) =>
        v == null ? null : v.Equals("italic", StringComparison.OrdinalIgnoreCase);

    private static bool? ParseNonNone(string? v) =>
        v == null ? null : !v.Equals("none", StringComparison.OrdinalIgnoreCase);

    private static string? ParseBaseline(string? v)
    {
        if (v == null) return null;
        if (v.StartsWith("super", StringComparison.OrdinalIgnoreCase)) return "super";
        if (v.StartsWith("sub",   StringComparison.OrdinalIgnoreCase)) return "sub";
        return "normal";
    }

    private static int? ParseInt(string? v, int subtract = 0)
    {
        if (int.TryParse(v, out var i)) return i - subtract;
        return null;
    }

    private string? ResolveFontFamily(string? fontName)
    {
        if (fontName == null) return null;
        return _fontFaces.TryGetValue(fontName, out var family) ? family : fontName;
    }
}
