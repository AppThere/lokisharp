// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Implementation
// PURPOSE: Shapes Unicode text into one or more GlyphRuns using HarfBuzz via
//          SkiaSharp.HarfBuzz (SKShaper). Splits runs at typeface boundaries when
//          a codepoint is absent from the primary typeface. Runs a lightweight
//          Unicode BiDi scan (BidiAnalyser) to set IsRtl on each run.
//          Does NOT expose SKFont, SKTypeface, or HarfBuzz types to callers.
// DEPENDS: IFontManager, ILokiTypeface, SkiaTypeface, GlyphRun, FontDescriptor,
//          BidiAnalyser, ILokiLogger, FontResolutionException
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
using SkiaSharp.HarfBuzz; // BlobExtensions.ToHarfBuzzBlob
using HB = HarfBuzzSharp;

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
            var isRtl = BidiAnalyser.HasStrongRtl(text.AsSpan());
            return ShapeSegment(typeface, text, sizeInPoints, isRtl);
        }

        private static GlyphRun ShapeSegment(ILokiTypeface loki, string text, float size, bool isRtl)
        {
            var skTypeface = ResolveSkTypeface(loki);

            using var buffer = new HB.Buffer();
            buffer.AddUtf16(text);
            buffer.Direction = isRtl ? HB.Direction.RightToLeft : HB.Direction.LeftToRight;
            buffer.GuessSegmentProperties();

            // Build the HarfBuzz font directly so GlyphPositions remain readable after
            // shaping. (SKShaper.Shape consumes the buffer and zeroes its positions
            // before it returns, making buffer.GlyphPositions unusable afterwards.)
            int ttcIndex;
            using var stream = skTypeface.OpenStream(out ttcIndex);
            using var blob   = stream.ToHarfBuzzBlob();
            using var face   = new HB.Face(blob, (uint)ttcIndex);
            using var font   = new HB.Font(face);
            font.SetFunctionsOpenType();
            var upem = (int)face.UnitsPerEm;
            font.SetScale(upem, upem); // unscaled: XAdvance values are in design units

            font.Shape(buffer);

            var glyphInfos = buffer.GlyphInfos;
            var positions  = buffer.GlyphPositions;
            var n          = glyphInfos.Length;

            var glyphIds = new ushort[n];
            for (var i = 0; i < n; i++)
                glyphIds[i] = (ushort)glyphInfos[i].Codepoint;

            // Convert design-unit advances to Skia points: pts = designUnits * size / upem.
            var advances = new float[n];
            for (var i = 0; i < n; i++)
                advances[i] = positions[i].XAdvance * size / upem;

            return new GlyphRun(
                loki, size,
                ImmutableArray.Create(glyphIds),
                ImmutableArray.Create(advances),
                OffsetX: null,
                OffsetY: null,
                IsRtl:   isRtl);
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
