// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Implementation
// PURPOSE: Shapes Unicode text into one or more GlyphRuns using the Latin fast path
//          (SKFont.GetGlyphs + GetGlyphWidths). Splits runs at typeface boundaries
//          when a codepoint is absent from the primary typeface, calling
//          IFontManager.GetFallbackForScript to obtain the correct fallback.
//          BiDi reordering is deferred to Task 4 — IsRtl is always false here.
//          Does NOT expose SKFont or SKTypeface to callers above this layer.
// DEPENDS: IFontManager, ILokiTypeface, SkiaTypeface, GlyphRun, FontDescriptor,
//          ILokiLogger, FontResolutionException
// USED BY: LokiSkiaPainter, TileRenderer — injected via DI
// PHASE:   1
// ADR:     ADR-001

using System.Collections.Immutable;
using System.Text;
using AppThere.Loki.Kernel.Errors;
using AppThere.Loki.Kernel.Fonts;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.Skia.Scene;
using SkiaSharp;

namespace AppThere.Loki.Skia.Fonts;

public sealed class LokiTextShaper
{
    private readonly IFontManager _fontManager;
    private readonly ILokiLogger  _logger;

    public LokiTextShaper(IFontManager fontManager, ILokiLogger logger)
    {
        _fontManager = fontManager ?? throw new ArgumentNullException(nameof(fontManager));
        _logger      = logger      ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Shapes <paramref name="text"/> using <paramref name="font"/>.
    /// Returns an empty list for empty input. Returns one GlyphRun per
    /// contiguous block of codepoints sharing the same resolved typeface.
    /// </summary>
    public IReadOnlyList<GlyphRun> Shape(string text, FontDescriptor font)
    {
        if (string.IsNullOrEmpty(text))
            return ImmutableArray<GlyphRun>.Empty;

        if (!_fontManager.TryGetTypeface(font, out var primary) || primary == null)
            throw new FontResolutionException(font.FamilyName,
                $"No typeface found for '{font.FamilyName}'.");

        var runs  = new List<GlyphRun>();
        var accum = new TextSegmentAccumulator();

        for (var i = 0; i < text.Length;)
        {
            var isSurrogatePair = char.IsHighSurrogate(text[i])
                                  && i + 1 < text.Length
                                  && char.IsLowSurrogate(text[i + 1]);
            var charCount = isSurrogatePair ? 2 : 1;
            var codepoint = isSurrogatePair
                ? char.ConvertToUtf32(text[i], text[i + 1])
                : text[i];

            var typeface = primary.ContainsGlyph(codepoint)
                ? primary
                : _fontManager.GetFallbackForScript(DetectScript(codepoint));

            if (accum.HasPending && accum.CurrentFamily != typeface.FamilyName)
                runs.Add(accum.Flush(font.SizeInPoints));

            accum.Append(text.Substring(i, charCount), typeface);
            i += charCount;
        }

        if (accum.HasPending)
            runs.Add(accum.Flush(font.SizeInPoints));

        return runs;
    }

    // ── Script detection ──────────────────────────────────────────────────────

    private static UnicodeScript DetectScript(int codepoint) => codepoint switch
    {
        >= 0x0000 and <= 0x024F => UnicodeScript.Latin,
        >= 0x0590 and <= 0x05FF => UnicodeScript.Hebrew,
        >= 0x0600 and <= 0x06FF => UnicodeScript.Arabic,
        >= 0x0900 and <= 0x097F => UnicodeScript.Devanagari,
        >= 0x3000 and <= 0x9FFF => UnicodeScript.CjkUnifiedIdeographs,
        >= 0x1F300 and <= 0x1FAFF => UnicodeScript.Emoji,
        _ => UnicodeScript.Unknown,
    };

    // ── TextSegmentAccumulator ────────────────────────────────────────────────

    private sealed class TextSegmentAccumulator
    {
        private readonly StringBuilder _sb       = new();
        private          ILokiTypeface? _typeface;

        public bool   HasPending     => _sb.Length > 0;
        public string CurrentFamily  => _typeface?.FamilyName ?? string.Empty;

        public void Append(string chars, ILokiTypeface typeface)
        {
            _typeface = typeface;
            _sb.Append(chars);
        }

        public GlyphRun Flush(float sizeInPoints)
        {
            var text     = _sb.ToString();
            var typeface = _typeface!;
            _sb.Clear();
            _typeface = null;
            return ShapeSegment(typeface, text, sizeInPoints);
        }

        private static GlyphRun ShapeSegment(ILokiTypeface loki, string text, float size)
        {
            var skTypeface = ResolveSkTypeface(loki);
            using var font = new SKFont(skTypeface, size);

            // Count glyphs, allocate buffers, then fill in one pass each.
            var count    = font.CountGlyphs(text);
            var glyphIds = new ushort[count];
            font.GetGlyphs(text, glyphIds.AsSpan());

            var advances = new float[count];
            font.GetGlyphWidths(glyphIds.AsSpan(), advances.AsSpan(),
                                Span<SKRect>.Empty, paint: null);

            return new GlyphRun(
                loki, size,
                ImmutableArray.Create(glyphIds),
                ImmutableArray.Create(advances),
                OffsetX: null,
                OffsetY: null,
                IsRtl:   false);
        }

        private static SKTypeface ResolveSkTypeface(ILokiTypeface loki)
        {
            if (loki is SkiaTypeface st) return st.Inner;

            // Fallback for test doubles: resolve by family name from the system.
            return SKFontManager.Default.MatchFamily(loki.FamilyName)
                   ?? SKTypeface.Default;
        }
    }
}
