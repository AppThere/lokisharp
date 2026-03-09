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
using AppThere.Loki.Avalonia.Cache;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.LokiKit.View;

namespace AppThere.Loki.Avalonia.Controls;

public sealed class LokiTileControl : Control
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

    // ── Fields ───────────────────────────────────────────────────────────────

    private readonly TileCacheOptions      _options;
    private LokiTileCache?                 _cache;
    private ViewportGeometry?              _viewport;
    private LokiCompositionDrawOp?         _currentDrawOp;

    // ── Constructor ──────────────────────────────────────────────────────────

    public LokiTileControl(TileCacheOptions options)
    {
        _options = options;
        GestureRecognizers.Add(new PinchGestureRecognizer());
    }

    // ── Property change dispatch ─────────────────────────────────────────────

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DocumentViewProperty)
        {
            OnDocumentViewChanged(
                change.GetOldValue<ILokiView?>(),
                change.GetNewValue<ILokiView?>());
        }
        else if (change.Property == ZoomProperty ||
                 change.Property == ScrollOffsetProperty)
        {
            UpdateViewport();
            InvalidateVisual();
        }
        else if (change.Property == BoundsProperty)
        {
            UpdateViewport();
            InvalidateVisual();
        }
    }

    // ── Rendering ────────────────────────────────────────────────────────────

    public override void Render(DrawingContext context)
    {
        var snapshot = BuildTileSnapshot();
        _currentDrawOp?.Dispose();
        _currentDrawOp = new LokiCompositionDrawOp(snapshot, new Rect(Bounds.Size));
        _currentDrawOp.Render(context);
    }

    // ── Layout ───────────────────────────────────────────────────────────────

    protected override Size MeasureOverride(Size availableSize)
    {
        var view = DocumentView;
        if (view is null) return new Size(0, 0);
        var size = view.GetPartSize(0);
        var zoom = Zoom;
        return new Size(size.Width * zoom, size.Height * zoom);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private void UpdateViewport()
    {
        var view = DocumentView;
        if (view is null || Bounds.Width <= 0 || Bounds.Height <= 0) return;

        var scroll = ScrollOffset;
        var zoom   = Zoom;
        var vp = new ViewportGeometry(
            PartIndex:         0,
            ViewportWidthPts:  (float)Bounds.Width,
            ViewportHeightPts: (float)Bounds.Height,
            ScrollOffsetXPts:  (float)scroll.X,
            ScrollOffsetYPts:  (float)scroll.Y,
            Zoom:              zoom,
            TileSizePx:        _options.TileSizePx);
        _viewport = vp;
        _cache?.UpdateViewport(vp);
    }

    private IReadOnlyList<PositionedTile> BuildTileSnapshot()
    {
        var vp   = _viewport;
        var view = DocumentView;
        if (vp is null || view is null) return Array.Empty<PositionedTile>();

        float docW = view.GetPartSize(vp.PartIndex).Width;
        float docH = view.GetPartSize(vp.PartIndex).Height;
        var keys   = TileGridMath.TilesForViewport(vp, docW, docH).ToList();

        var result = new List<PositionedTile>(keys.Count);
        foreach (var key in keys)
        {
            var screenRect = TileGridMath.ScreenRect(key, vp);
            var bitmap     = _cache?.TryGetTile(key);
            result.Add(new PositionedTile(key, screenRect, bitmap));
        }
        return result;
    }

    private void OnDocumentViewChanged(ILokiView? oldView, ILokiView? newView)
    {
        if (_cache is { } old)
        {
            old.TileReady -= OnTileReady;
            _ = old.DisposeAsync().AsTask();
        }
        _cache = null;

        if (newView is null) return;

        _cache = new LokiTileCache(newView, _options, NullLokiLogger.Instance);
        _cache.TileReady += OnTileReady;
        UpdateViewport();
        InvalidateVisual();
    }

    private void OnTileReady(object? sender, Cache.TileKey key) =>
        InvalidateVisual();

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_cache is { } c)
        {
            c.TileReady -= OnTileReady;
            _ = c.DisposeAsync().AsTask();
            _cache = null;
        }
        _currentDrawOp?.Dispose();
        _currentDrawOp = null;
    }

    // ── Ctrl+scroll zoom ─────────────────────────────────────────────────────

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            var delta = e.Delta.Y > 0 ? 1.1f : 1f / 1.1f;
            Zoom = Math.Clamp(Zoom * delta, 0.25f, 4.0f);
            e.Handled = true;
        }
        // Non-Ctrl scroll: not handled — falls through to ScrollViewer.
    }
}
