// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Implementation
// PURPOSE: CPU tile renderer and PDF exporter. Implements ITileRenderer.
//          RenderTileAsync: maps a TileRequest to a logical rect, creates a
//          BitmapRenderSurface, constructs LokiSkiaPainter, applies the
//          scale+translate transform, culls bands and nodes against the tile rect,
//          then returns an independent copy of the rendered bitmap.
//          RenderToPdfAsync: renders all scenes to a single PDF stream using
//          PdfRenderSurface, flushing between scenes to advance pages.
//          Node dispatch is handled by DispatchNode (exhaustive pattern match).
// DEPENDS: ITileRenderer, TileRequest, PaintScene, PaintBand, PaintNode,
//          LokiSkiaPainter, IRenderSurfaceFactory, BitmapRenderSurface,
//          PdfRenderSurface, IImageStore, ILokiLogger, TileRenderException,
//          PdfRenderException, LokiColor, RectF, SizeF
// USED BY: lokiprint CLI, TileRendererTests
// PHASE:   1
// ADR:     ADR-002, ADR-003, ADR-004

using AppThere.Loki.Kernel.Color;
using AppThere.Loki.Kernel.Geometry;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.Skia.Errors;
using AppThere.Loki.Skia.Images;
using AppThere.Loki.Skia.Painting;
using AppThere.Loki.Skia.Scene;
using AppThere.Loki.Skia.Scene.Nodes;
using AppThere.Loki.Skia.Surfaces;
using SkiaSharp;

namespace AppThere.Loki.Skia.Rendering;

public sealed class TileRenderer : ITileRenderer
{
    private readonly IRenderSurfaceFactory _factory;
    private readonly IImageStore           _imageStore;
    private readonly ILokiLogger           _logger;

    public TileRenderer(IRenderSurfaceFactory factory, IImageStore imageStore, ILokiLogger logger)
    {
        _factory    = factory    ?? throw new ArgumentNullException(nameof(factory));
        _imageStore = imageStore ?? throw new ArgumentNullException(nameof(imageStore));
        _logger     = logger     ?? throw new ArgumentNullException(nameof(logger));
    }

    // ── RenderTileAsync ───────────────────────────────────────────────────────

    public Task<SKBitmap> RenderTileAsync(PaintScene scene, TileRequest request,
                                          CancellationToken ct = default)
        => Task.Run(() => RenderTileCore(scene, request, ct), ct);

    private SKBitmap RenderTileCore(PaintScene scene, TileRequest request, CancellationToken ct)
    {
        float tileOriginX  = request.TileCol * request.PixelSize / request.ZoomLevel;
        float tileOriginY  = request.TileRow * request.PixelSize / request.ZoomLevel;
        float tileWidthPts = request.PixelSize / request.ZoomLevel;
        float tileHeightPts= request.PixelSize / request.ZoomLevel;
        var   tileRect     = new RectF(tileOriginX, tileOriginY, tileWidthPts, tileHeightPts);

        IRenderSurface? surface = null;
        try
        {
            surface = _factory.CreateHeadlessBitmapSurface(
                new SizeF(request.PixelSize, request.PixelSize));
            var bitmapSurface = (BitmapRenderSurface)surface;
            var painter       = new LokiSkiaPainter(bitmapSurface.GetCanvas(), _imageStore, _logger);

            ct.ThrowIfCancellationRequested();

            painter.Clear(LokiColor.White);
            painter.SetTransform(
                request.ZoomLevel, request.ZoomLevel,
                -tileOriginX * request.ZoomLevel,
                -tileOriginY * request.ZoomLevel);

            foreach (var band in scene.Bands)
            {
                var bandRect = new RectF(0f, band.YStart, scene.SizeInPoints.Width, band.Height);
                if (!bandRect.IntersectsWith(tileRect)) continue;

                foreach (var node in band.Nodes)
                {
                    if (!node.Bounds.IntersectsWith(tileRect)) continue;
                    DispatchNode(painter, node);
                }
            }

            surface.Flush();
            var result = bitmapSurface.GetBitmap().Copy();
            surface.Dispose();
            surface = null;
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            surface?.Dispose();
            throw new TileRenderException(
                request.PartIndex, request.TileCol, request.TileRow,
                "Tile render failed", ex);
        }
        finally
        {
            surface?.Dispose();
        }
    }

    // ── RenderToPdfAsync ──────────────────────────────────────────────────────

    public Task RenderToPdfAsync(IReadOnlyList<PaintScene> scenes, Stream output,
                                  PdfMetadata meta, CancellationToken ct = default)
        => Task.Run(() => RenderToPdfCore(scenes, output, meta, ct), ct);

    private void RenderToPdfCore(IReadOnlyList<PaintScene> scenes, Stream output,
                                  PdfMetadata meta, CancellationToken ct)
    {
        if (scenes.Count == 0) return;

        PdfRenderSurface? surface = null;
        try
        {
            surface = (PdfRenderSurface)_factory.CreatePdfSurface(
                output, scenes[0].SizeInPoints, meta);

            for (var i = 0; i < scenes.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var scene   = scenes[i];
                var painter = new LokiSkiaPainter(surface.Canvas, _imageStore, _logger);

                painter.Clear(LokiColor.White);
                painter.SetTransform(1f, 1f, 0f, 0f);

                foreach (var band in scene.Bands)
                    foreach (var node in band.Nodes)
                        DispatchNode(painter, node);

                if (i < scenes.Count - 1)
                    surface.Flush();
            }

            surface.Close();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new PdfRenderException("PDF render failed", inner: ex);
        }
        finally
        {
            surface?.Dispose();
        }
    }

    // ── Node dispatch ─────────────────────────────────────────────────────────

    private void DispatchNode(ILokiPainter painter, PaintNode node)
    {
        switch (node)
        {
            case RectNode n:
                painter.DrawRect(n.Bounds, n.Fill, n.Stroke);
                break;

            case RoundRectNode n:
                painter.DrawRoundRect(n.Bounds, n.RadiusX, n.RadiusY, n.Fill, n.Stroke);
                break;

            case LineNode n:
                painter.DrawLine(n.A, n.B, n.Paint);
                break;

            case PathNode n:
                painter.DrawPath(n.Path, n.Fill, n.Stroke);
                break;

            case ImageNode n:
                painter.DrawImage(n.Bounds, n.Image, n.Opacity, n.Fit);
                break;

            case GlyphRunNode n:
                foreach (var run in n.Runs)
                    painter.DrawGlyphRun(n.Origin, run, n.Paint);
                break;

            case ShadowNode n:
                painter.DrawShadow(n.Bounds, n.Offset, n.BlurRadius, n.ShadowColor);
                DispatchNode(painter, n.Content);
                break;

            case GroupNode n:
                painter.BeginGroup(n.Bounds, n.Opacity, n.Clip);
                foreach (var child in n.Children)
                    DispatchNode(painter, child);
                painter.EndGroup();
                break;

            default:
                _logger.Warn("Unknown PaintNode type: {0}", node.GetType().Name);
                break;
        }
    }
}
