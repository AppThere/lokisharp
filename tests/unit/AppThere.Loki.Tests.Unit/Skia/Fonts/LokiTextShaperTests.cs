// LAYER:   AppThere.Loki.Tests.Unit — Tests
// KIND:    Tests
// PURPOSE: Specification tests for LokiTextShaper.
//          Verifies the Latin fast path (single run), typeface boundary detection
//          (multiple runs when primary font lacks a glyph), advance scaling with
//          font size, and empty-input handling.
//          IFontManager is mocked via NSubstitute; SkiaTypeface instances wrap
//          real system typefaces so advances and glyph IDs are real non-zero values.
// DEPENDS: LokiTextShaper, IFontManager, ILokiTypeface, SkiaTypeface, GlyphRun,
//          FontDescriptor, NullLokiLogger
// USED BY: CI
// PHASE:   1
// ADR:     ADR-001

using AppThere.Loki.Kernel.Fonts;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.Skia.Fonts;
using AppThere.Loki.Skia.Scene;
using AppThere.Loki.Tests.Unit.Skia;
using FluentAssertions;
using NSubstitute;
using SkiaSharp;

namespace AppThere.Loki.Tests.Unit.Skia.Fonts;

public sealed class LokiTextShaperTests : IDisposable
{
    // Real typefaces so advances and glyph IDs are real non-zero values.
    private readonly SkiaTypeface    _primaryTypeface;
    private readonly SkiaTypeface    _fallbackTypeface;
    private readonly IFontManager    _mockFm;
    private readonly LokiTextShaper  _sut;

    private const string LatinWord   = "hello";
    private const string LatinPhrase = "hello world";

    // U+E001 is in the Private Use Area — no standard font maps it, so
    // ContainsGlyph reliably returns false for any system serif/sans-serif font.
    private const string FallbackChar = "\uE001";

    static LokiTextShaperTests()
    {
        SkiaTestInitializer.EnsureSkiaSharpLoaded();
    }

    public LokiTextShaperTests()
    {
        _primaryTypeface  = new SkiaTypeface(
            SKTypeface.FromFamilyName("serif"),     isBundled: false, ownsTypeface: true, isVariable: false);
        _fallbackTypeface = new SkiaTypeface(
            SKTypeface.FromFamilyName("sans-serif"), isBundled: false, ownsTypeface: true, isVariable: false);

        _mockFm = Substitute.For<IFontManager>();

        // Default: TryGetTypeface → primary.
        ILokiTypeface? tfOut = null;
        _mockFm.TryGetTypeface(Arg.Any<FontDescriptor>(), out tfOut)
               .Returns(ci => { ci[1] = _primaryTypeface; return true; });

        // Default: any fallback request → fallback typeface.
        _mockFm.GetFallbackForScript(Arg.Any<UnicodeScript>())
               .Returns(_fallbackTypeface);

        _sut = new LokiTextShaper(_mockFm, NullLokiLogger.Instance);
    }

    public void Dispose()
    {
        _primaryTypeface.Dispose();
        _fallbackTypeface.Dispose();
    }

    // ── Empty / whitespace ────────────────────────────────────────────────────

    [Fact]
    public void Shape_EmptyString_ReturnsEmptyList()
    {
        var runs = _sut.Shape("", FontDescriptor.Default);

        runs.Should().BeEmpty();
    }

    [Fact]
    public void Shape_WhitespaceOnly_ReturnsSingleRunWithSpaceGlyphs()
    {
        var runs = _sut.Shape("   ", FontDescriptor.Default);

        runs.Should().HaveCount(1);
        runs[0].GlyphIds.Should().HaveCount(3);
    }

    // ── Single-run Latin ──────────────────────────────────────────────────────

    [Fact]
    public void Shape_SingleLatinWord_ReturnsSingleRun()
    {
        var runs = _sut.Shape(LatinWord, FontDescriptor.Default);

        runs.Should().HaveCount(1);
    }

    [Fact]
    public void Shape_SingleLatinWord_AllAdvancesPositive()
    {
        var runs = _sut.Shape(LatinWord, FontDescriptor.Default);

        runs[0].Advances.Should().OnlyContain(a => a > 0f);
    }

    [Fact]
    public void Shape_SingleLatinWord_GlyphCountMatchesCharCount()
    {
        var runs = _sut.Shape(LatinWord, FontDescriptor.Default);

        runs[0].GlyphIds.Length.Should().Be(LatinWord.Length);
    }

    [Fact]
    public void Shape_MultipleWords_ReturnsSingleRun()
    {
        var runs = _sut.Shape(LatinPhrase, FontDescriptor.Default);

        runs.Should().HaveCount(1);
    }

    [Fact]
    public void Shape_SingleRun_AdvancesAndGlyphIdsAreSameLength()
    {
        var runs = _sut.Shape(LatinWord, FontDescriptor.Default);

        runs[0].Advances.Length.Should().Be(runs[0].GlyphIds.Length);
    }

    // ── Fallback ──────────────────────────────────────────────────────────────

    [Fact]
    public void Shape_TextRequiringFallback_ReturnsMultipleRuns()
    {
        // Primary font never contains PUA codepoints → triggers fallback.
        var runs = _sut.Shape(LatinWord + FallbackChar, FontDescriptor.Default);

        runs.Count.Should().BeGreaterThan(1);
    }

    [Fact]
    public void Shape_TextRequiringFallback_EachRunHasDistinctTypeface()
    {
        var runs = _sut.Shape(LatinWord + FallbackChar, FontDescriptor.Default);

        // Adjacent runs must not share the same family name.
        for (var i = 0; i + 1 < runs.Count; i++)
            runs[i].Typeface.FamilyName.Should().NotBe(runs[i + 1].Typeface.FamilyName);
    }

    [Fact]
    public void Shape_TextRequiringFallback_TotalGlyphCountMatchesInputLength()
    {
        var text = LatinWord + FallbackChar;   // all BMP chars, 1 char = 1 glyph
        var runs = _sut.Shape(text, FontDescriptor.Default);

        var totalGlyphs = runs.Sum(r => r.GlyphIds.Length);
        totalGlyphs.Should().Be(text.Length);
    }

    // ── Font size ─────────────────────────────────────────────────────────────

    [Fact]
    public void Shape_FontSizeAffectsAdvances()
    {
        var small = FontDescriptor.Default with { SizeInPoints = 12f };
        var large = FontDescriptor.Default with { SizeInPoints = 24f };

        var smallAdvance = _sut.Shape(LatinWord, small)[0].TotalAdvance;
        var largeAdvance = _sut.Shape(LatinWord, large)[0].TotalAdvance;

        smallAdvance.Should().BeLessThan(largeAdvance);
    }
}
