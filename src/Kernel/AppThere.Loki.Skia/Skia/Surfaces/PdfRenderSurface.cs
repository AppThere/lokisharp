// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Implementation
// PURPOSE: PDF-page render surface backed by SKDocument. The Canvas property is valid
//          between BeginPage and EndPage. Flush() ends the current page and begins a
//          new one. Close() ends the last page and seals the PDF stream. Both are
//          idempotent after the first call. Produced by HeadlessSurfaceFactory.
// DEPENDS: IRenderSurface, PdfMetadata, SKDocument, SKCanvas, SizeF
// USED BY: HeadlessSurfaceFactory, PdfRenderer
// PHASE:   1
// ADR:     ADR-002

using AppThere.Loki.Kernel.Geometry;
using SkiaSharp;

namespace AppThere.Loki.Skia.Surfaces;

public sealed class PdfRenderSurface : IRenderSurface
{
    private readonly SKDocument _document;
    private          SKCanvas?  _canvas;
    private          bool       _closed;
    private          bool       _disposed;

    public int WidthPx  { get; }
    public int HeightPx { get; }

    /// <summary>
    /// The Skia canvas for the current PDF page. Valid until <see cref="Flush"/> or
    /// <see cref="Close"/> is called. The document owns this canvas — do not dispose it.
    /// </summary>
    public SKCanvas Canvas => _canvas
        ?? throw new InvalidOperationException("No active PDF page (document was closed).");

    public PdfRenderSurface(Stream output, SizeF pageSizeInPoints, PdfMetadata meta)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(meta);

        WidthPx  = (int)pageSizeInPoints.Width;
        HeightPx = (int)pageSizeInPoints.Height;

        var pdfMeta = new SKDocumentPdfMetadata(rasterDpi: 72f)
        {
            Title   = meta.Title,
            Author  = meta.Author,
            Creator = meta.Creator,
        };

        _document = SKDocument.CreatePdf(output, pdfMeta)
            ?? throw new InvalidOperationException("SKDocument.CreatePdf returned null.");

        _canvas = _document.BeginPage(pageSizeInPoints.Width, pageSizeInPoints.Height);
    }

    /// <summary>
    /// Ends the current page and begins a new one with the same dimensions.
    /// No-op once <see cref="Close"/> has been called.
    /// </summary>
    public void Flush()
    {
        if (_closed) return;
        _document.EndPage();
        _canvas = _document.BeginPage(WidthPx, HeightPx);
    }

    /// <summary>
    /// Ends the last page and seals the PDF stream. Idempotent.
    /// </summary>
    public void Close()
    {
        if (_closed) return;
        _closed = true;
        _document.EndPage();
        _document.Close();
        _canvas = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (!_closed) Close();
        _document.Dispose();
    }
}
