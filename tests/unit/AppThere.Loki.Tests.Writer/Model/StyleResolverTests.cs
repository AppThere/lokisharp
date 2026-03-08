// LAYER:   AppThere.Loki.Tests.Writer — Tests
// KIND:    Tests
// PURPOSE: Unit tests for StyleResolver and ColorParser — verifies cascade logic,
//          style chain inheritance, direct formatting override, cycle detection,
//          character style paragraph inheritance, and hex colour parsing.
//          Does NOT test ODF unit conversion (covered by OdfUnitsTests).
// DEPENDS: StyleResolver, ColorParser, StyleRegistry, ParagraphStyleDef,
//          CharacterStyleDef, ParagraphStyle, CharacterStyle,
//          IFontManager, ILokiTypeface, ILokiLogger
// USED BY: CI unit test run
// PHASE:   3
// ADR:     ADR-007

using System.Diagnostics;
using AppThere.Loki.Kernel.Color;
using AppThere.Loki.Kernel.Fonts;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.Skia.Fonts;
using AppThere.Loki.Writer.Model.Styles;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AppThere.Loki.Tests.Writer.Model;

public sealed class StyleResolverTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static IFontManager MakeFontManager()
    {
        var fm       = Substitute.For<IFontManager>();
        var typeface = Substitute.For<ILokiTypeface>();
        fm.TryGetTypeface(Arg.Any<FontDescriptor>(), out Arg.Any<ILokiTypeface?>())
          .Returns(ci => { ci[1] = typeface; return true; });
        return fm;
    }

    private static ILokiLogger MakeLogger() => Substitute.For<ILokiLogger>();

    private static StyleRegistry MakeParagraphRegistry(
        string? defaultId,
        params ParagraphStyleDef[] defs)
    {
        var dict = defs.ToDictionary(d => d.Id, StringComparer.Ordinal);
        return new StyleRegistry(dict,
            new Dictionary<string, CharacterStyleDef>(),
            defaultId, null);
    }

    private static StyleRegistry MakeCharacterRegistry(
        string? defaultParaId,
        string? defaultCharId,
        ParagraphStyleDef[] paraDefs,
        CharacterStyleDef[] charDefs)
    {
        var pDict = paraDefs.ToDictionary(d => d.Id, StringComparer.Ordinal);
        var cDict = charDefs.ToDictionary(d => d.Id, StringComparer.Ordinal);
        return new StyleRegistry(pDict, cDict, defaultParaId, defaultCharId);
    }

    // ── ResolveParagraph tests ────────────────────────────────────────────────

    [Fact]
    public void Resolve_NullStyleId_ReturnsDefault()
    {
        var registry = MakeParagraphRegistry(null);
        var resolver = new StyleResolver(registry, MakeFontManager(), MakeLogger());

        var result = resolver.ResolveParagraph(null, null);

        result.Should().Be(ParagraphStyle.Default);
    }

    [Fact]
    public void Resolve_NamedStyle_AppliesProperties()
    {
        var normal   = new ParagraphStyleDef { Id = "Normal", FontSizePts = 14f };
        var registry = MakeParagraphRegistry("Normal", normal);
        var resolver = new StyleResolver(registry, MakeFontManager(), MakeLogger());

        var result = resolver.ResolveParagraph("Normal", null);

        result.FontSizePts.Should().BeApproximately(14f, 0.001f);
    }

    [Fact]
    public void Resolve_InheritedStyle_MergesChain()
    {
        var baseStyle  = new ParagraphStyleDef { Id = "Base",  FontSizePts = 12f };
        var childStyle = new ParagraphStyleDef { Id = "Child", ParentId = "Base", Bold = true };
        var registry   = MakeParagraphRegistry("Base", baseStyle, childStyle);
        var resolver   = new StyleResolver(registry, MakeFontManager(), MakeLogger());

        var result = resolver.ResolveParagraph("Child", null);

        result.FontSizePts.Should().BeApproximately(12f, 0.001f);
        result.Font.Weight.Should().Be(FontWeight.Bold);
    }

    [Fact]
    public void Resolve_DirectFormatting_OverridesStyle()
    {
        var normal   = new ParagraphStyleDef { Id = "Normal", FontSizePts = 12f };
        var registry = MakeParagraphRegistry("Normal", normal);
        var resolver = new StyleResolver(registry, MakeFontManager(), MakeLogger());
        var direct   = new ParagraphStyleDef { Id = "_direct", FontSizePts = 18f };

        var result = resolver.ResolveParagraph("Normal", direct);

        result.FontSizePts.Should().BeApproximately(18f, 0.001f);
    }

    [Fact]
    public void Resolve_CycleDetection_DoesNotHang()
    {
        var a = new ParagraphStyleDef { Id = "A", ParentId = "B" };
        var b = new ParagraphStyleDef { Id = "B", ParentId = "A" };
        var registry = MakeParagraphRegistry(null, a, b);
        var resolver = new StyleResolver(registry, MakeFontManager(), MakeLogger());

        var sw = Stopwatch.StartNew();
        var act = () => resolver.ResolveParagraph("A", null);
        act.Should().NotThrow();
        sw.ElapsedMilliseconds.Should().BeLessThan(100);
    }

    // ── ResolveCharacter tests ────────────────────────────────────────────────

    [Fact]
    public void ResolveCharacter_InheritsFromParagraph()
    {
        var registry = MakeCharacterRegistry(null, null, [], []);
        var resolver = new StyleResolver(registry, MakeFontManager(), MakeLogger());
        var para     = ParagraphStyle.Default with { FontSizePts = 14f };

        var result = resolver.ResolveCharacter(null, null, para);

        result.FontSizePts.Should().BeApproximately(14f, 0.001f);
    }

    [Fact]
    public void ResolveCharacter_OverridesColor()
    {
        var redStyle = new CharacterStyleDef { Id = "RedStyle", Color = "FF0000" };
        var registry = MakeCharacterRegistry(null, null, [],
            [redStyle]);
        var resolver = new StyleResolver(registry, MakeFontManager(), MakeLogger());
        var para     = ParagraphStyle.Default;

        var result = resolver.ResolveCharacter("RedStyle", null, para);

        result.Color.R8.Should().Be(255);
        result.Color.G8.Should().Be(0);
        result.Color.B8.Should().Be(0);
    }

    // ── ColorParser tests ─────────────────────────────────────────────────────

    [Fact]
    public void ColorParser_ValidHex_ParsesCorrectly()
    {
        var color = ColorParser.ParseHex("FF0000");
        color.Should().NotBeNull();
        color!.Value.R8.Should().Be(255);
        color.Value.G8.Should().Be(0);
        color.Value.B8.Should().Be(0);
        color.Value.A8.Should().Be(255);
    }

    [Fact]
    public void ColorParser_LowerCase_ParsesCorrectly()
    {
        var color = ColorParser.ParseHex("ff8800");
        color.Should().NotBeNull();
        color!.Value.R8.Should().Be(255);
        color.Value.G8.Should().Be(136);
        color.Value.B8.Should().Be(0);
    }

    [Fact]
    public void ColorParser_Null_ReturnsNull()
    {
        ColorParser.ParseHex(null).Should().BeNull();
    }

    [Fact]
    public void ColorParser_Empty_ReturnsNull()
    {
        ColorParser.ParseHex("").Should().BeNull();
    }

    [Fact]
    public void ColorParser_InvalidHex_ReturnsNull()
    {
        ColorParser.ParseHex("ZZZZZZ").Should().BeNull();
    }
}
