// LAYER:   AppThere.Loki.Tests.Writer — Tests
// KIND:    Tests
// PURPOSE: Unit tests for StyleParser (ODF import Passes 1 and 2).
//          Verifies style parsing, inheritance chain, automatic-style marking,
//          font face resolution, default style identification, and robustness
//          against unknown XML elements. Uses in-memory XDocument fixtures.
// DEPENDS: StyleParser, OdfNamespaces, StyleRegistry, ParagraphStyleDef,
//          CharacterStyleDef, ILokiLogger
// USED BY: CI unit test run
// PHASE:   3
// ADR:     ADR-009

using System.Xml.Linq;
using AppThere.Loki.Format.Odf;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.Writer.Model.Styles;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AppThere.Loki.Tests.Writer.Model;

public sealed class OdfStyleParserTests
{
    // ── XML fixture helpers ───────────────────────────────────────────────────

    private const string NsDecls =
        "xmlns:office=\"urn:oasis:names:tc:opendocument:xmlns:office:1.0\" " +
        "xmlns:style=\"urn:oasis:names:tc:opendocument:xmlns:style:1.0\" " +
        "xmlns:text=\"urn:oasis:names:tc:opendocument:xmlns:text:1.0\" " +
        "xmlns:fo=\"urn:oasis:names:tc:opendocument:xmlns:xsl-fo-compatible:1.0\" " +
        "xmlns:svg=\"urn:oasis:names:tc:opendocument:xmlns:svg-compatible:1.0\"";

    private static XDocument MakeStylesDoc(
        string namedStylesXml  = "",
        string fontFaceDeclXml = "")
    {
        return XDocument.Parse(
            $"<office:document-styles {NsDecls}>" +
            $"  <office:font-face-decls>{fontFaceDeclXml}</office:font-face-decls>" +
            $"  <office:styles>{namedStylesXml}</office:styles>" +
            "</office:document-styles>");
    }

    private static XDocument MakeContentDoc(
        string autoStylesXml  = "",
        string fontFaceDeclXml = "")
    {
        return XDocument.Parse(
            $"<office:document-content {NsDecls}>" +
            $"  <office:font-face-decls>{fontFaceDeclXml}</office:font-face-decls>" +
            $"  <office:automatic-styles>{autoStylesXml}</office:automatic-styles>" +
            "</office:document-content>");
    }

    private static StyleParser MakeParser() =>
        new(Substitute.For<ILokiLogger>());

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ParseRegistry_EmptyDoc_ReturnsEmptyRegistry()
    {
        var parser   = MakeParser();
        var content  = MakeContentDoc();
        var styles   = MakeStylesDoc();

        var registry = parser.ParseStyleRegistry(content, styles);

        registry.ParagraphStyles.Should().BeEmpty();
        registry.CharacterStyles.Should().BeEmpty();
    }

    [Fact]
    public void ParseRegistry_NamedParagraphStyle_Parsed()
    {
        const string styleXml =
            "<style:style style:name=\"Normal\" style:family=\"paragraph\">" +
            "  <style:text-properties fo:font-size=\"14pt\"/>" +
            "</style:style>";

        var registry = MakeParser().ParseStyleRegistry(
            MakeContentDoc(), MakeStylesDoc(namedStylesXml: styleXml));

        registry.ParagraphStyles.Should().ContainKey("Normal");
        registry.ParagraphStyles["Normal"].FontSizePts.Should().BeApproximately(14f, 0.001f);
    }

    [Fact]
    public void ParseRegistry_StyleInheritance_ParentIdSet()
    {
        const string styleXml =
            "<style:style style:name=\"Child\" style:family=\"text\" " +
            "style:parent-style-name=\"Base\"/>";

        var registry = MakeParser().ParseStyleRegistry(
            MakeContentDoc(), MakeStylesDoc(namedStylesXml: styleXml));

        registry.CharacterStyles.Should().ContainKey("Child");
        registry.CharacterStyles["Child"].ParentId.Should().Be("Base");
    }

