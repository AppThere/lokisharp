// LAYER:   AppThere.Loki.Kernel — Kernel
// KIND:    Record
// PURPOSE: Immutable axis-aligned rectangle in logical units.
//          Defined by top-left origin (X, Y) and dimensions (Width, Height).
//          Right = X + Width; Bottom = Y + Height.
// DEPENDS: PointF, SizeF
// USED BY: PaintNode, ILokiPainter, TileRequest, PaintBand
// PHASE:   1

namespace AppThere.Loki.Kernel.Geometry;

public readonly record struct RectF
{
    public float X      { get; }
    public float Y      { get; }
    public float Width  { get; }
    public float Height { get; }

    public RectF(float x, float y, float width, float height)
    {
        if (width  < 0f) throw new ArgumentOutOfRangeException(nameof(width));
        if (height < 0f) throw new ArgumentOutOfRangeException(nameof(height));
        X = x; Y = y; Width = width; Height = height;
    }

    public static RectF FromLTRB(float l, float t, float r, float b) =>
        new(l, t, r - l, b - t);

    public static RectF FromCenterSize(PointF center, SizeF size) =>
        new(center.X - size.Width / 2f, center.Y - size.Height / 2f, size.Width, size.Height);

    public static readonly RectF Empty = new(0f, 0f, 0f, 0f);

    public float   Left        => X;
    public float   Top         => Y;
    public float   Right       => X + Width;
    public float   Bottom      => Y + Height;
    public PointF  TopLeft     => new(X, Y);
    public PointF  TopRight    => new(Right, Y);
    public PointF  BottomLeft  => new(X, Bottom);
    public PointF  BottomRight => new(Right, Bottom);
    public PointF  Center      => new(X + Width / 2f, Y + Height / 2f);
    public SizeF   Size        => new(Width, Height);
    public bool    IsEmpty     => Width == 0f || Height == 0f;

    public bool Contains(PointF p) =>
        p.X >= X && p.X < Right && p.Y >= Y && p.Y < Bottom;

    public bool IntersectsWith(RectF other) =>
        other.Left < Right && other.Right > Left &&
        other.Top  < Bottom && other.Bottom > Top;

    public RectF Intersect(RectF other)
    {
        var l = MathF.Max(Left,  other.Left);
        var t = MathF.Max(Top,   other.Top);
        var r = MathF.Min(Right, other.Right);
        var b = MathF.Min(Bottom,other.Bottom);
        return r > l && b > t ? FromLTRB(l, t, r, b) : Empty;
    }

    public RectF Union(RectF other) =>
        FromLTRB(MathF.Min(Left, other.Left), MathF.Min(Top, other.Top),
                 MathF.Max(Right, other.Right), MathF.Max(Bottom, other.Bottom));

    public RectF Inflate(float dx, float dy) =>
        new(X - dx, Y - dy, Width + dx * 2f, Height + dy * 2f);

    public RectF Offset(float dx, float dy) => new(X + dx, Y + dy, Width, Height);
    public RectF Offset(PointF d)           => Offset(d.X, d.Y);

    public override string ToString() => $"[{X:F2}, {Y:F2}, {Width:F2}×{Height:F2}]";
}
