// LAYER:   AppThere.Loki.Avalonia — Controls
// KIND:    Class (Avalonia Control)
// PURPOSE: The primary document rendering control. Displays a scrollable
//          tiled view of an ILokiView. Owns ILokiTileCache (one per
//          control instance). Responds to scroll, resize, and zoom changes
//          by updating the viewport geometry and scheduling tile renders.
//          Implements ICustomHitTest so pointer events pass through to
//          Avalonia's input system.
//          Mobile-first: touch pan/pinch-to-zoom are handled natively
//          via Avalonia's gesture recognizers.
// DEPENDS: ILokiView, ILokiTileCache, TileCacheOptions,
//          LokiCompositionDrawOp, ViewportGeometry
// USED BY: LokiDocumentPage (Avalonia page/view)
// PHASE:   4
// ADR:     ADR-010, ADR-011

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using AppThere.Loki.LokiKit.View;

namespace AppThere.Loki.Avalonia.Controls;

public sealed class LokiTileControl : Control, ICustomHitTest
{
    // ── Avalonia StyledProperties ────────────────────────────────────────────

    /// <summary>The document view to render. Set by the parent page.</summary>
    public static readonly StyledProperty<ILokiView?> DocumentViewProperty =
        AvaloniaProperty.Register<LokiTileControl, ILokiView?>(nameof(DocumentView));

    /// <summary>Current zoom level. 1.0 = 100%. Clamped to [0.25, 4.0].</summary>
    public static readonly StyledProperty<float> ZoomProperty =
        AvaloniaProperty.Register<LokiTileControl, float>(nameof(Zoom), defaultValue: 1.0f);

    /// <summary>Scroll offset in document points.</summary>
    public static readonly StyledProperty<Vector> ScrollOffsetProperty =
        AvaloniaProperty.Register<LokiTileControl, Vector>(nameof(ScrollOffset));

    public ILokiView? DocumentView
    {
        get => GetValue(DocumentViewProperty);
        set => SetValue(DocumentViewProperty, value);
    }

    public float Zoom
    {
        get => GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    public Vector ScrollOffset
    {
        get => GetValue(ScrollOffsetProperty);
        set => SetValue(ScrollOffsetProperty, value);
    }

    // ── Constructor ──────────────────────────────────────────────────────────

    /// <summary>
    /// Constructor: takes TileCacheOptions so the cache is configured at
    /// construction time. In production, injected via DI-aware control
    /// factory. In tests, constructed directly.
    /// </summary>
    public LokiTileControl(TileCacheOptions options)
    {
        // Implementation: create LokiTileCache, subscribe to TileReady,
        // register gesture recognizers, subscribe to property changes.
        throw new NotImplementedException("Implemented by Claude Code");
    }

    // ── Rendering ────────────────────────────────────────────────────────────

    /// <summary>
    /// Avalonia render override. Builds a LokiCompositionDrawOp from the
    /// current tile snapshot and passes it to the DrawingContext.
    /// Called on the render thread — must not access DI or async operations.
    /// </summary>
    public override void Render(DrawingContext context)
    {
        throw new NotImplementedException("Implemented by Claude Code");
    }

    // ── Layout ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the total size of the document in DIPs at current zoom.
    /// Avalonia calls this during layout to size scroll containers.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        throw new NotImplementedException("Implemented by Claude Code");
    }

    // ── ICustomHitTest ───────────────────────────────────────────────────────

    public bool HitTest(Point point) => Bounds.Contains(point);

    // ── Internal ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Recomputes ViewportGeometry from current Bounds, ScrollOffset, Zoom,
    /// and DocumentView, then calls cache.UpdateViewport.
    /// Called on scroll, resize, and zoom change.
    /// </summary>
    private void UpdateViewport() =>
        throw new NotImplementedException("Implemented by Claude Code");

    /// <summary>
    /// Builds the PositionedTile list for the current viewport.
    /// Queries the cache for each tile in the visible grid.
    /// </summary>
    private IReadOnlyList<PositionedTile> BuildTileSnapshot() =>
        throw new NotImplementedException("Implemented by Claude Code");
}
