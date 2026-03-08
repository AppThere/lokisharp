// LAYER:   AppThere.Loki.Format.Odf — ODF Import
// KIND:    Implementation
// PURPOSE: Three-pass ODF document parser. Converts parsed XDocument(s) into
//          a LokiDocument. Pass 1 reads named styles; Pass 2 reads automatic
//          styles and page layout; Pass 3 reads the document body.
//          Handles both flat ODF (single XDocument) and ZIP ODF (two XDocuments).
//          Logs and skips unrecognised elements — never throws on content errors.
// DEPENDS: OdfUnitConverter, OdfStyleResolver, StyleRegistry,
//          LokiDocument, ParagraphNode, BlockNode, InlineNode hierarchy
// USED BY: OdfImporter
// PHASE:   3
// ADR:     ADR-009

using System.Collections.Immutable;
using System.Xml.Linq;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.Writer.Model;
using AppThere.Loki.Writer.Model.Inlines;
using AppThere.Loki.Writer.Model.Styles;

namespace AppThere.Loki.Format.Odf;

internal static class OdfDocumentParser
{
    // ── ODF XML namespaces ────────────────────────────────────────────────────

    private static readonly XNamespace NsOffice =
        "urn:oasis:names:tc:opendocument:xmlns:office:1.0";
    private static readonly XNamespace NsText =
        "urn:oasis:names:tc:opendocument:xmlns:text:1.0";
    private static readonly XNamespace NsStyle =
        "urn:oasis:names:tc:opendocument:xmlns:style:1.0";
    private static readonly XNamespace NsFo =
        "urn:oasis:names:tc:opendocument:xmlns:xsl-fo-compatible:1.0";

    // ── Public entry ──────────────────────────────────────────────────────────

    /// <summary>
    /// Parse a flat-ODF XDocument into a LokiDocument.
    /// </summary>
    public static LokiDocument Parse(XDocument document, ILokiLogger logger)
    {
        var root = document.Root!;

        var stylesSection = root.Element(NsOffice + "styles");
        var autoSection   = root.Element(NsOffice + "automatic-styles");
        var body          = root.Element(NsOffice + "body")
                                ?.Element(NsOffice + "text");

        var registry  = BuildRegistry(stylesSection, autoSection);
        var pageStyle = ExtractPageStyle(root, autoSection);
        var resolver  = new OdfStyleResolver(registry);
        var blocks    = body is not null
            ? ParseBody(body, resolver, logger)
            : ImmutableList<BlockNode>.Empty;

        return new LokiDocument(blocks, registry, pageStyle, null, null);
    }

    // ── Pass 1 + 2: StyleRegistry ─────────────────────────────────────────────

    private static StyleRegistry BuildRegistry(
        XElement? namedSection, XElement? autoSection)
    {
        var paraStyles = new Dictionary<string, ParagraphStyleDef>(
            StringComparer.OrdinalIgnoreCase);
        var charStyles = new Dictionary<string, CharacterStyleDef>(
            StringComparer.OrdinalIgnoreCase);

        ParseStyleSection(namedSection,  paraStyles, charStyles, isAuto: false);
        ParseStyleSection(autoSection,   paraStyles, charStyles, isAuto: true);

        return new StyleRegistry(paraStyles, charStyles, null, null);
    }

    private static void ParseStyleSection(
        XElement? section,
        Dictionary<string, ParagraphStyleDef> paraStyles,
        Dictionary<string, CharacterStyleDef> charStyles,
        bool isAuto)
    {
        if (section is null) return;

        foreach (var style in section.Elements(NsStyle + "style"))
        {
            var name   = style.Attribute(NsStyle + "name")?.Value;
            var family = style.Attribute(NsStyle + "family")?.Value;
            var parent = style.Attribute(NsStyle + "parent-style-name")?.Value;
            if (name is null || family is null) continue;

            if (family is "paragraph")
                paraStyles[name] = ParseParaDef(style, name, parent, isAuto);
            else if (family is "text")
                charStyles[name] = ParseCharDef(style, name, parent, isAuto);
        }
    }

