// LAYER:   AppThere.Loki.Format.Odf — ODF Import
// KIND:    Implementation
// PURPOSE: ODF import Pass 3 — walks office:text children and produces an
//          ImmutableList<BlockNode> by resolving style cascades through
//          IStyleResolver. Handles text:p, text:h, text:list, text:span,
//          text:s, text:tab, and text:line-break. Ignores unknown elements
//          with a Debug log entry. Does NOT parse styles or page geometry.
// DEPENDS: IStyleResolver, OdfNamespaces, ParagraphNode, RunNode,
//          HardLineBreakNode, TabNode, ILokiLogger
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

internal sealed class BodyParser
{
    private readonly IStyleResolver _resolver;
    private readonly ILokiLogger    _logger;

    public BodyParser(IStyleResolver resolver, ILokiLogger logger)
    {
        _resolver = resolver;
        _logger   = logger;
    }

    // ── Public entry point ────────────────────────────────────────────────────

    public ImmutableList<BlockNode> ParseBody(XElement officeText)
    {
        var blocks = new List<BlockNode>();

        foreach (var child in officeText.Elements())
        {
            if (child.Name == OdfNamespaces.Text + "p" ||
                child.Name == OdfNamespaces.Text + "h")
            {
                blocks.Add(ParseParagraph(child, null, 0));
            }
            else if (child.Name == OdfNamespaces.Text + "list")
            {
                blocks.AddRange(ParseList(child, 0));
            }
            else if (child.Name == OdfNamespaces.Text + "sequence-decls")
            {
                // intentionally skipped
            }
            else
            {
                _logger.Debug("BodyParser: ignoring element '{0}'", child.Name.LocalName);
            }
        }

        return blocks.ToImmutableList();
    }

    // ── Block-level parsers ───────────────────────────────────────────────────

    private ParagraphNode ParseParagraph(
        XElement el,
        string?  listStyleId,
        int      listLevel)
    {
        var styleId  = (string?)el.Attribute(OdfNamespaces.Text + "style-name");
        var resolved = _resolver.ResolveParagraph(styleId, null);

        if (listStyleId is not null)
            resolved = resolved with { ListStyleId = listStyleId, ListLevel = listLevel };

        var defaultChar = _resolver.ResolveCharacter(null, null, resolved);
        var inlines     = CollectInlines(el, defaultChar, resolved);

        return new ParagraphNode(inlines.ToImmutableList(), resolved, styleId);
    }

    private IEnumerable<ParagraphNode> ParseList(XElement list, int level)
    {
        var listStyleId = (string?)list.Attribute(OdfNamespaces.Text + "style-name");

        foreach (var item in list.Elements(OdfNamespaces.Text + "list-item"))
        {
            foreach (var child in item.Elements())
            {
                if (child.Name == OdfNamespaces.Text + "p" ||
                    child.Name == OdfNamespaces.Text + "h")
                {
                    yield return ParseParagraph(child, listStyleId, level);
                }
                else if (child.Name == OdfNamespaces.Text + "list")
                {
                    foreach (var nested in ParseList(child, level + 1))
                        yield return nested;
                }
                else
                {
                    _logger.Debug("BodyParser: ignoring list-item child '{0}'",
                        child.Name.LocalName);
                }
            }
        }
    }

    // ── Inline parsers ────────────────────────────────────────────────────────

    private List<InlineNode> CollectInlines(
        XElement       parent,
        CharacterStyle defaultCharStyle,
        ParagraphStyle paraStyle)
    {
        var inlines = new List<InlineNode>();

        foreach (var node in parent.Nodes())
        {
            if (node is XText text)
            {
                if (!string.IsNullOrEmpty(text.Value))
                    inlines.Add(new RunNode(text.Value, defaultCharStyle, null));
            }
            else if (node is XElement el)
            {
                if (el.Name == OdfNamespaces.Text + "span" ||
                    el.Name == OdfNamespaces.Text + "a")
                {
                    inlines.AddRange(ParseSpan(el, paraStyle, defaultCharStyle));
                }
                else if (el.Name == OdfNamespaces.Text + "s")
                {
                    var count  = (int?)el.Attribute(OdfNamespaces.Text + "c") ?? 1;
                    var spaces = new string(' ', Math.Max(1, count));
                    inlines.Add(new RunNode(spaces, defaultCharStyle, null));
                }
                else if (el.Name == OdfNamespaces.Text + "tab")
                {
                    inlines.Add(new TabNode());
                }
                else if (el.Name == OdfNamespaces.Text + "line-break")
                {
                    inlines.Add(new HardLineBreakNode());
                }
                else
                {
                    _logger.Debug("BodyParser: ignoring inline element '{0}'",
                        el.Name.LocalName);
                }
            }
        }

        return inlines;
    }

    private IEnumerable<InlineNode> ParseSpan(
        XElement       span,
        ParagraphStyle paraStyle,
        CharacterStyle parentCharStyle)
    {
        var styleId  = (string?)span.Attribute(OdfNamespaces.Text + "style-name");
        var resolved = styleId is not null
            ? _resolver.ResolveCharacter(styleId, null, paraStyle)
            : parentCharStyle;

        return CollectInlines(span, resolved, paraStyle);
    }
}
