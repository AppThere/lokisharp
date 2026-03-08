// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Interface
// PURPOSE: Platform-neutral drawing API. The sole entry point for all rendering.
//          Wraps an SKCanvas without exposing SkiaSharp types to callers.
//          All draw calls are synchronous and single-threaded per surface.
//          Implemented by LokiSkiaPainter. Used by TileRenderer.
// DEPENDS: RectF, PointF, SizeF, LokiColor, PaintStyle, TextPaint, LinePaint,
//          GlyphRun, LokiPath (all from Kernel/Skia)
// USED BY: TileRenderer — the only caller in Phase 1
// PHASE:   1
// ADR:     ADR-002, ADR-003

using AppThere.Loki.Kernel.Color;
using AppThere.Loki.Kernel.Geometry;
using AppThere.Loki.Skia.Fonts;
using AppThere.Loki.Skia.Paths;
using AppThere.Loki.Skia.Scene;

namespace AppThere.Loki.Skia.Painting;

public interface ILokiPainter
{
    // ── State ─────────────────────────────────────────────────────────────────
    void Save();
    void Restore();
    void ClipRect(RectF rect);
    void SetTransform(float sx, float sy, float tx, float ty); // scale + translate only in Phase 1
    void Clear(LokiColor color);

    // ── Geometry ──────────────────────────────────────────────────────────────
    void DrawRect(RectF bounds, PaintStyle fill, PaintStyle? stroke = null);
    void DrawRoundRect(RectF bounds, float rx, float ry, PaintStyle fill, PaintStyle? stroke = null);
    void DrawLine(PointF a, PointF b, LinePaint paint);
    void DrawPath(LokiPath path, PaintStyle fill, PaintStyle? stroke = null);

    // ── Images ────────────────────────────────────────────────────────────────
    void DrawImage(RectF destBounds, ImageRef image, float opacity = 1f, ImageFit fit = ImageFit.Contain);

    // ── Text ─────────────────────────────────────────────────────────────────
    void DrawGlyphRun(PointF origin, GlyphRun run, TextPaint paint);

    // ── Groups / effects ──────────────────────────────────────────────────────
    void BeginGroup(RectF bounds, float opacity = 1f, RectF? clip = null);
    void EndGroup();
    void DrawShadow(RectF contentBounds, PointF offset, float blurRadius, LokiColor color);
}
