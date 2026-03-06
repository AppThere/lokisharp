// LAYER:   AppThere.Loki.Kernel — Kernel
// KIND:    Record
// PURPOSE: Immutable 2D size in logical units (points at 96 dpi).
//          Width and Height are always non-negative; construction enforces this.
// DEPENDS: (none)
// USED BY: PaintScene, IRenderSurface, TileRequest, ILokiPainter
// PHASE:   1

namespace AppThere.Loki.Kernel.Geometry;

public readonly record struct SizeF
{
    public float Width  { get; }
    public float Height { get; }

    public SizeF(float width, float height)
    {
        if (width  < 0f) throw new ArgumentOutOfRangeException(nameof(width),  "Width must be >= 0.");
        if (height < 0f) throw new ArgumentOutOfRangeException(nameof(height), "Height must be >= 0.");
        Width  = width;
        Height = height;
    }

    public static readonly SizeF Zero = new(0f, 0f);

    public bool IsEmpty => Width == 0f && Height == 0f;

    public SizeF Scale(float factor)           => new(Width * factor, Height * factor);
    public SizeF Scale(float sx, float sy)     => new(Width * sx, Height * sy);
    public SizeF Inflate(float dw, float dh)   => new(Width + dw, Height + dh);

    public PointF ToPoint() => new(Width, Height);

    public override string ToString() => $"{Width:F2} × {Height:F2}";
}
