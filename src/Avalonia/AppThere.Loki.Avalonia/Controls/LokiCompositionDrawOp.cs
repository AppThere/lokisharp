// LAYER:   AppThere.Loki.Avalonia — Controls
// KIND:    Class (Avalonia draw helper)
// PURPOSE: Called by LokiTileControl.Render(). Holds an immutable snapshot of
//          positioned tile bitmaps. Draws white background, then each tile bitmap
//          at its screen rect via DrawingContext.DrawImage. Does NO tile rendering.
//          Created fresh on each Render() call — old instances are disposed after
//          the frame completes.
// DEPENDS: TileKey
// USED BY: LokiTileControl.Render
// PHASE:   4
// ADR:     ADR-010, ADR-011

using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AppThere.Loki.Avalonia.Cache;

namespace AppThere.Loki.Avalonia.Controls;

public sealed class LokiCompositionDrawOp
{
    private static readonly IBrush _placeholder =
        new ImmutableSolidColorBrush(Color.FromRgb(220, 220, 220));

    /// <summary>
    /// Immutable snapshot of tiles to draw this frame.
    /// Each entry is (screen rect in DIPs, bitmap).
    /// Built by LokiTileControl before calling Render().
    /// </summary>
    public IReadOnlyList<PositionedTile> Tiles { get; }

    /// <summary>Total bounds of the draw area.</summary>
    public Rect Bounds { get; }

    public LokiCompositionDrawOp(IReadOnlyList<PositionedTile> tiles, Rect bounds)
    {
        Tiles  = tiles;
        Bounds = bounds;
    }

    /// <summary>
    /// Draws white background, then each tile bitmap at its screen rect.
    /// Called by LokiTileControl on the UI thread during Render().
    /// </summary>
    public void Render(DrawingContext context)
    {
        context.FillRectangle(Brushes.White, Bounds);

        foreach (var tile in Tiles)
        {
            if (tile.Bitmap is not null)
                context.DrawImage(tile.Bitmap, tile.ScreenRect);
            else
                context.FillRectangle(_placeholder, tile.ScreenRect);
        }
    }

    public void Dispose() { /* tiles are owned by cache, not disposed here */ }
}

/// <summary>
/// A tile positioned in screen space (device-independent pixels).
/// Bitmap may be null if the tile is not yet rendered (placeholder drawn instead).
/// </summary>
public sealed record PositionedTile(
    TileKey           Key,
    Rect              ScreenRect,   // in DIPs
    WriteableBitmap?  Bitmap);      // null = not yet rendered
