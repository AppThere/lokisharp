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
using AppThere.Loki.LokiKit.Host;
using AppThere.Loki.LokiKit.Commands;
using AppThere.Loki.LokiKit.Document;
using AppThere.Loki.LokiKit.Events;
using Avalonia.Threading;
using System.Text;
using System.Linq;
using AppThere.Loki.Skia.Scene.Nodes;

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
    private readonly LokiHostOptions       _hostOptions;
    private LokiTileCache?                 _cache;
    private ViewportGeometry?              _viewport;
    private LokiCompositionDrawOp?         _currentDrawOp;

    private CaretPosition?                 _localCaret;
    private bool                           _caretVisible = true;
    private DispatcherTimer?               _blinkTimer;



    // ── Constructor ──────────────────────────────────────────────────────────

    public LokiTileControl(TileCacheOptions options, LokiHostOptions? hostOptions = null)
    {
        _options     = options;
        _hostOptions = hostOptions ?? LokiHostOptions.Default;
        Focusable    = true;
        GestureRecognizers.Add(new PinchGestureRecognizer());

        _blinkTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(530) };
        _blinkTimer.Tick += (_, _) =>
        {
            _caretVisible = !_caretVisible;
            if (DocumentView != null) InvalidateVisual();
        };
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
        var carets = BuildCaretSnapshot();
        _currentDrawOp?.Dispose();
        _currentDrawOp = new LokiCompositionDrawOp(snapshot, carets, new Rect(Bounds.Size));
        _currentDrawOp.Render(context);
    }

    // ── Layout ───────────────────────────────────────────────────────────────

    protected override Size MeasureOverride(Size availableSize)
    {
        var view = DocumentView;
        if (view is null) return new Size(0, 0);
        
        int pageCount = view.PartCount;
        var size = view.GetPartSize(0);
        var zoom = Zoom;
        
        float canvasH = TileGridMath.CanvasHeightPts(pageCount, size.Height, _hostOptions.PageGapPts) * zoom;
        float canvasW = size.Width * zoom;
        
        return new Size(canvasW, canvasH);
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
        _cache?.UpdateViewport(vp, view.PartCount);
    }

    private IReadOnlyList<PositionedTile> BuildTileSnapshot()
    {
        var vp   = _viewport;
        var view = DocumentView;
        if (vp is null || view is null) return Array.Empty<PositionedTile>();

        int pageCount = view.PartCount;
        float pageSizeH = view.GetPartSize(0).Height;
        float pageSizeW = view.GetPartSize(0).Width;
        float gapPts = _hostOptions.PageGapPts;

        float viewTopPts = (float)(ScrollOffset.Y / Zoom);
        float viewBottomPts = viewTopPts + (float)(Bounds.Height / Zoom);

        var result = new List<PositionedTile>();

        for (int p = 0; p < pageCount; p++)
        {
            float pageTopPts = p * (pageSizeH + gapPts);
            float pageBottomPts = pageTopPts + pageSizeH;

            if (p > 0)
            {
                float gapTopPts = (p - 1) * (pageSizeH + gapPts) + pageSizeH;
                float gapBottomPts = pageTopPts;

                if (gapBottomPts >= viewTopPts && gapTopPts <= viewBottomPts)
                {
                    double gapScreenY = (gapTopPts - vp.ScrollOffsetYPts) * vp.Zoom;
                    double gapScreenX = (0f - vp.ScrollOffsetXPts) * vp.Zoom;
                    double gapScreenHeight = gapPts * vp.Zoom;
                    double gapScreenWidth = pageSizeW * vp.Zoom;

                    var gapRect = new Rect(gapScreenX, gapScreenY, gapScreenWidth, gapScreenHeight);
                    result.Add(new PositionedTile(new AppThere.Loki.Avalonia.Cache.TileKey(p, 0, -1, vp.Zoom), gapRect, null, true));
                }
            }

            if (pageBottomPts < viewTopPts || pageTopPts > viewBottomPts)
                continue;

            var pageVp = vp with { 
                PartIndex = p, 
                ScrollOffsetYPts = Math.Max(0f, vp.ScrollOffsetYPts - pageTopPts) 
            };
            var keys = TileGridMath.TilesForViewport(pageVp, pageSizeW, pageSizeH).ToList();

            foreach (var key in keys)
            {
                var screenRect = TileGridMath.ScreenRect(key, pageVp);
                
                float tileDocX = key.TileX * vp.TileSizePx / vp.Zoom;
                float tileDocY = key.TileY * vp.TileSizePx / vp.Zoom;

                double globalScreenX = (tileDocX - vp.ScrollOffsetXPts) * vp.Zoom;
                double globalScreenY = (tileDocY + pageTopPts - vp.ScrollOffsetYPts) * vp.Zoom;

                var adjustedScreenRect = new Rect(globalScreenX, globalScreenY, vp.TileSizePx, vp.TileSizePx);

                var bitmap = _cache?.TryGetTile(key);
                result.Add(new PositionedTile(key, adjustedScreenRect, bitmap));
            }
        }
        return result;
    }

    private IReadOnlyList<CaretRenderInfo> BuildCaretSnapshot()
    {
        var view = DocumentView;
        var vp = _viewport;
        if (view is null || vp is null) return Array.Empty<CaretRenderInfo>();

        var result = new List<CaretRenderInfo>();
        var carets = view.GetCarets();
        if (carets.Count == 0 || view.Document is not LokiDocumentImpl impl) return result;

        foreach (var entry in carets)
        {
            var p = entry.Selection.Focus.ParagraphIndex;
            var r = entry.Selection.Focus.RunIndex;
            var c = entry.Selection.Focus.CharOffset;
            
            // To approximate we can pull all parts, but part is probably 0 for now.
            // We should find the part containing the paragraph. For Phase 5, all is part 0.
            var scene = view.GetPaintScene(0);
            var node = scene.Bands.SelectMany(b => b.Nodes).OfType<AppThere.Loki.Skia.Scene.Nodes.GlyphRunNode>()
                           .FirstOrDefault(n => n.ParagraphIndex == p && n.RunIndex == r);
            
            float docX = 0f, docY = 0f, docH = 20f; // defaults if not found
            if (node != null)
            {
                float relativeX = node.Text.Length > 0 ? ((float)c / node.Text.Length) * node.Bounds.Width : 0f;
                docX = node.Bounds.Left + relativeX;
                docY = node.Bounds.Top;
                docH = node.Bounds.Height;
            }
            else
            {
                // Try fetching first run of that para
                var paraNode = scene.Bands.SelectMany(b => b.Nodes).OfType<AppThere.Loki.Skia.Scene.Nodes.GlyphRunNode>()
                                    .FirstOrDefault(n => n.ParagraphIndex == p);
                if (paraNode != null)
                {
                    docX = paraNode.Bounds.Left;
                    docY = paraNode.Bounds.Top;
                    docH = paraNode.Bounds.Height;
                }
            }

            double screenX = (docX - vp.ScrollOffsetXPts) * vp.Zoom;
            double screenY = (docY - vp.ScrollOffsetYPts) * vp.Zoom;
            double screenH = docH * vp.Zoom;
            
            bool isLocal = entry.SessionId == _hostOptions.LocalSessionId;
            result.Add(new CaretRenderInfo(
                new Rect(screenX - 1, screenY, 2, screenH),
                isLocal,
                entry.Color,
                _caretVisible || !isLocal
            ));
        }

        return result;
    }

    private void OnDocumentViewChanged(ILokiView? oldView, ILokiView? newView)
    {
        if (oldView != null)
        {
            oldView.LayoutInvalidated -= OnLayoutInvalidated;
        }

        if (_cache is { } old)
        {
            old.TileReady -= OnTileReady;
            _ = old.DisposeAsync().AsTask();
        }
        _cache = null;

        if (newView is null)
        {
            _blinkTimer?.Stop();
            _localCaret = null;
            return;
        }

        _cache = new LokiTileCache(newView, _options, NullLokiLogger.Instance, _hostOptions);
        _cache.TileReady += OnTileReady;

        newView.LayoutInvalidated += OnLayoutInvalidated;

        UpdateViewport();
        InvalidateVisual();
    }

    // Phase 6: replace InvalidateAll with targeted per-tile
    // invalidation using paragraph-to-tile mapping to eliminate
    // the grey flicker on each keystroke.
    private void OnLayoutInvalidated(
        object? sender,
        EngineLayoutInvalidatedEventArgs e)
    {
        _cache?.InvalidateAll();
        Dispatcher.UIThread.Post(() =>
        {
            InvalidateMeasure();
            InvalidateVisual();
        }, DispatcherPriority.Render);
    }

    private void OnTileReady(object? sender, Cache.TileKey key) =>
        InvalidateVisual();

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _blinkTimer?.Stop();
        if (_cache is { } c)
        {
            c.TileReady -= OnTileReady;
            _ = c.DisposeAsync().AsTask();
            _cache = null;
        }
        _currentDrawOp?.Dispose();
        _currentDrawOp = null;
    }

    // ── Input Handling ───────────────────────────────────────────────────────

    private void ResetCaretBlink()
    {
        _caretVisible = true;
        _blinkTimer?.Stop();
        _blinkTimer?.Start();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();

        if (DocumentView == null) return;
        var pos = e.GetPosition(this);
        
        float docX = (float)(pos.X / Zoom) + (float)(ScrollOffset.X / Zoom);
        float docY = (float)(pos.Y / Zoom) + (float)(ScrollOffset.Y / Zoom);
        
        int pageIndex = TileGridMath.PageForCanvasY(
            docY,
            (float)DocumentView.GetPartSize(0).Height,
            _hostOptions.PageGapPts,
            DocumentView.PartCount);
            
        float localY = TileGridMath.LocalYOnPage(
            docY, pageIndex,
            (float)DocumentView.GetPartSize(0).Height,
            _hostOptions.PageGapPts);
            
        var caret = DocumentView.HitTest(docX, localY, pageIndex);
        if (caret != null)
        {
            var sel = Selection.Collapsed(caret);
            DocumentView.SetCaret(sel);
            _localCaret = caret;
            ResetCaretBlink();
            InvalidateVisual();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        var view = DocumentView;
        if (view == null || _localCaret == null) return;

        var sessionId = _hostOptions.LocalSessionId;
        var version   = new DocumentVersion(0);

        bool isCmd = e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta);
        bool isShift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

        switch (e.Key)
        {
            case Key.Back:
                if (_localCaret.CharOffset > 0)
                {
                    var target = new CaretPosition(_localCaret.ParagraphIndex, _localCaret.RunIndex, _localCaret.CharOffset - 1, false);
                    _ = view.ExecuteAsync(new DeleteTextCommand(sessionId, version, target, 1, " "));
                    _localCaret = ClampCaret(target);
                    view.SetCaret(Selection.Collapsed(_localCaret));
                }
                else if (_localCaret.ParagraphIndex > 0)
                {
                    _ = view.ExecuteAsync(new MergeParagraphCommand(sessionId, version, _localCaret.ParagraphIndex));
                    int prevPara = _localCaret.ParagraphIndex - 1;
                    int lastRun = Math.Max(0, GetRunCount(prevPara) - 1);
                    _localCaret = new CaretPosition(prevPara, lastRun, GetRunLength(prevPara, lastRun), false);
                    view.SetCaret(Selection.Collapsed(_localCaret));
                }
                e.Handled = true;
                break;
            case Key.Delete:
                var delTarget = new CaretPosition(_localCaret.ParagraphIndex, _localCaret.RunIndex, _localCaret.CharOffset, false);
                _ = view.ExecuteAsync(new DeleteTextCommand(sessionId, version, delTarget, 1, " "));
                e.Handled = true;
                break;
            case Key.Enter:
                _ = view.ExecuteAsync(new SplitParagraphCommand(sessionId, version, _localCaret));
                _localCaret = new CaretPosition(_localCaret.ParagraphIndex + 1, 0, 0, false);
                view.SetCaret(Selection.Collapsed(_localCaret));
                e.Handled = true;
                break;
            case Key.Left:
                if (_localCaret.CharOffset > 0)
                {
                    _localCaret = _localCaret with { CharOffset = _localCaret.CharOffset - 1 };
                }
                else if (_localCaret.RunIndex > 0)
                {
                    int prevRun = _localCaret.RunIndex - 1;
                    _localCaret = new CaretPosition(_localCaret.ParagraphIndex, prevRun, GetRunLength(_localCaret.ParagraphIndex, prevRun), false);
                }
                else if (_localCaret.ParagraphIndex > 0)
                {
                    int prevPara = _localCaret.ParagraphIndex - 1;
                    int lastRun = Math.Max(0, GetRunCount(prevPara) - 1);
                    _localCaret = new CaretPosition(prevPara, lastRun, GetRunLength(prevPara, lastRun), false);
                }
                view?.SetCaret(Selection.Collapsed(_localCaret));
                ResetCaretBlink();
                e.Handled = true;
                break;
            case Key.Right:
                int currentLen = GetRunLength(_localCaret.ParagraphIndex, _localCaret.RunIndex);
                if (_localCaret.CharOffset < currentLen)
                {
                    _localCaret = _localCaret with { CharOffset = _localCaret.CharOffset + 1 };
                }
                else
                {
                    int nextRun = _localCaret.RunIndex + 1;
                    if (GetRunCount(_localCaret.ParagraphIndex) > nextRun)
                    {
                        _localCaret = new CaretPosition(_localCaret.ParagraphIndex, nextRun, 0, false);
                    }
                    else
                    {
                        if (_localCaret.ParagraphIndex < GetParagraphCount() - 1)
                            _localCaret = new CaretPosition(_localCaret.ParagraphIndex + 1, 0, 0, false);
                    }
                }
                view?.SetCaret(Selection.Collapsed(_localCaret));
                ResetCaretBlink();
                e.Handled = true;
                break;
            case Key.Up:
                if (_localCaret.ParagraphIndex > 0)
                {
                    _localCaret = ClampCaret(new CaretPosition(_localCaret.ParagraphIndex - 1, 0, _localCaret.CharOffset, false));
                    view.SetCaret(Selection.Collapsed(_localCaret));
                    ResetCaretBlink();
                    InvalidateVisual();
                }
                e.Handled = true;
                break;
            case Key.Down:
                int paraCount = GetParagraphCount();
                if (_localCaret.ParagraphIndex < paraCount - 1)
                {
                    _localCaret = ClampCaret(new CaretPosition(_localCaret.ParagraphIndex + 1, 0, _localCaret.CharOffset, false));
                    view.SetCaret(Selection.Collapsed(_localCaret));
                    ResetCaretBlink();
                    InvalidateVisual();
                }
                e.Handled = true;
                break;
            case Key.Z:
                if (isCmd)
                {
                    if (isShift)
                        _ = view.ExecuteAsync(new RedoCommand());
                    else
                        _ = view.ExecuteAsync(new UndoCommand());
                    e.Handled = true;
                }
                break;
            case Key.Y:
                if (isCmd)
                {
                    _ = view.ExecuteAsync(new RedoCommand());
                    e.Handled = true;
                }
                break;
            case Key.B:
                if (isCmd)
                {
                    _ = view.ExecuteAsync(new SetCharacterStyleCommand(sessionId, version, _localCaret, _localCaret, new CharacterStyleDef { Bold = true }));
                    e.Handled = true;
                }
                break;
            case Key.I:
                if (isCmd)
                {
                    _ = view.ExecuteAsync(new SetCharacterStyleCommand(sessionId, version, _localCaret, _localCaret, new CharacterStyleDef { Italic = true }));
                    e.Handled = true;
                }
                break;
            default:
                if (e.KeySymbol != null && e.KeySymbol.Length == 1 && !char.IsControl(e.KeySymbol[0]))
                {
                    char c = e.KeySymbol[0];

                    _ = view.ExecuteAsync(new InsertTextCommand(sessionId, version, _localCaret, c.ToString()));
                    _localCaret = ClampCaret(_localCaret with { CharOffset = _localCaret.CharOffset + 1 });
                    view.SetCaret(Selection.Collapsed(_localCaret));
                    e.Handled = true;
                }
                break;
        }

        if (e.Handled)
        {
            ResetCaretBlink();
            InvalidateVisual();
        }
    }

    private int GetRunLength(int paraIndex, int runIndex)
        => DocumentView?.GetRunLength(paraIndex, runIndex) ?? 0;

    private int GetRunCount(int paraIndex)
        => DocumentView?.GetRunCount(paraIndex) ?? 0;

    private int GetParagraphCount()
        => DocumentView?.ParagraphCount ?? 0;

    private CaretPosition ClampCaret(CaretPosition caret)
    {
        var view = DocumentView;
        if (view == null) return caret;
        
        int maxOffset = GetRunLength(caret.ParagraphIndex, caret.RunIndex);
        int clamped = Math.Clamp(caret.CharOffset, 0, maxOffset);
        return caret with { CharOffset = clamped };
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
