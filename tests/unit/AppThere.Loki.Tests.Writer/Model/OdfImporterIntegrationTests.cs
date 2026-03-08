// LAYER:   AppThere.Loki.Tests.Writer — Tests
// KIND:    Tests
// PURPOSE: Integration tests for OdfImporter against the real corpus file
//          tests/corpus/writer/simple.fodt. Verifies that the three-pass
//          pipeline produces a non-null LokiDocument with the expected
//          block structure and style properties.
//          Also verifies that invalid inputs throw LokiOpenException.
// DEPENDS: OdfImporter, IFontManager, ILokiTypeface, NullLokiLogger,
//          LokiDocument, ParagraphNode, RunNode, LokiOpenException
// USED BY: CI unit test run
// PHASE:   3
// ADR:     ADR-009

using AppThere.Loki.Format.Odf;
using AppThere.Loki.Kernel.Fonts;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.LokiKit.Errors;
using AppThere.Loki.Skia.Fonts;
using AppThere.Loki.Writer.Model;
using AppThere.Loki.Writer.Model.Inlines;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AppThere.Loki.Tests.Writer.Model;

public sealed class OdfImporterIntegrationTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static readonly string FodtPath = Path.GetFullPath(
        Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "..",
            "tests", "corpus", "writer", "simple.fodt"));

    private static IFontManager MakeFontManager()
    {
        var fm = Substitute.For<IFontManager>();
        var tf = Substitute.For<ILokiTypeface>();
        fm.TryGetTypeface(Arg.Any<FontDescriptor>(), out Arg.Any<ILokiTypeface?>())
          .Returns(ci => { ci[1] = tf; return true; });
        return fm;
    }

    private static OdfImporter MakeImporter() => new();

    private static async Task<LokiDocument> ImportFodt()
    {
        using var stream = File.OpenRead(FodtPath);
        return await MakeImporter().ImportAsync(
            stream, isFlat: true, MakeFontManager(), NullLokiLogger.Instance);
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ImportFodt_ValidFile_ReturnsNonNullDocument()
    {
        var doc = await ImportFodt();
        doc.Should().NotBeNull();
    }

    [Fact]
    public async Task ImportFodt_ValidFile_BodyHasBlocks()
    {
        var doc = await ImportFodt();
        doc.Body.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ImportFodt_ValidFile_FirstBlockIsParagraphNode()
    {
        var doc = await ImportFodt();
        doc.Body[0].Should().BeOfType<ParagraphNode>();
    }

    [Fact]
    public async Task ImportFodt_ValidFile_FirstParagraphContainsHeadingText()
    {
        var doc  = await ImportFodt();
        var para = doc.Body[0].Should().BeOfType<ParagraphNode>().Subject;
        para.Inlines.Should().NotBeEmpty();
        para.Inlines.OfType<RunNode>()
            .Should().Contain(r => r.Text.Contains("Heading"));
    }

    [Fact]
    public async Task ImportFodt_ValidFile_ParagraphFontSizePtsIsPositive()
    {
        var doc  = await ImportFodt();
        var para = doc.Body[0].Should().BeOfType<ParagraphNode>().Subject;
        para.Style.FontSizePts.Should().BeGreaterThan(0f);
    }

    [Fact]
    public async Task ImportFodt_InvalidXml_ThrowsLokiOpenException()
    {
        using var stream = new MemoryStream(
            System.Text.Encoding.UTF8.GetBytes("this is not xml"));
        var act = async () => await MakeImporter().ImportAsync(
            stream, isFlat: true, MakeFontManager(), NullLokiLogger.Instance);
        await act.Should().ThrowAsync<LokiOpenException>();
    }

    [Fact]
    public async Task ImportFodt_EmptyStream_ThrowsLokiOpenException()
    {
        using var stream = new MemoryStream(Array.Empty<byte>());
        var act = async () => await MakeImporter().ImportAsync(
            stream, isFlat: true, MakeFontManager(), NullLokiLogger.Instance);
        await act.Should().ThrowAsync<LokiOpenException>();
    }
}
