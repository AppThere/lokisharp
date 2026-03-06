// LAYER:   AppThere.Loki.Kernel — Kernel
// KIND:    Record
// PURPOSE: Encapsulates DPI scaling factors for a display or output target.
//          ScaleX and ScaleY are independent to support non-square pixels
//          (rare but required for some printer outputs).
//          At 96 dpi with 1× scale: 1 logical point = 1 physical pixel.
// DEPENDS: (none)
// USED BY: TileRequest, IRenderSurfaceFactory, TileRenderer
// PHASE:   1

namespace AppThere.Loki.Kernel.Color;

public readonly record struct DpiScale(float ScaleX, float ScaleY)
{
    public static readonly DpiScale Identity  = new(1f, 1f);
    public static readonly DpiScale Retina2x  = new(2f, 2f);
    public static readonly DpiScale Retina3x  = new(3f, 3f);

    public DpiScale(float uniform) : this(uniform, uniform) { }

    public bool IsIdentity => ScaleX == 1f && ScaleY == 1f;

    /// <summary>Physical pixels per logical point on X axis.</summary>
    public float PixelsPerPointX => ScaleX;
    /// <summary>Physical pixels per logical point on Y axis.</summary>
    public float PixelsPerPointY => ScaleY;

    public float ToPhysicalX(float logical) => logical * ScaleX;
    public float ToPhysicalY(float logical) => logical * ScaleY;
    public float ToLogicalX(float physical) => physical / ScaleX;
    public float ToLogicalY(float physical) => physical / ScaleY;

    public override string ToString() =>
        ScaleX == ScaleY ? $"{ScaleX}×" : $"{ScaleX}×{ScaleY}";
}
