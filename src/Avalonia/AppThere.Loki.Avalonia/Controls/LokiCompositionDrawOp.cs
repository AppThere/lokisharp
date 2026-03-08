// LAYER:   AppThere.Loki.Avalonia — Controls
// KIND:    Class (Avalonia ICustomDrawOperation implementation)
// PURPOSE: Called by Avalonia on the render thread in response to
//          InvalidateVisual(). Holds an immutable snapshot of positioned
//          tile bitmaps. Iterates the snapshot and draws each tile via
//          DrawingContext.DrawImage. Does NO tile rendering itself.
//          Created fresh on each InvalidateVisual() call with the
//          current tile snapshot — old instances are disposed after the
//          frame completes.
// DEPENDS: ILokiTileCache, TileKey, TileCacheOptions
// USED BY: LokiTileControl.OnRender
// PHASE:   4
// ADR:     ADR-010, ADR-011

using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Skia;
using SkiaSharp;

namespace AppThere.Loki.Avalonia.Controls;

public sealed class LokiCompositionDrawOp : ICustomDrawOperation
{
    /// <summary>
    /// Immutable snapshot of tiles to draw this frame.
    /// Each entry is (screen rect in DIPs, bitmap).
    /// Built by LokiTileControl before calling InvalidateVisual().
    /// </summary>
    public IReadOnlyList<PositionedTile> Tiles { get; }

    /// <summary>Total bounds of the draw operation (required by Avalonia).</summary>
    public Avalonia.Rect Bounds { get; }

    public LokiCompositionDrawOp(
        IReadOnlyList<PositionedTile> tiles,
        Avalonia.Rect bounds)
    {
        Tiles  = tiles;
        Bounds = bounds;
    }

    /// <summary>
    /// Called by Avalonia on the render thread.
    /// Draws white background, then each tile bitmap at its screen rect.
    /// </summary>
    public void Render(ImmediateDrawingContext context)
    {
        var skia = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
        if (skia is null) return;

        using var lease  = skia.Lease();
        var canvas = lease.SkCanvas;

        canvas.Clear(SKColors.White);

        foreach (var tile in Tiles)
        {
            var r      = tile.ScreenRect;
            var skRect = SKRect.Create((float)r.X, (float)r.Y, (float)r.Width, (float)r.Height);

            if (tile.Bitmap is not null)
            {
                using var fb = tile.Bitmap.Lock();
                var info = new SKImageInfo(
                    tile.Bitmap.PixelSize.Width, tile.Bitmap.PixelSize.Height,
                    SKColorType.Bgra8888, SKAlphaType.Premul);
                var bmp = new SKBitmap();
                bmp.InstallPixels(info, fb.Address, fb.RowBytes);
                canvas.DrawBitmap(bmp, skRect);
                bmp.Dispose();
            }
            else
            {
                using var paint = new SKPaint { Color = new SKColor(220, 220, 220) };
                canvas.DrawRect(skRect, paint);
            }
        }
    }

    public bool HitTest(Avalonia.Point p) => Bounds.Contains(p);
    public bool Equals(ICustomDrawOperation? other) => ReferenceEquals(this, other);
    public void Dispose() { /* tiles are owned by cache, not disposed here */ }
}

/// <summary>
/// A tile positioned in screen space (device-independent pixels).
/// Bitmap may be null if the tile is not yet rendered (placeholder drawn instead).
/// </summary>
public sealed record PositionedTile(
    TileKey            Key,
    Avalonia.Rect      ScreenRect,    // in DIPs
    WriteableBitmap?   Bitmap);       // null = not yet rendered
