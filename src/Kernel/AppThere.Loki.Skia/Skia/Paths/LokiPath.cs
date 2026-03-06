// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Implementation
// PURPOSE: Immutable vector path built from PathVerbs.
//          Constructed via LokiPath.Builder; frozen at Build().
//          Lazily converts to SKPath on first render (cached, thread-safe).
//          SKPath is transient — not serialised. Reconstructed on deserialisation.
// DEPENDS: PathVerb, RectF, PointF
// USED BY: PathNode, ILokiPainter.DrawPath
// PHASE:   1
// ADR:     ADR-003

using System.Collections.Immutable;
using AppThere.Loki.Kernel.Geometry;
using SkiaSharp;

namespace AppThere.Loki.Skia.Paths;

public sealed class LokiPath
{
    public ImmutableArray<PathVerb> Verbs   { get; }
    public RectF                    Bounds  { get; }
    public SKPathFillType           FillType { get; }

    // Lazily computed, thread-safe SKPath cache
    private readonly Lazy<SKPath> _skPath;

    private LokiPath(ImmutableArray<PathVerb> verbs, RectF bounds, SKPathFillType fillType)
    {
        Verbs    = verbs;
        Bounds   = bounds;
        FillType = fillType;
        _skPath  = new Lazy<SKPath>(BuildSkPath, isThreadSafe: true);
    }

    public SKPath ToSkiaPath() => _skPath.Value;

    private SKPath BuildSkPath()
    {
        var p = new SKPath { FillType = FillType };
        foreach (var verb in Verbs)
        {
            switch (verb)
            {
                case MoveToVerb  v: p.MoveTo(v.P.X, v.P.Y);                                   break;
                case LineToVerb  v: p.LineTo(v.P.X, v.P.Y);                                   break;
                case CubicToVerb v: p.CubicTo(v.C1.X,v.C1.Y,v.C2.X,v.C2.Y,v.P.X,v.P.Y);    break;
                case QuadToVerb  v: p.QuadTo(v.Control.X,v.Control.Y,v.P.X,v.P.Y);           break;
                case ConicToVerb v: p.ConicTo(v.Control.X,v.Control.Y,v.P.X,v.P.Y,v.W);     break;
                case CloseVerb:     p.Close();                                                 break;
            }
        }
        return p;
    }

    public static Builder CreateBuilder() => new();

    public sealed class Builder
    {
        private readonly List<PathVerb> _verbs = new();
        private SKPathFillType _fill = SKPathFillType.Winding;

        public Builder WithFillType(SKPathFillType fill) { _fill = fill; return this; }
        public Builder MoveTo(PointF p)                  { _verbs.Add(new MoveToVerb(p)); return this; }
        public Builder LineTo(PointF p)                  { _verbs.Add(new LineToVerb(p)); return this; }
        public Builder CubicTo(PointF c1,PointF c2,PointF p) { _verbs.Add(new CubicToVerb(c1,c2,p)); return this; }
        public Builder QuadTo(PointF ctrl,PointF p)      { _verbs.Add(new QuadToVerb(ctrl,p)); return this; }
        public Builder ConicTo(PointF ctrl,PointF p,float w) { _verbs.Add(new ConicToVerb(ctrl,p,w)); return this; }
        public Builder Close()                           { _verbs.Add(new CloseVerb()); return this; }

        public LokiPath Build()
        {
            var verbs = _verbs.ToImmutableArray();
            // Compute bounds from a temporary SKPath
            using var tmp = new LokiPath(verbs, RectF.Empty, _fill).BuildSkPath();
            var b = tmp.Bounds;
            var bounds = new RectF(b.Left, b.Top, b.Width, b.Height);
            return new LokiPath(verbs, bounds, _fill);
        }
    }
}
