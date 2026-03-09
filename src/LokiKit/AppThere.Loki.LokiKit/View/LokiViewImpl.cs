// LAYER:   AppThere.Loki.LokiKit — Hourglass Waist
// KIND:    Implementation
// PURPOSE: ILokiView concrete implementation. Lightweight viewport onto one
//          ILokiDocument. Holds zoom, scroll position, and active part index.
//          Fires TileInvalidated when ActivePart or Zoom changes, or when the
//          document fires Changed. Does NOT cache tiles.
//          Implements RenderToPdfAsync so TestRenderCommand/UI can export PDF
//          without casting to a renderer type.
// DEPENDS: ILokiView, ILokiDocument, LokiDocumentImpl, ITileRenderer,
//          TileRequest, PdfMetadata, ILokiLogger, TileInvalidatedEventArgs
// USED BY: LokiHostImpl.CreateView, lokiprint CLI, rendering tests
// PHASE:   2

using AppThere.Loki.Kernel.Geometry;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.LokiKit.Commands;
using AppThere.Loki.LokiKit.Document;
using AppThere.Loki.LokiKit.Engine;
using AppThere.Loki.LokiKit.Events;
using AppThere.Loki.LokiKit.Host;
using AppThere.Loki.Skia.Rendering;
using AppThere.Loki.Skia.Scene;
using AppThere.Loki.Skia.Surfaces;
using SkiaSharp;

namespace AppThere.Loki.LokiKit.View;

public sealed class LokiViewImpl : ILokiView
{
    private readonly ITileRenderer _renderer;
    private readonly ILokiLogger   _logger;
    private readonly LokiHostOptions _options;
    private int    _activePart   = 0;
    private float  _zoom         = 1.0f;
    private PointF _scrollOffset = new(0f, 0f);

    private EventHandler<EngineLayoutInvalidatedEventArgs>? _pendingLayoutInvalidated;

    public ILokiDocument Document { get; private set; } = null!;

    public int PartCount => Document is LokiDocumentImpl impl ? impl.Engine.PartCount : 0;

    public event EventHandler<TileInvalidatedEventArgs>? TileInvalidated;

    public event EventHandler<EngineLayoutInvalidatedEventArgs>? LayoutInvalidated
    {
        add
        {
            _pendingLayoutInvalidated += value;
            if (Document is LokiDocumentImpl impl) impl.Engine.LayoutInvalidated += value;
        }
        remove
        {
            _pendingLayoutInvalidated -= value;
            if (Document is LokiDocumentImpl impl) impl.Engine.LayoutInvalidated -= value;
        }
    }

    public LokiViewImpl(ILokiDocument document, ITileRenderer renderer, ILokiLogger logger, LokiHostOptions options)
    {
        _renderer  = renderer;
        _logger    = logger;
        _options   = options;

        SetDocument(document);
    }

    private void SetDocument(ILokiDocument document)
    {
        if (Document == document) return;

        // Unsubscribe from old
        if (Document != null)
        {
            Document.Changed -= OnDocumentChanged;
            if (Document is LokiDocumentImpl oldImpl && _pendingLayoutInvalidated != null)
            {
                oldImpl.Engine.LayoutInvalidated -= _pendingLayoutInvalidated;
            }
        }

        Document = document;

        // Subscribe to new
        if (Document != null)
        {
            Document.Changed += OnDocumentChanged;
            if (Document is LokiDocumentImpl newImpl && _pendingLayoutInvalidated != null)
            {
                newImpl.Engine.LayoutInvalidated += _pendingLayoutInvalidated;
            }
        }
    }

    // ── View state ────────────────────────────────────────────────────────────

    public int ActivePart
    {
        get => _activePart;
        set
        {
            _activePart = value;
            FireTileInvalidated();
        }
    }

    public float Zoom
    {
        get => _zoom;
        set
        {
            if (value <= 0f) throw new ArgumentOutOfRangeException(nameof(value), "Zoom must be > 0.");
            _zoom = value;
            FireTileInvalidated();
        }
    }

    public PointF ScrollOffset
    {
        get => _scrollOffset;
        set => _scrollOffset = value;   // Intentionally does NOT fire TileInvalidated.
    }

