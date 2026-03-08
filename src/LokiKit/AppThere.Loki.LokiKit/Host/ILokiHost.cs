// LAYER:   AppThere.Loki.LokiKit — Hourglass Waist
// KIND:    Interface
// PURPOSE: Process-level entry point. Creates and tracks open documents.
//          One ILokiHost per process. Owns the root DI container.
//          Constructed via LokiHostBuilder — never directly.
// DEPENDS: ILokiDocument, ILokiView, OpenOptions, DocumentKind
// USED BY: lokiprint CLI, Avalonia app host (Phase 4+), tests
// PHASE:   2
// ADR:     ADR-005, ADR-006

using AppThere.Loki.LokiKit.Document;
using AppThere.Loki.LokiKit.View;

namespace AppThere.Loki.LokiKit.Host;

public interface ILokiHost : IAsyncDisposable
{
    /// <summary>
    /// Opens a document from a stream. Detects format from content unless
    /// OpenOptions.FormatHint is supplied. Creates a document-scoped DI
    /// scope and returns the initialised document handle.
    /// Throws LokiOpenException if the stream cannot be parsed.
    /// </summary>
    Task<ILokiDocument> OpenAsync(
        Stream source,
        OpenOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a new empty document of the given kind.
    /// </summary>
    Task<ILokiDocument> NewAsync(
        DocumentKind kind,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a rendering view onto an open document.
    /// The view is owned by the caller — dispose when the panel closes.
    /// The document must have been opened by this host instance.
    /// </summary>
    ILokiView CreateView(ILokiDocument document);

    /// <summary>
    /// All documents currently open on this host. Snapshot — not live.
    /// </summary>
    IReadOnlyList<ILokiDocument> OpenDocuments { get; }
}
