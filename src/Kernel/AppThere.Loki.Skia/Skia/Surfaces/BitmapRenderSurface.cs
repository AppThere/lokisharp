// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Implementation
// PURPOSE: CPU-backed render surface. Wraps SKBitmap (RGBA8888 premultiplied) and an
//          SKCanvas for drawing. No shared state — concurrent instances on separate
//          threads are safe. Produced by HeadlessSurfaceFactory.
//          GetBitmap() returns the backing bitmap; caller does not own it.
// DEPENDS: IRenderSurface, SKBitmap, SKCanvas
// USED BY: HeadlessSurfaceFactory, TileRenderer
// PHASE:   1
// ADR:     ADR-002

using SkiaSharp;

namespace AppThere.Loki.Skia.Surfaces;

public sealed class BitmapRenderSurface : IRenderSurface
{
    private readonly SKBitmap _bitmap;
    private readonly SKCanvas _canvas;
    private          bool     _disposed;

    public int WidthPx  { get; }
    public int HeightPx { get; }

    public BitmapRenderSurface(int widthPx, int heightPx)
    {
        if (widthPx  <= 0) throw new ArgumentOutOfRangeException(nameof(widthPx),  "Width must be > 0.");
        if (heightPx <= 0) throw new ArgumentOutOfRangeException(nameof(heightPx), "Height must be > 0.");

        WidthPx  = widthPx;
        HeightPx = heightPx;

        var info = new SKImageInfo(widthPx, heightPx, SKColorType.Rgba8888, SKAlphaType.Premul);
        _bitmap = new SKBitmap(info);
        _canvas = new SKCanvas(_bitmap);
    }

    public void Flush()
    {
        ThrowIfDisposed();
        _canvas.Flush();
    }

    /// <summary>
    /// Returns the backing bitmap. Caller does not own it — do not dispose.
    /// </summary>
    public SKBitmap GetBitmap()
    {
        ThrowIfDisposed();
        return _bitmap;
    }

    /// <summary>
    /// Returns the underlying canvas. Caller does not own it — do not dispose.
    /// </summary>
    public SKCanvas GetCanvas()
    {
        ThrowIfDisposed();
        return _canvas;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(BitmapRenderSurface));
    }
}
