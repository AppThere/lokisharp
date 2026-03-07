// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Implementation
// PURPOSE: Implements IFontManager. Registers bundled variable fonts from embedded
//          assembly resources on construction. Resolves FontDescriptors using a
//          two-tier strategy: bundled fonts first, then SKFontManager.Default (system).
//          Does NOT perform text shaping (LokiTextShaper's responsibility).
//          TryDownloadFamilyAsync always returns false in Phase 1.
// DEPENDS: IFontManager, ILokiTypeface, SkiaTypeface, FontFamilyInfo, FontAxis,
//          FontDescriptor, ILokiLogger, SKTypeface, SKFontManager
// USED BY: LokiTextShaper, LokiSkiaPainter — injected via DI
// PHASE:   1
// ADR:     ADR-001

using System.Collections.Immutable;
using System.Reflection;
using SkiaSharp;
using AppThere.Loki.Kernel.Fonts;
using AppThere.Loki.Kernel.Logging;

namespace AppThere.Loki.Skia.Fonts;

public sealed class SkiaFontManager : IFontManager
{
    private readonly ILokiLogger _logger;

    // Owned SKTypeface objects — disposed when manager is GC'd via finaliser (no IDisposable on IFontManager).
    private readonly List<SKTypeface> _owned = new();

    // Bundled typefaces grouped by family name (case-insensitive).
    private readonly Dictionary<string, List<SKTypeface>> _bundled =
        new(StringComparer.OrdinalIgnoreCase);

    // Pre-parsed variable axis metadata per bundled family.
    private readonly Dictionary<string, IReadOnlyList<FontAxis>> _axes =
        new(StringComparer.OrdinalIgnoreCase);

    // Pre-resolved fallback typefaces per Unicode script.
    private readonly Dictionary<UnicodeScript, SKTypeface> _fallbacks = new();

    public SkiaFontManager(ILokiLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        RegisterAllEmbedded();
        InitFallbacks();
    }

    // ── Registration ─────────────────────────────────────────────────────────

