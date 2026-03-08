// LAYER:   AppThere.Loki.Tests.Writer — Tests
// KIND:    Tests
// PURPOSE: Regression tests for the three style-cascade and rendering bugs fixed in
//          Phase 3 Track B Task 3: percentage font-size cascade (Bug 1), bold
//          weight not applied to FontDescriptor (Bug 2), and list-item detection
//          for bullet rendering (Bug 3).
// DEPENDS: OdfStyleResolver, StyleRegistry, ParagraphStyleDef, CharacterStyleDef,
//          OdfImporter, NullLokiLogger, simple.fodt
// USED BY: CI test runner
// PHASE:   3
// ADR:     ADR-007, ADR-009

using AppThere.Loki.Format.Odf;

namespace AppThere.Loki.Tests.Writer;

public sealed class StyleResolverBugFixTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static StyleRegistry MakeParagraphRegistry(
        params ParagraphStyleDef[] defs)
    {
        var dict = defs.ToDictionary(
            d => d.Id,
            d => d,
            StringComparer.OrdinalIgnoreCase);
        return new StyleRegistry(dict,
            new Dictionary<string, CharacterStyleDef>(), null, null);
    }

    private static StyleRegistry MakeCharacterRegistry(
        params CharacterStyleDef[] defs)
    {
        var dict = defs.ToDictionary(
            d => d.Id,
            d => d,
            StringComparer.OrdinalIgnoreCase);
        return new StyleRegistry(
            new Dictionary<string, ParagraphStyleDef>(), dict, null, null);
    }

    private static string CorpusPath(string file) =>
        Path.Combine(AppContext.BaseDirectory, "corpus", "writer", file);

    // ── Bug 1: Percentage font size ───────────────────────────────────────────

    [Fact]
    public void Resolve_PercentageFontSize_ResolvesRelativeToParent()
    {
        // "Base" at 12pt; "Child" at 150% → expects 18pt
        var registry = MakeParagraphRegistry(
            new ParagraphStyleDef { Id = "Base",  FontSizePts = 12f },
            new ParagraphStyleDef { Id = "Child", FontSizePercentage = 150f, ParentId = "Base" });
        var resolver = new OdfStyleResolver(registry);

        var result = resolver.ResolveParagraph("Child", null);

        result.FontSizePts.Should().BeApproximately(18f, 0.01f,
            "150% of 12pt inherited size must yield 18pt");
    }

    [Fact]
    public void Resolve_PercentageFontSize_DefaultParentIs12pt()
    {
        // "MyStyle" at 100% with no parent → should remain at 12pt (the default)
        var registry = MakeParagraphRegistry(
            new ParagraphStyleDef { Id = "MyStyle", FontSizePercentage = 100f });
        var resolver = new OdfStyleResolver(registry);

        var result = resolver.ResolveParagraph("MyStyle", null);

        result.FontSizePts.Should().BeApproximately(12f, 0.01f,
            "100% of the 12pt default must yield 12pt");
    }

    // ── Bug 2: Bold weight on FontDescriptor ──────────────────────────────────

    [Fact]
    public void Resolve_BoldCharacterStyle_UsesBoldWeight()
    {
        var registry = MakeCharacterRegistry(
            new CharacterStyleDef { Id = "Strong", Bold = true });
        var resolver = new OdfStyleResolver(registry);

        var result = resolver.ResolveCharacter("Strong", null, ParagraphStyle.Default);

        result.Bold.Should().BeTrue("the character style declares bold");
        result.Font.Weight.Should().Be(FontWeight.Bold,
            "FontDescriptor.Weight must reflect the Bold flag so IFontManager returns the bold typeface");
    }

    // ── Bug 3 pre-requisite: list-item import ─────────────────────────────────

    [Fact]
    public async Task ImportAsync_Heading1_HasFontSizeAbove12pt()
    {
        var importer = new OdfImporter();
        await using var stream = File.OpenRead(CorpusPath("simple.fodt"));
        var doc = await importer.ImportAsync(stream, isFlat: true,
            fontManager: null!, logger: NullLokiLogger.Instance, ct: default);

        // Heading_20_1 sets fo:font-size="18pt" → resolved size must exceed 12pt
        var headingPara = doc.Body
            .OfType<ParagraphNode>()
            .FirstOrDefault(p =>
                p.StyleName is not null &&
                p.StyleName.Contains("Heading", StringComparison.OrdinalIgnoreCase));

        headingPara.Should().NotBeNull("simple.fodt contains a Heading 1 paragraph");
        headingPara!.Style.FontSizePts.Should().BeGreaterThan(12f,
            "Heading 1 inherits 18pt from Heading_20_1");
    }

    [Fact]
    public async Task ImportAsync_StrongRun_HasBoldWeight()
    {
        var importer = new OdfImporter();
        await using var stream = File.OpenRead(CorpusPath("simple.fodt"));
        var doc = await importer.ImportAsync(stream, isFlat: true,
            fontManager: null!, logger: NullLokiLogger.Instance, ct: default);

        // simple.fodt has a Strong_20_Emphasis span whose text is not nested in T1
        var boldRun = doc.Body
            .OfType<ParagraphNode>()
            .SelectMany(p => p.Inlines)
            .OfType<RunNode>()
            .FirstOrDefault(r => r.Style.Bold);

        boldRun.Should().NotBeNull("simple.fodt contains strongly-formatted inline text");
        boldRun!.Style.Bold.Should().BeTrue();
        boldRun.Style.Font.Weight.Should().Be(FontWeight.Bold,
            "FontDescriptor.Weight must be Bold when Style.Bold is true");
    }

    [Fact]
    public async Task ImportAsync_ListItem_HasListStyleId()
    {
        var importer = new OdfImporter();
        await using var stream = File.OpenRead(CorpusPath("simple.fodt"));
        var doc = await importer.ImportAsync(stream, isFlat: true,
            fontManager: null!, logger: NullLokiLogger.Instance, ct: default);

        var listPara = doc.Body
            .OfType<ParagraphNode>()
            .FirstOrDefault(p => p.Style.ListStyleId is not null);

        listPara.Should().NotBeNull(
            "simple.fodt contains list items whose paragraph style carries a list-style-id");
        listPara!.Style.ListStyleId.Should().NotBeNullOrWhiteSpace();
    }
}
