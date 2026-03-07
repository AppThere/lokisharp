// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Implementation
// PURPOSE: Lightweight Unicode BiDi paragraph analyser for Phase 1.
//          Scans a text span for strong right-to-left codepoints (Hebrew, Arabic,
//          Syriac, Thaana, NKo, and related presentation-form blocks) to determine
//          the paragraph base direction. Full UAX #9 BiDi is deferred to Phase 4.
// DEPENDS: (none — pure .NET)
// USED BY: LokiTextShaper
// PHASE:   1
// ADR:     ADR-001

namespace AppThere.Loki.Skia.Fonts;

internal static class BidiAnalyser
{
    /// <summary>
    /// Returns <c>true</c> if <paramref name="text"/> contains at least one
    /// strong RTL codepoint (Arabic, Hebrew, Syriac, Thaana, NKo, and related
    /// presentation-form blocks). Returns <c>false</c> for empty spans.
    /// </summary>
    internal static bool HasStrongRtl(ReadOnlySpan<char> text)
    {
        for (var i = 0; i < text.Length;)
        {
            int cp;
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length
                                               && char.IsLowSurrogate(text[i + 1]))
            {
                cp = char.ConvertToUtf32(text[i], text[i + 1]);
                i += 2;
            }
            else
            {
                cp = text[i];
                i++;
            }

            if (IsStrongRtl(cp)) return true;
        }

        return false;
    }

    // ── Unicode strong RTL ranges (UAX #9 bidi categories R, AL, AN) ─────────

    private static bool IsStrongRtl(int cp) =>
        (cp >= 0x0590 && cp <= 0x05FF) ||   // Hebrew
        (cp >= 0x0600 && cp <= 0x08FF) ||   // Arabic, Syriac, Thaana, NKo, Samaritan…
        (cp >= 0xFB1D && cp <= 0xFB4F) ||   // Hebrew presentation forms
        (cp >= 0xFB50 && cp <= 0xFDFF) ||   // Arabic presentation forms A
        (cp >= 0xFE70 && cp <= 0xFEFF);     // Arabic presentation forms B
}