    // ── Tile rendering ────────────────────────────────────────────────────────

    public Task<SKBitmap> RenderTileAsync(TileRequest request, CancellationToken ct = default)
    {
        var scene = GetPaintScene(request.PartIndex);
        return _renderer.RenderTileAsync(scene, request, ct);
    }

    /// <summary>
    /// Renders all parts of the document to a PDF stream.
    /// Convenience wrapper over ITileRenderer.RenderToPdfAsync.
    /// </summary>
    public Task RenderToPdfAsync(Stream output, PdfMetadata meta,
                                 CancellationToken ct = default)
    {
        var scenes = new PaintScene[Document.PartCount];
        for (var i = 0; i < Document.PartCount; i++)
            scenes[i] = GetPaintScene(i);
        return _renderer.RenderToPdfAsync(scenes, output, meta, ct);
    }

    // ── Layout queries ────────────────────────────────────────────────────────

    public int ParagraphCount
    {
        get
        {
            if (Document is LokiDocumentImpl impl)
            {
                try
                {
                    dynamic engine = impl.Engine;
                    return engine.GetSnapshot().Body.Count;
                }
                catch { }
            }
            return 0;
        }
    }

    public int GetRunCount(int paragraphIndex)
    {
        if (Document is LokiDocumentImpl impl)
        {
            try
            {
                dynamic engine = impl.Engine;
                var snapshot = engine.GetSnapshot();
                if (paragraphIndex >= 0 && paragraphIndex < snapshot.Body.Count)
                {
                    var para = snapshot.Body[paragraphIndex];
                    return para.Inlines.Count;
                }
            }
            catch { }
        }
        return 0;
    }

    public int GetRunLength(int paragraphIndex, int runIndex)
    {
        if (Document is LokiDocumentImpl impl)
        {
            try
            {
                dynamic engine = impl.Engine;
                var snapshot = engine.GetSnapshot();
                if (paragraphIndex >= 0 && paragraphIndex < snapshot.Body.Count)
                {
                    var para = snapshot.Body[paragraphIndex];
                    if (runIndex >= 0 && runIndex < para.Inlines.Count)
                    {
                        var run = para.Inlines[runIndex];
                        return run.Text?.Length ?? 0;
                    }
                }
            }
            catch { }
        }
        return 0;
    }

    public SizeF GetPartSize(int partIndex) => Document.GetPart(partIndex).SizeInPoints;

    // ── Editing ───────────────────────────────────────────────────────────────

    public CaretPosition? HitTest(float xPts, float yPts, int partIndex)
    {
        if (Document is not LokiDocumentImpl impl) return null;
        return impl.Engine.HitTest(partIndex, xPts, yPts);
    }

    public void SetCaret(Selection selection)
    {
        if (Document is not LokiDocumentImpl impl) return;
        impl.Engine.SetCaret(_options.LocalSessionId, selection);
    }

    public Task<bool> ExecuteAsync(ILokiCommand command, CancellationToken ct = default)
    {
        if (Document is not LokiDocumentImpl impl) return Task.FromResult(false);
        return impl.Engine.ExecuteAsync(command, ct);
    }

    public IReadOnlyList<CaretEntry> GetCarets()
    {
        if (Document is not LokiDocumentImpl impl) return Array.Empty<CaretEntry>();
        return impl.Engine.GetCarets();
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    public AppThere.Loki.Skia.Scene.PaintScene GetPaintScene(int partIndex)
    {
        if (Document is not LokiDocumentImpl impl)
            throw new InvalidOperationException("Document is not a LokiDocumentImpl.");
        return impl.Engine.GetPaintScene(partIndex);
    }

    private void FireTileInvalidated() =>
        TileInvalidated?.Invoke(this, TileInvalidatedEventArgs.All);

    private void OnDocumentChanged(object? sender, Events.DocumentChangedEventArgs e)
    {
        _logger.Debug("View: document changed ({0}) — invalidating all tiles.", e.ChangeKind);
        FireTileInvalidated();
    }

    // ── Dispose ───────────────────────────────────────────────────────────────

    public void Dispose()
    {
        Document.Changed -= OnDocumentChanged;
    }
}
