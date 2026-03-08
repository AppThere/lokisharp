// LAYER:   AppThere.Loki.Tests.Writer — Tests
// KIND:    Tests
// PURPOSE: Unit tests for BodyParser — verifies that ParseBody correctly maps
//          ODF text:p, text:h, text:list, text:span, text:s, text:tab, and
//          text:line-break elements to the appropriate BlockNode / InlineNode
//          types. Uses XElement fixtures rather than XML string parsing.
//          Does NOT test style cascade (covered by StyleResolverTests).
// DEPENDS: BodyParser, OdfNamespaces, IStyleResolver, NullLokiLogger,
//          ParagraphNode, RunNode, HardLineBreakNode, TabNode
// USED BY: CI unit test run
// PHASE:   3
// ADR:     ADR-009

using System.Collections.Immutable;
using System.Xml.Linq;
using AppThere.Loki.Format.Odf;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.Writer.Model;
using AppThere.Loki.Writer.Model.Inlines;
using AppThere.Loki.Writer.Model.Styles;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AppThere.Loki.Tests.Writer.Model;

public sealed class OdfBodyParserTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static IStyleResolver MakeResolver()
    {
        var resolver = Substitute.For<IStyleResolver>();
        resolver
            .ResolveParagraph(Arg.Any<string?>(), Arg.Any<ParagraphStyleDef?>())
            .Returns(ParagraphStyle.Default);
        resolver
            .ResolveCharacter(
                Arg.Any<string?>(),
                Arg.Any<CharacterStyleDef?>(),
                Arg.Any<ParagraphStyle>())
            .Returns(CharacterStyle.Default);
        return resolver;
    }

    private static BodyParser MakeBodyParser() =>
        new(MakeResolver(), NullLokiLogger.Instance);

    private static XElement OfficeText(params XNode[] children) =>
        new(OdfNamespaces.Office + "text", children);

    private static XElement TextP(string? styleId, params XNode[] children)
    {
        var el = new XElement(OdfNamespaces.Text + "p", children);
        if (styleId is not null)
            el.SetAttributeValue(OdfNamespaces.Text + "style-name", styleId);
        return el;
    }

    private static XElement TextH(string? styleId, params XNode[] children)
    {
        var el = new XElement(OdfNamespaces.Text + "h", children);
        if (styleId is not null)
            el.SetAttributeValue(OdfNamespaces.Text + "style-name", styleId);
        return el;
    }

    private static XElement TextSpan(string? styleId, params XNode[] children)
    {
        var el = new XElement(OdfNamespaces.Text + "span", children);
        if (styleId is not null)
            el.SetAttributeValue(OdfNamespaces.Text + "style-name", styleId);
        return el;
    }

    private static XElement TextList(string? styleId, params XElement[] items)
    {
        var listItems = items.Select(
            item => new XElement(OdfNamespaces.Text + "list-item", item));
        var el = new XElement(OdfNamespaces.Text + "list", listItems);
        if (styleId is not null)
            el.SetAttributeValue(OdfNamespaces.Text + "style-name", styleId);
        return el;
    }

    // ── Tests — body-level parsing ────────────────────────────────────────────

    [Fact]
    public void ParseBody_EmptyOfficeText_ReturnsEmptyList()
    {
        var parser = MakeBodyParser();
        var result = parser.ParseBody(OfficeText());
        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseBody_SingleParagraph_ReturnsSingleParagraphNode()
    {
        var parser = MakeBodyParser();
        var result = parser.ParseBody(OfficeText(TextP("P1", new XText("Hello"))));
        result.Should().ContainSingle()
              .Which.Should().BeOfType<ParagraphNode>();
    }

    [Fact]
    public void ParseBody_HeadingElement_ReturnsParagraphNode()
    {
        var parser = MakeBodyParser();
        var result = parser.ParseBody(OfficeText(TextH("H1", new XText("Heading"))));
        result.Should().ContainSingle()
              .Which.Should().BeOfType<ParagraphNode>();
    }

    [Fact]
    public void ParseBody_SequenceDeclsIgnored_NoBlockNode()
    {
        var seqDecls = new XElement(OdfNamespaces.Text + "sequence-decls");
        var parser   = MakeBodyParser();
        var result   = parser.ParseBody(OfficeText(seqDecls, TextP("P1", new XText("x"))));
        result.Should().ContainSingle()
              .Which.Should().BeOfType<ParagraphNode>();
    }

    // ── Tests — inline parsing ────────────────────────────────────────────────

    [Fact]
    public void ParseParagraph_PlainTextRun_ReturnsRunNodeWithText()
    {
        var parser = MakeBodyParser();
        var body   = parser.ParseBody(OfficeText(TextP("P1", new XText("Hello world"))));
        var para   = body.Should().ContainSingle().Which.Should().BeOfType<ParagraphNode>().Subject;
        var run    = para.Inlines.Should().ContainSingle().Which.Should().BeOfType<RunNode>().Subject;
        run.Text.Should().Be("Hello world");
    }

    [Fact]
    public void ParseParagraph_EmptyParagraph_ReturnsEmptyInlines()
    {
        var parser = MakeBodyParser();
        var body   = parser.ParseBody(OfficeText(TextP("P1")));
        var para   = body.Should().ContainSingle().Which.Should().BeOfType<ParagraphNode>().Subject;
        para.Inlines.Should().BeEmpty();
    }

    [Fact]
    public void ParseParagraph_TextSpan_ReturnsRunNodeFromSpanText()
    {
        var parser = MakeBodyParser();
        var body   = parser.ParseBody(OfficeText(
            TextP("P1", TextSpan("Emphasis", new XText("italic text")))));
        var para   = body.Should().ContainSingle().Which.Should().BeOfType<ParagraphNode>().Subject;
        var run    = para.Inlines.Should().ContainSingle().Which.Should().BeOfType<RunNode>().Subject;
        run.Text.Should().Be("italic text");
    }

    [Fact]
    public void ParseParagraph_TabElement_ReturnsTabNode()
    {
        var tab    = new XElement(OdfNamespaces.Text + "tab");
        var parser = MakeBodyParser();
        var body   = parser.ParseBody(OfficeText(TextP("P1", tab)));
        var para   = body.Should().ContainSingle().Which.Should().BeOfType<ParagraphNode>().Subject;
        para.Inlines.Should().ContainSingle().Which.Should().BeOfType<TabNode>();
    }

    [Fact]
    public void ParseParagraph_LineBreakElement_ReturnsHardLineBreakNode()
    {
        var lb     = new XElement(OdfNamespaces.Text + "line-break");
        var parser = MakeBodyParser();
        var body   = parser.ParseBody(OfficeText(TextP("P1", lb)));
        var para   = body.Should().ContainSingle().Which.Should().BeOfType<ParagraphNode>().Subject;
        para.Inlines.Should().ContainSingle().Which.Should().BeOfType<HardLineBreakNode>();
    }

    // ── Tests — list parsing ──────────────────────────────────────────────────

    [Fact]
    public void ParseBody_TextList_ReturnsParagraphNodesWithListStyleId()
    {
        var parser = MakeBodyParser();
        var body   = parser.ParseBody(OfficeText(
            TextList("L1",
                TextP("P4", new XText("Item 1")),
                TextP("P4", new XText("Item 2")))));

        body.Should().HaveCount(2);
        foreach (var block in body)
        {
            var para = block.Should().BeOfType<ParagraphNode>().Subject;
            para.Style.ListStyleId.Should().Be("L1");
            para.Style.ListLevel.Should().Be(0);
        }
    }
}
