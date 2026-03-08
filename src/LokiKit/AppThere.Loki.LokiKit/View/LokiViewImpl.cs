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
using AppThere.Loki.LokiKit.Document;
using AppThere.Loki.LokiKit.Events;
using AppThere.Loki.Skia.Rendering;
using AppThere.Loki.Skia.Scene;
using AppThere.Loki.Skia.Surfaces;
using SkiaSharp;

namespace AppThere.Loki.LokiKit.View;

public sealed class LokiViewImpl : ILokiView
{
    private readonly ITileRenderer _renderer;
    private readonly ILokiLogger   _logger;
    private int    _activePart   = 0;
    private float  _zoom         = 1.0f;
    private PointF _scrollOffset = new(0f, 0f);

    public ILokiDocument Document { get; }

    public event EventHandler<TileInvalidatedEventArgs>? TileInvalidated;

    public LokiViewImpl(ILokiDocument document, ITileRenderer renderer, ILokiLogger logger)
    {
        Document   = document;
        _renderer  = renderer;
        _logger    = logger;

        Document.Changed += OnDocumentChanged;
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
        var scene = GetEngineScene(_activePart);
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
            scenes[i] = GetEngineScene(i);
        return _renderer.RenderToPdfAsync(scenes, output, meta, ct);
    }

    // ── Layout queries ────────────────────────────────────────────────────────

    public SizeF GetPartSize(int partIndex) => Document.GetPart(partIndex).SizeInPoints;

    // ── Internal helpers ──────────────────────────────────────────────────────

    private PaintScene GetEngineScene(int partIndex)
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
