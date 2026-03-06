// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Interface
// PURPOSE: Resolves FontDescriptors to typefaces using the two-tier strategy:
//          bundled variable fonts are checked first, then system fonts.
//          Does NOT perform text shaping — that is LokiTextShaper's responsibility.
//          Does NOT download fonts — that is IFontDownloadProvider's responsibility.
// DEPENDS: FontDescriptor, FontFamilyInfo, FontAxis (Kernel)
// USED BY: LokiTextShaper, LokiSkiaPainter — injected via DI
// PHASE:   1
// ADR:     ADR-001

using AppThere.Loki.Kernel.Fonts;

namespace AppThere.Loki.Skia.Fonts;

public interface IFontManager
{
    /// <summary>All font families bundled with the application.</summary>
    IReadOnlyList<FontFamilyInfo> GetBundledFamilies();

    /// <summary>All font families available from the OS font manager.</summary>
    IReadOnlyList<FontFamilyInfo> GetSystemFamilies();

    /// <summary>
    /// Resolves a FontDescriptor to a typeface.
    /// Checks bundled fonts first, then system fonts.
    /// Returns false if no typeface can be found for the given descriptor.
    /// Never throws — returns false on any resolution failure.
    /// </summary>
    bool TryGetTypeface(FontDescriptor descriptor, out ILokiTypeface? typeface);

    /// <summary>Returns the best available fallback typeface for the given Unicode script.</summary>
    ILokiTypeface GetFallbackForScript(UnicodeScript script);

    /// <summary>
    /// Returns variable font axes for the given family, if it is a variable font.
    /// Returns false for static/non-variable fonts.
    /// </summary>
    bool TryGetVariableAxes(string familyName, out IReadOnlyList<FontAxis>? axes);

    /// <summary>Registers a font from an embedded assembly resource stream.</summary>
    void RegisterEmbedded(string resourceName, Stream data);

    /// <summary>Attempts to download and register a font family. Phase 1: always returns false.</summary>
    Task<bool> TryDownloadFamilyAsync(string familyName, CancellationToken ct = default);
}

public enum UnicodeScript
{
    Latin, Arabic, Hebrew, Devanagari, CjkUnifiedIdeographs, Emoji, Unknown
}
