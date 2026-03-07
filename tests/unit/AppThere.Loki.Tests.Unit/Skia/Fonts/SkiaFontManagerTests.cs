// LAYER:   AppThere.Loki.Tests.Unit — Tests
// KIND:    Tests
// PURPOSE: Specification tests for SkiaFontManager and SkiaTypeface.
//          Verifies the two-tier font resolution strategy (bundled first, then system),
//          fallback-per-script, variable axis discovery, and Phase-1 stub behaviour.
// DEPENDS: SkiaFontManager, IFontManager, ILokiTypeface, FontDescriptor,
//          FontWeight, FontSlant, NullLokiLogger
// USED BY: CI
// PHASE:   1
// ADR:     ADR-001

using AppThere.Loki.Kernel.Fonts;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.Skia.Fonts;
using AppThere.Loki.Tests.Unit.Skia;
using FluentAssertions;

namespace AppThere.Loki.Tests.Unit.Skia.Fonts;

public sealed class SkiaFontManagerTests : IDisposable
{
    private readonly SkiaFontManager _sut;

    static SkiaFontManagerTests()
    {
        // Ensure the native SkiaSharp library can load in all host environments.
        SkiaTestInitializer.EnsureSkiaSharpLoaded();
    }

    public SkiaFontManagerTests()
    {
        _sut = new SkiaFontManager(NullLokiLogger.Instance);
    }

    public void Dispose()
    {
        // SkiaFontManager owns its SKTypeface objects; no IDisposable on the interface,
        // but we release the manager reference here to allow GC to clean up.
    }

    // ── TryGetTypeface ────────────────────────────────────────────────────────

    [Fact]
    public void TryGetTypeface_BundledInterFamily_ReturnsTypeface()
    {
        var descriptor = new FontDescriptor("Inter");

        var result = _sut.TryGetTypeface(descriptor, out var typeface);

        result.Should().BeTrue();
        typeface.Should().NotBeNull();
    }

    [Fact]
    public void TryGetTypeface_BundledInterFamily_ReturnsBundledTypeface()
    {
        var descriptor = new FontDescriptor("Inter");

        _sut.TryGetTypeface(descriptor, out var typeface);

        typeface!.IsBundled.Should().BeTrue();
    }

    [Fact]
    public void TryGetTypeface_UnknownFamily_ReturnsFalse()
    {
        var descriptor = new FontDescriptor("ThisFamilyDefinitelyDoesNotExist_XYZ");

        var result = _sut.TryGetTypeface(descriptor, out var typeface);

        result.Should().BeFalse();
        typeface.Should().BeNull();
    }

    [Fact]
    public void TryGetTypeface_InterBold_ReturnsWeightBold()
    {
        var descriptor = new FontDescriptor("Inter", FontWeight.Bold);

        _sut.TryGetTypeface(descriptor, out var typeface);

        typeface!.Weight.Should().Be(FontWeight.Bold);
    }

    // ── GetFallbackForScript ──────────────────────────────────────────────────

    [Fact]
    public void GetFallbackForScript_Arabic_ReturnsNotoSansArabic()
    {
        var fallback = _sut.GetFallbackForScript(UnicodeScript.Arabic);

        fallback.FamilyName.Should().Be("Noto Sans Arabic");
    }

    [Fact]
    public void GetFallbackForScript_Latin_ReturnsInter()
    {
        var fallback = _sut.GetFallbackForScript(UnicodeScript.Latin);

        fallback.FamilyName.Should().Be("Inter");
    }

    // ── GetBundledFamilies ────────────────────────────────────────────────────

    [Fact]
    public void GetBundledFamilies_ContainsInter()
    {
        var families = _sut.GetBundledFamilies();

        families.Should().Contain(f => f.Name == "Inter");
    }

    [Fact]
    public void GetBundledFamilies_ContainsNotoSansArabic()
    {
        var families = _sut.GetBundledFamilies();

        families.Should().Contain(f => f.Name == "Noto Sans Arabic");
    }

    // ── TryGetVariableAxes ────────────────────────────────────────────────────

    [Fact]
    public void TryGetVariableAxes_InterFamily_ReturnsWghtAxis()
    {
        var result = _sut.TryGetVariableAxes("Inter", out var axes);

        result.Should().BeTrue();
        axes.Should().NotBeNull();
        axes!.Should().Contain(a => a.Tag == "wght");
    }

    [Fact]
    public void TryGetVariableAxes_UnknownFamily_ReturnsFalse()
    {
        var result = _sut.TryGetVariableAxes("ThisFamilyDefinitelyDoesNotExist_XYZ", out var axes);

        result.Should().BeFalse();
        axes.Should().BeNull();
    }

    // ── TryDownloadFamilyAsync ────────────────────────────────────────────────

    [Fact]
    public async Task TryDownloadFamilyAsync_AnyFamily_ReturnsFalse()
    {
        var result = await _sut.TryDownloadFamilyAsync("Inter");

        result.Should().BeFalse();
    }
}