    [Fact]
    public void ParseRegistry_AutomaticStyle_MarkedAsAutomatic()
    {
        const string autoXml =
            "<style:style style:name=\"P1\" style:family=\"paragraph\" " +
            "style:parent-style-name=\"Normal\"/>";

        var registry = MakeParser().ParseStyleRegistry(
            MakeContentDoc(autoStylesXml: autoXml), MakeStylesDoc());

        registry.ParagraphStyles.Should().ContainKey("P1");
        registry.ParagraphStyles["P1"].IsAutomatic.Should().BeTrue();
    }

    [Fact]
    public void ParseRegistry_TextProperties_BoldItalic()
    {
        const string styleXml =
            "<style:style style:name=\"Emph\" style:family=\"text\">" +
            "  <style:text-properties fo:font-weight=\"bold\" fo:font-style=\"italic\"/>" +
            "</style:style>";

        var registry = MakeParser().ParseStyleRegistry(
            MakeContentDoc(), MakeStylesDoc(namedStylesXml: styleXml));

        registry.CharacterStyles.Should().ContainKey("Emph");
        registry.CharacterStyles["Emph"].Bold.Should().BeTrue();
        registry.CharacterStyles["Emph"].Italic.Should().BeTrue();
    }

    [Fact]
    public void ParseRegistry_ParagraphProperties_Margins()
    {
        const string styleXml =
            "<style:style style:name=\"Spaced\" style:family=\"paragraph\">" +
            "  <style:paragraph-properties fo:margin-top=\"0.5cm\"/>" +
            "</style:style>";

        var registry = MakeParser().ParseStyleRegistry(
            MakeContentDoc(), MakeStylesDoc(namedStylesXml: styleXml));

        registry.ParagraphStyles["Spaced"].SpaceBeforePts
            .Should().BeApproximately(14.17f, 0.01f);
    }

    [Fact]
    public void ParseRegistry_TextAlign_Justify()
    {
        const string styleXml =
            "<style:style style:name=\"Justified\" style:family=\"paragraph\">" +
            "  <style:paragraph-properties fo:text-align=\"justify\"/>" +
            "</style:style>";

        var registry = MakeParser().ParseStyleRegistry(
            MakeContentDoc(), MakeStylesDoc(namedStylesXml: styleXml));

        registry.ParagraphStyles["Justified"].Alignment.Should().Be("justify");
    }

    [Fact]
    public void ParseRegistry_FontFaceDecl_ResolvesFamily()
    {
        const string fontFaceXml =
            "<style:font-face style:name=\"MyFont\" svg:font-family=\"Arial\"/>";
        const string styleXml =
            "<style:style style:name=\"AF\" style:family=\"text\">" +
            "  <style:text-properties style:font-name=\"MyFont\"/>" +
            "</style:style>";

        var stylesDoc = MakeStylesDoc(
            namedStylesXml: styleXml, fontFaceDeclXml: fontFaceXml);

        var registry = MakeParser().ParseStyleRegistry(MakeContentDoc(), stylesDoc);

        registry.CharacterStyles["AF"].FontFamily.Should().Be("Arial");
    }

    [Fact]
    public void ParseRegistry_DefaultParagraphStyle_Identified()
    {
        const string defaultXml =
            "<style:default-style style:family=\"paragraph\">" +
            "  <style:text-properties fo:font-size=\"12pt\"/>" +
            "</style:default-style>";

        var registry = MakeParser().ParseStyleRegistry(
            MakeContentDoc(), MakeStylesDoc(namedStylesXml: defaultXml));

        registry.DefaultParagraphStyleId.Should().NotBeNull();
        registry.ParagraphStyles.Should().ContainKey(registry.DefaultParagraphStyleId!);
    }

    [Fact]
    public void ParseRegistry_UnknownElement_Skipped()
    {
        const string mixedXml =
            "<style:style style:name=\"Normal\" style:family=\"paragraph\"/>" +
            "<my:unknown xmlns:my=\"urn:example:unknown\">ignored</my:unknown>";

        var act = () => MakeParser().ParseStyleRegistry(
            MakeContentDoc(), MakeStylesDoc(namedStylesXml: mixedXml));

        act.Should().NotThrow();
    }
}