    private static ParagraphStyleDef ParseParaDef(
        XElement style, string name, string? parent, bool isAuto)
    {
        var pp = style.Element(NsStyle + "paragraph-properties");
        var tp = style.Element(NsStyle + "text-properties");

        return new ParagraphStyleDef
        {
            Id              = name,
            ParentId        = parent,
            IsAutomatic     = isAuto,
            FontFamily      = tp?.Attribute(NsStyle + "font-name")?.Value
                           ?? tp?.Attribute(NsFo   + "font-family")?.Value,
            FontSizePts     = ParsePts(tp?.Attribute(NsFo + "font-size")?.Value),
            Color           = tp?.Attribute(NsFo + "color")?.Value?.TrimStart('#'),
            Alignment       = pp?.Attribute(NsFo + "text-align")?.Value,
            MarginTopPts    = ParsePts(pp?.Attribute(NsFo + "margin-top")?.Value),
            MarginBottomPts = ParsePts(pp?.Attribute(NsFo + "margin-bottom")?.Value),
            MarginStartPts  = ParsePts(pp?.Attribute(NsFo + "margin-left")?.Value),
            MarginEndPts    = ParsePts(pp?.Attribute(NsFo + "margin-right")?.Value),
            SpaceBeforePts  = ParsePts(pp?.Attribute(NsFo + "margin-top")?.Value),
            SpaceAfterPts   = ParsePts(pp?.Attribute(NsFo + "margin-bottom")?.Value),
            Bold            = ParseBool(tp?.Attribute(NsFo + "font-weight")?.Value, "bold"),
            Italic          = ParseBool(tp?.Attribute(NsFo + "font-style")?.Value, "italic"),
        };
    }

    private static CharacterStyleDef ParseCharDef(
        XElement style, string name, string? parent, bool isAuto)
    {
        var tp = style.Element(NsStyle + "text-properties");

        return new CharacterStyleDef
        {
            Id          = name,
            ParentId    = parent,
            IsAutomatic = isAuto,
            FontFamily  = tp?.Attribute(NsStyle + "font-name")?.Value,
            FontSizePts = ParsePts(tp?.Attribute(NsFo + "font-size")?.Value),
            Color       = tp?.Attribute(NsFo + "color")?.Value?.TrimStart('#'),
            Bold        = ParseBool(tp?.Attribute(NsFo + "font-weight")?.Value, "bold"),
            Italic      = ParseBool(tp?.Attribute(NsFo + "font-style")?.Value, "italic"),
            Underline   = tp?.Attribute(NsStyle + "text-underline-style") is { } u
                            && u.Value != "none",
        };
    }

    // ── Page style extraction ─────────────────────────────────────────────────

    private static PageStyle ExtractPageStyle(XElement root, XElement? autoSection)
    {
        // Find the Standard master page layout name
        var masterPage = root
            .Element(NsOffice + "master-styles")
            ?.Elements(NsStyle + "master-page")
            .FirstOrDefault(e => e.Attribute(NsStyle + "name")?.Value == "Standard");
        var layoutName = masterPage?.Attribute(NsStyle + "page-layout-name")?.Value ?? "pm1";

        // Find that page-layout in automatic-styles
        var layout = autoSection
            ?.Elements(NsStyle + "page-layout")
            .FirstOrDefault(e => e.Attribute(NsStyle + "name")?.Value == layoutName);

        var props = layout?.Element(NsStyle + "page-layout-properties");
        if (props is null) return PageStyle.A4;

        return new PageStyle(
            WidthPts:         OdfUnitConverter.ToPoints(props.Attribute(NsFo + "page-width")?.Value),
            HeightPts:        OdfUnitConverter.ToPoints(props.Attribute(NsFo + "page-height")?.Value),
            MarginTopPts:     OdfUnitConverter.ToPoints(props.Attribute(NsFo + "margin-top")?.Value),
            MarginBottomPts:  OdfUnitConverter.ToPoints(props.Attribute(NsFo + "margin-bottom")?.Value),
            MarginStartPts:   OdfUnitConverter.ToPoints(props.Attribute(NsFo + "margin-left")?.Value),
            MarginEndPts:     OdfUnitConverter.ToPoints(props.Attribute(NsFo + "margin-right")?.Value));
    }

