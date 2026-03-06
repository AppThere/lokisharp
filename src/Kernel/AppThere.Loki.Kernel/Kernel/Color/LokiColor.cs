// LAYER:   AppThere.Loki.Kernel — Kernel
// KIND:    Record
// PURPOSE: Immutable RGBA colour in linear float space (0.0–1.0 per channel).
//          Named LokiColor to avoid collision with System.Drawing.Color.
//          Use LokiColor.FromArgb32 for 8-bit-per-channel construction.
// DEPENDS: (none)
// USED BY: PaintStyle, TextPaint, LinePaint, ShadowNode
// PHASE:   1

namespace AppThere.Loki.Kernel.Color;

public readonly record struct LokiColor(float R, float G, float B, float A = 1.0f)
{
    // Common colours
    public static readonly LokiColor Black       = new(0f,    0f,    0f,    1f);
    public static readonly LokiColor White       = new(1f,    1f,    1f,    1f);
    public static readonly LokiColor Transparent = new(0f,    0f,    0f,    0f);
    public static readonly LokiColor Red         = new(1f,    0f,    0f,    1f);
    public static readonly LokiColor Green       = new(0f,    0.502f,0f,    1f);
    public static readonly LokiColor Blue        = new(0f,    0f,    1f,    1f);

    /// <summary>Construct from 8-bit ARGB values (0–255).</summary>
    public static LokiColor FromArgb32(byte a, byte r, byte g, byte b) =>
        new(r / 255f, g / 255f, b / 255f, a / 255f);

    /// <summary>Construct from hex string: "RRGGBB" or "AARRGGBB".</summary>
    public static LokiColor FromHex(string hex)
    {
        hex = hex.TrimStart('#');
        return hex.Length switch
        {
            6 => FromArgb32(255,
                     Convert.ToByte(hex[0..2], 16),
                     Convert.ToByte(hex[2..4], 16),
                     Convert.ToByte(hex[4..6], 16)),
            8 => FromArgb32(
                     Convert.ToByte(hex[0..2], 16),
                     Convert.ToByte(hex[2..4], 16),
                     Convert.ToByte(hex[4..6], 16),
                     Convert.ToByte(hex[6..8], 16)),
            _ => throw new FormatException($"Unrecognised hex colour: #{hex}")
        };
    }

    public byte A8 => (byte)(A * 255f);
    public byte R8 => (byte)(R * 255f);
    public byte G8 => (byte)(G * 255f);
    public byte B8 => (byte)(B * 255f);
    public uint ToArgb32() => ((uint)A8 << 24) | ((uint)R8 << 16) | ((uint)G8 << 8) | B8;

    public LokiColor WithAlpha(float alpha) => this with { A = alpha };
    public LokiColor Lerp(LokiColor other, float t) =>
        new(R + (other.R - R) * t, G + (other.G - G) * t,
            B + (other.B - B) * t, A + (other.A - A) * t);

    public override string ToString() =>
        $"#{A8:X2}{R8:X2}{G8:X2}{B8:X2}";
}