    private void RegisterAllEmbedded()
    {
        var asm = typeof(SkiaFontManager).Assembly;
        foreach (var name in asm.GetManifestResourceNames())
        {
            if (!name.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)) continue;
            using var stream = asm.GetManifestResourceStream(name);
            if (stream is null) continue;
            RegisterEmbedded(name, stream);
        }
    }

    public void RegisterEmbedded(string resourceName, Stream data)
    {
        SKTypeface? tf = null;
        try
        {
            tf = SKTypeface.FromStream(data);
            if (tf is null)
            {
                _logger.Warn("Could not load typeface from resource: {0}", resourceName);
                return;
            }

            string family = tf.FamilyName;
            if (!_bundled.TryGetValue(family, out var list))
            {
                list = new List<SKTypeface>();
                _bundled[family] = list;
            }
            list.Add(tf);
            _owned.Add(tf);

            var parsedAxes = ParseFvarAxes(tf);
            if (parsedAxes.Count > 0 && !_axes.ContainsKey(family))
                _axes[family] = parsedAxes;

            _logger.Debug("Registered bundled font '{0}' from {1}", family, resourceName);
        }
        catch (Exception ex)
        {
            tf?.Dispose();
            _logger.Error("Failed to register font resource '{0}'", ex, resourceName);
        }
    }

    private void InitFallbacks()
    {
        SetFallback(UnicodeScript.Arabic,              "Noto Sans Arabic");
        SetFallback(UnicodeScript.CjkUnifiedIdeographs, "Noto Sans SC");
        SetFallback(UnicodeScript.Emoji,               "Noto Color Emoji");
        SetFallback(UnicodeScript.Latin,               "Inter");
        SetFallback(UnicodeScript.Hebrew,              "Inter");
        SetFallback(UnicodeScript.Devanagari,          "Inter");
        SetFallback(UnicodeScript.Unknown,             "Inter");
    }

    private void SetFallback(UnicodeScript script, string familyName)
    {
        if (_bundled.TryGetValue(familyName, out var list) && list.Count > 0)
            _fallbacks[script] = list[0];
        else
            _logger.Warn("Bundled fallback family not found for script {0}: '{1}'", script, familyName);
    }

    // ── IFontManager ─────────────────────────────────────────────────────────

    public IReadOnlyList<FontFamilyInfo> GetBundledFamilies()
    {
        var result = ImmutableArray.CreateBuilder<FontFamilyInfo>(_bundled.Count);
        foreach (var (family, faces) in _bundled)
        {
            bool isVar = _axes.ContainsKey(family);
            var  ax    = _axes.TryGetValue(family, out var found) ? found : ImmutableArray<FontAxis>.Empty;
            result.Add(new FontFamilyInfo(family, isVar, IsBundled: true, ax));
        }
        return result.ToImmutable();
    }

    public IReadOnlyList<FontFamilyInfo> GetSystemFamilies()
    {
        var names  = SKFontManager.Default.GetFontFamilies();
        var result = ImmutableArray.CreateBuilder<FontFamilyInfo>(names.Length);
        foreach (var name in names)
            result.Add(new FontFamilyInfo(name, IsVariable: false, IsBundled: false, ImmutableArray<FontAxis>.Empty));
        return result.ToImmutable();
    }

    public bool TryGetTypeface(FontDescriptor descriptor, out ILokiTypeface? typeface)
    {
        typeface = null;
        try
        {
            // Tier 1 — bundled fonts.
            if (_bundled.TryGetValue(descriptor.FamilyName, out var list) && list.Count > 0)
            {
                var best = FindBestMatch(list, descriptor);
                bool isVar = _axes.ContainsKey(descriptor.FamilyName);
                typeface = new SkiaTypeface(best, isBundled: true, ownsTypeface: false, isVar, descriptor.Weight);
                return true;
            }

            // Tier 2 — system fonts.
            var style = new SKFontStyle((int)descriptor.Weight, 5, MapSlant(descriptor.Slant));
            var sk    = SKFontManager.Default.MatchFamily(descriptor.FamilyName, style);
            if (sk is null) return false;

            // Reject substitutes returned by Skia when the family is unknown.
            if (!sk.FamilyName.Equals(descriptor.FamilyName, StringComparison.OrdinalIgnoreCase))
            {
                sk.Dispose();
                return false;
            }

            bool sysIsVar = ParseFvarAxes(sk).Count > 0;
            typeface = new SkiaTypeface(sk, isBundled: false, ownsTypeface: true, sysIsVar);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error("Error resolving typeface for '{0}'", ex, descriptor.FamilyName);
            return false;
        }
    }

    public ILokiTypeface GetFallbackForScript(UnicodeScript script)
    {
        if (_fallbacks.TryGetValue(script, out var sk))
        {
            bool isVar = _axes.ContainsKey(sk.FamilyName);
            return new SkiaTypeface(sk, isBundled: true, ownsTypeface: false, isVar);
        }

        // Last resort — return Inter if somehow the script is unmapped.
        if (_bundled.TryGetValue("Inter", out var inter) && inter.Count > 0)
            return new SkiaTypeface(inter[0], isBundled: true, ownsTypeface: false, _axes.ContainsKey("Inter"));

        throw new InvalidOperationException(
            "No fallback typeface available — bundled fonts were not loaded correctly.");
    }

    public bool TryGetVariableAxes(string familyName, out IReadOnlyList<FontAxis>? axes)
    {
        if (_axes.TryGetValue(familyName, out var found) && found.Count > 0)
        {
            axes = found;
            return true;
        }
        axes = null;
        return false;
    }

    public Task<bool> TryDownloadFamilyAsync(string familyName, CancellationToken ct = default)
        => Task.FromResult(false);

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static SKTypeface FindBestMatch(List<SKTypeface> candidates, FontDescriptor descriptor)
    {
        if (candidates.Count == 1) return candidates[0];
        var targetSlant  = MapSlant(descriptor.Slant);
        int targetWeight = (int)descriptor.Weight;
        return candidates
            .OrderBy(f => Math.Abs(f.FontStyle.Weight - targetWeight))
            .ThenBy(f => f.FontStyle.Slant == targetSlant ? 0 : 1)
            .First();
    }

    private static SKFontStyleSlant MapSlant(FontSlant slant) => slant switch
    {
        FontSlant.Italic  => SKFontStyleSlant.Italic,
        FontSlant.Oblique => SKFontStyleSlant.Oblique,
        _                 => SKFontStyleSlant.Upright,
    };

    /// <summary>
    /// Parses OpenType fvar table to extract variable font axes.
    /// fvar spec: https://docs.microsoft.com/en-us/typography/opentype/spec/fvar
    /// </summary>
    private static IReadOnlyList<FontAxis> ParseFvarAxes(SKTypeface typeface)
    {
        const uint FvarTag = 0x66766172; // 'fvar'
        var data = typeface.GetTableData(FvarTag);
        if (data is null || data.Length < 16) return ImmutableArray<FontAxis>.Empty;

        // fvar header (all values big-endian):
        //   offset 0: majorVersion (uint16) = 1
        //   offset 2: minorVersion (uint16) = 0
        //   offset 4: offsetToAxesArray (uint16)
        //   offset 6: reserved (uint16)
        //   offset 8: axisCount (uint16)
        //   offset 10: axisSize (uint16) = 20
        int offset    = (data[4] << 8) | data[5];
        int axisCount = (data[8] << 8) | data[9];
        int axisSize  = (data[10] << 8) | data[11];

        if (axisCount <= 0 || axisSize < 20 || offset + axisCount * axisSize > data.Length)
            return ImmutableArray<FontAxis>.Empty;

        var builder = ImmutableArray.CreateBuilder<FontAxis>(axisCount);
        for (int i = 0; i < axisCount; i++)
        {
            int pos = offset + i * axisSize;
            // Each axis record (20 bytes):
            //   offset 0:  axisTag (uint32, 4 ASCII chars)
            //   offset 4:  minValue (Fixed 16.16)
            //   offset 8:  defaultValue (Fixed 16.16)
            //   offset 12: maxValue (Fixed 16.16)
            string tag = new(new[] { (char)data[pos], (char)data[pos+1], (char)data[pos+2], (char)data[pos+3] });
            float  min = ReadFixed(data, pos +  4);
            float  def = ReadFixed(data, pos +  8);
            float  max = ReadFixed(data, pos + 12);
            builder.Add(new FontAxis(tag, tag, min, max, def));
        }
        return builder.ToImmutable();
    }

    private static float ReadFixed(byte[] data, int offset)
    {
        int raw = (data[offset] << 24) | (data[offset + 1] << 16)
                | (data[offset + 2] << 8) | data[offset + 3];
        return raw / 65536f;
    }
}