    // ── Pass 3: body parsing ──────────────────────────────────────────────────

    private static ImmutableList<BlockNode> ParseBody(
        XElement body, OdfStyleResolver resolver, ILokiLogger logger)
    {
        var blocks = ImmutableList.CreateBuilder<BlockNode>();

        foreach (var el in body.Elements())
        {
            if (el.Name == NsText + "h" || el.Name == NsText + "p")
            {
                var paraNode = ParseParagraph(el, resolver, logger);
                if (paraNode is not null) blocks.Add(paraNode);
            }
            else if (el.Name == NsText + "list")
            {
                foreach (var item in el.Elements(NsText + "list-item"))
                {
                    foreach (var pe in item.Elements(NsText + "p"))
                    {
                        var paraNode = ParseParagraph(pe, resolver, logger);
                        if (paraNode is not null) blocks.Add(paraNode);
                    }
                }
            }
            // Other elements (sequence-decls, etc.) are silently skipped
        }

        return blocks.ToImmutable();
    }

    private static ParagraphNode? ParseParagraph(
        XElement el, OdfStyleResolver resolver, ILokiLogger logger)
    {
        var styleId = el.Attribute(NsText + "style-name")?.Value;
        var paraStyle = resolver.ResolveParagraph(styleId, null);
        var inlines = ParseInlines(el, paraStyle, resolver, logger);
        return new ParagraphNode(inlines, paraStyle, styleId);
    }

    private static ImmutableList<InlineNode> ParseInlines(
        XElement container, ParagraphStyle paraStyle,
        OdfStyleResolver resolver, ILokiLogger logger)
    {
        var inlines = ImmutableList.CreateBuilder<InlineNode>();
        var charStyle = resolver.ResolveCharacter(null, null, paraStyle);

        foreach (var node in container.Nodes())
        {
            if (node is XText textNode)
            {
                var text = textNode.Value;
                if (!string.IsNullOrEmpty(text))
                    inlines.Add(new RunNode(text, charStyle, null));
            }
            else if (node is XElement el)
            {
                if (el.Name == NsText + "span")
                {
                    var spanStyleId = el.Attribute(NsText + "style-name")?.Value;
                    var spanStyle   = resolver.ResolveCharacter(spanStyleId, null, paraStyle);
                    foreach (var child in el.Nodes())
                    {
                        if (child is XText t && !string.IsNullOrEmpty(t.Value))
                            inlines.Add(new RunNode(t.Value, spanStyle, spanStyleId));
                        else if (child is XElement ce)
                            CollectInlineElement(ce, paraStyle, spanStyle, resolver, inlines);
                    }
                }
                else
                    CollectInlineElement(el, paraStyle, charStyle, resolver, inlines);
            }
        }

        return inlines.ToImmutable();
    }

    private static void CollectInlineElement(
        XElement el, ParagraphStyle paraStyle, CharacterStyle charStyle,
        OdfStyleResolver resolver,
        ImmutableList<InlineNode>.Builder inlines)
    {
        if (el.Name == NsText + "line-break") inlines.Add(new HardLineBreakNode());
        else if (el.Name == NsText + "tab")   inlines.Add(new TabNode());
        else if (el.Name == NsText + "span")
        {
            var spanStyleId = el.Attribute(NsText + "style-name")?.Value;
            var spanStyle   = resolver.ResolveCharacter(spanStyleId, null, paraStyle);
            foreach (var child in el.Nodes())
            {
                if (child is XText t && !string.IsNullOrEmpty(t.Value))
                    inlines.Add(new RunNode(t.Value, spanStyle, spanStyleId));
                else if (child is XElement ce)
                    CollectInlineElement(ce, paraStyle, spanStyle, resolver, inlines);
            }
        }
    }

    // ── Primitive parsers ─────────────────────────────────────────────────────

    private static float? ParsePts(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var pts = OdfUnitConverter.ToPoints(value);
        return pts > 0f ? pts : null;
    }

    private static bool? ParseBool(string? value, string trueValue) =>
        value is null ? null : value.Equals(trueValue, StringComparison.OrdinalIgnoreCase);
}
