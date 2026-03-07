// LAYER:   AppThere.Loki.Tests.Unit — Tests
// KIND:    Tests
// PURPOSE: Specification tests for LokiTextShaper.
//          Verifies the Latin fast path (single run), typeface boundary detection
//          (multiple runs when primary font lacks a glyph), advance scaling with
//          font size, and empty-input handling.
//          IFontManager is mocked via NSubstitute; SkiaTypeface instances wrap
//          real system typefaces so advances and glyph IDs are real non-zero values.
// DEPENDS: LokiTextShaper, BidiAnalyser, IFontManager, ILokiTypeface, SkiaTypeface,
//          SkiaFontManager, GlyphRun, FontDescriptor, NullLokiLogger
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

// ReSharper disable StringLiteralTypo

namespace AppThere.Loki.Tests.Unit.Skia.Fonts;

public sealed class LokiTextShaperTests : IDisposable
{
    // _primaryTypeface is a controlled mock so ContainsGlyph and FamilyName are
    // predictable on every platform.  On Windows, generic family names ("serif",
    // "sans-serif") can resolve to the same underlying typeface — giving identical
    // FamilyNames — so the accumulator boundary check never fires and the whole
    // string collapses into one run.  Some Windows fonts also contain PUA glyphs,
    // making ContainsGlyph(U+E001) return true.  Using a mock removes both hazards.
    //
    // _fallbackTypeface wraps a real SKTypeface so HarfBuzz shaping in the
    // real-shaper tests (CreateRealShaper) gets authentic advance values.
    private readonly ILokiTypeface   _primaryTypeface;
    private readonly SkiaTypeface    _fallbackTypeface;
    private readonly IFontManager    _mockFm;
    private readonly LokiTextShaper  _sut;

    private const string LatinWord   = "hello";
    private const string LatinPhrase = "hello world";

    // U+E001 is in the Private Use Area.  The mock primary explicitly returns
    // false for this codepoint; the real fallback typeface is used instead.
    private const string FallbackChar      = "\uE001";
    private const int    FallbackCodepoint = 0xE001;

    static LokiTextShaperTests()
    {
        SkiaTestInitializer.EnsureSkiaSharpLoaded();
    }

    public LokiTextShaperTests()
    {
        // Mock primary: known family name, contains all Latin codepoints,
        // does NOT contain FallbackCodepoint.
        var mockPrimary = Substitute.For<ILokiTypeface>();
        mockPrimary.FamilyName.Returns("LokiTestSerif");
        mockPrimary.ContainsGlyph(Arg.Is<int>(cp => cp != FallbackCodepoint)).Returns(true);
        // FallbackCodepoint falls through to NSubstitute's default (false).
        _primaryTypeface = mockPrimary;

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

    // ── BiDi ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Shape_LatinOnly_IsRtlFalse()
    {
        var runs = _sut.Shape(LatinWord, FontDescriptor.Default);

        runs.Should().OnlyContain(r => r.IsRtl == false);
    }

    [Fact]
    public void Shape_ArabicOnly_IsRtlTrue()
    {
        var sut  = CreateRealShaper();
        var runs = sut.Shape("مرحبا", FontDescriptor.Default);

        runs.Should().NotBeEmpty();
        runs.Should().OnlyContain(r => r.IsRtl);
    }

    [Fact]
    public void Shape_MixedLatinArabic_HasRtlRun()
    {
        var sut  = CreateRealShaper();
        var runs = sut.Shape("hello مرحبا", FontDescriptor.Default);

        runs.Should().Contain(r => r.IsRtl);
    }

    [Fact]
    public void Shape_MixedLatinArabic_HasLtrRun()
    {
        var sut  = CreateRealShaper();
        var runs = sut.Shape("hello مرحبا", FontDescriptor.Default);

        runs.Should().Contain(r => !r.IsRtl);
    }

    // ── HarfBuzz / Arabic shaping ─────────────────────────────────────────────

    [Fact]
    public void Shape_LatinWord_GlyphIdsNonZero_WithRealFont()
    {
        var sut  = CreateRealShaper();
        var runs = sut.Shape(LatinWord, FontDescriptor.Default);

        // HarfBuzz must assign valid (non-zero) glyph IDs via the real font manager.
        runs.Should().HaveCount(1);
        runs[0].GlyphIds.Should().NotContain((ushort)0);
    }

    [Fact]
    public void Shape_ArabicWord_AllAdvancesNonNegative()
    {
        var sut  = CreateRealShaper();
        var runs = sut.Shape("مرحبا", FontDescriptor.Default);

        foreach (var run in runs)
            run.Advances.Should().OnlyContain(a => a >= 0f);
    }

    [Fact]
    public void Shape_ArabicWord_TotalAdvancePositive()
    {
        var sut         = CreateRealShaper();
        var runs        = sut.Shape("مرحبا", FontDescriptor.Default);
        var totalAdvance = runs.Sum(r => r.TotalAdvance);

        totalAdvance.Should().BeGreaterThan(0f);
    }

    [Fact]
    public void Shape_ArabicWord_GlyphIdsNonZero()
    {
        var sut  = CreateRealShaper();
        var runs = sut.Shape("مرحبا", FontDescriptor.Default);

        // HarfBuzz should assign valid (non-zero) glyph IDs for Arabic text.
        runs.Should().NotBeEmpty();
        runs[0].GlyphIds.Should().NotContain((ushort)0);
    }

    [Fact]
    public void Shape_ArabicWord_FontSizeAffectsAdvances()
    {
        var sut   = CreateRealShaper();
        var small = FontDescriptor.Default with { SizeInPoints = 12f };
        var large = FontDescriptor.Default with { SizeInPoints = 24f };

        var smallAdv = sut.Shape("مرحبا", small).Sum(r => r.TotalAdvance);
        var largeAdv = sut.Shape("مرحبا", large).Sum(r => r.TotalAdvance);

        smallAdv.Should().BeLessThan(largeAdv);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static LokiTextShaper CreateRealShaper()
    {
        var fm = new SkiaFontManager(NullLokiLogger.Instance);
        return new LokiTextShaper(fm, NullLokiLogger.Instance);
    }
}
