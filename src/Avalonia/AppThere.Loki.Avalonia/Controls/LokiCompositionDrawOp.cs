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
using AppThere.Loki.Kernel.Color;

namespace AppThere.Loki.Avalonia.Controls;

public sealed class LokiCompositionDrawOp
{
    private static readonly IBrush _placeholder =
        new SolidColorBrush(Color.FromRgb(220, 220, 220));

    /// <summary>
    /// Immutable snapshot of tiles to draw this frame.
    /// Each entry is (screen rect in DIPs, bitmap).
    /// Built by LokiTileControl before calling Render().
    /// </summary>
    public IReadOnlyList<PositionedTile> Tiles { get; }

    /// <summary>Caret positions and visibility for this frame.</summary>
    public IReadOnlyList<CaretRenderInfo> Carets { get; }

    /// <summary>Total bounds of the draw area.</summary>
    public Rect Bounds { get; }

    public LokiCompositionDrawOp(IReadOnlyList<PositionedTile> tiles, IReadOnlyList<CaretRenderInfo> carets, Rect bounds)
    {
        Tiles  = tiles;
        Carets = carets;
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
            if (tile.IsPageGap)
            {
                context.FillRectangle(new SolidColorBrush(Color.FromRgb(128, 128, 128)), tile.ScreenRect);
            }
            else if (tile.Bitmap is not null)
            {
                context.DrawImage(tile.Bitmap, tile.ScreenRect);
            }
            else
            {
                context.FillRectangle(new SolidColorBrush(Color.FromRgb(230, 230, 230)), tile.ScreenRect);
            }
        }

        foreach (var caret in Carets)
        {
            if (!caret.IsVisible) continue;
            var brush = new SolidColorBrush(Color.FromRgb(caret.Color.R8, caret.Color.G8, caret.Color.B8));
            context.DrawRectangle(brush, null, caret.Rect);
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
    WriteableBitmap?  Bitmap,       // null = not yet rendered
    bool              IsPageGap = false);

public sealed record CaretRenderInfo(
    Rect          Rect,
    bool          IsLocal,
    LokiColor     Color,
    bool          IsVisible);
