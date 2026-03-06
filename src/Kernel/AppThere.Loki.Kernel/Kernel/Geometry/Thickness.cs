// LAYER:   AppThere.Loki.Kernel — Kernel
// KIND:    Record
// PURPOSE: Immutable margin/padding/border thickness (logical units).
//          Models CSS box-model spacing: Left, Top, Right, Bottom.
// DEPENDS: (none)
// USED BY: PaintStyle (border), layout engine (Phase 3+)
// PHASE:   1

namespace AppThere.Loki.Kernel.Geometry;

public readonly record struct Thickness(float Left, float Top, float Right, float Bottom)
{
    public static readonly Thickness Zero = new(0f, 0f, 0f, 0f);

    public Thickness(float uniform) : this(uniform, uniform, uniform, uniform) { }
    public Thickness(float horizontal, float vertical) : this(horizontal, vertical, horizontal, vertical) { }

    public float Horizontal => Left + Right;
    public float Vertical   => Top  + Bottom;

    public bool IsZero => Left == 0f && Top == 0f && Right == 0f && Bottom == 0f;

    public override string ToString() => $"L={Left} T={Top} R={Right} B={Bottom}";
}
