// LAYER:   AppThere.Loki.LokiKit — Hourglass Waist
// KIND:    Interface
// PURPOSE: Handle to one open document. Created by ILokiHost.OpenAsync.
//          Owns the engine instance and PaintScene cache for this document.
//          Fires Changed when layout or content is invalidated.
//          Dispose closes the document and releases the document-scoped DI scope.
// DEPENDS: ILokiCommand, DocumentKind, DocumentChangedEventArgs, SaveFormat
// USED BY: ILokiHost, ILokiView, lokiprint CLI
// PHASE:   2
// ADR:     ADR-005

using AppThere.Loki.LokiKit.Commands;
using AppThere.Loki.LokiKit.Events;
using AppThere.Loki.LokiKit.Host;

namespace AppThere.Loki.LokiKit.Document;

public interface ILokiDocument : IAsyncDisposable
{
    // ── Identity ─────────────────────────────────────────────────────────────

    /// <summary>Stable identifier for this open session. Not persisted.</summary>
    string DocumentId { get; }

    /// <summary>Which engine handles this document.</summary>
    DocumentKind Kind { get; }

    /// <summary>Source path if opened from file; null for new/untitled.</summary>
    string? SourcePath { get; }

    /// <summary>True if the document has unsaved changes.</summary>
    bool IsModified { get; }

    // ── Parts (pages / sheets / slides) ──────────────────────────────────────

    /// <summary>Number of parts (pages for Writer, sheets for Calc, etc.).</summary>
    int PartCount { get; }

    /// <summary>Returns metadata for the part at the given zero-based index.</summary>
    ILokiPart GetPart(int partIndex);

    // ── Commands ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Executes a command against the document engine.
    /// Fires Changed if the command modifies layout or content.
    /// Throws CommandDispatchException if no handler is registered.
    /// Throws InvalidOperationException if the document is read-only and
    /// the command is mutating.
    /// Phase 2: all commands are no-ops (StubDocument).
    /// </summary>
    Task ExecuteAsync(ILokiCommand command, CancellationToken ct = default);

    /// <summary>Returns true if the command can be executed in the current state.</summary>
    bool CanExecute(ILokiCommand command);

    // ── Persistence ───────────────────────────────────────────────────────────

    /// <summary>
    /// Saves the document to the given stream in the requested format.
    /// SaveFormat.Native uses the document's source format.
    /// Throws LokiSaveException on failure.
    /// Phase 2: not implemented — throws NotSupportedException.
    /// </summary>
    Task SaveAsync(
        Stream output,
        SaveFormat format = SaveFormat.Native,
        CancellationToken ct = default);

    // ── Change notifications ──────────────────────────────────────────────────

    /// <summary>
    /// Fired when document content or layout changes.
    /// ILokiView subscribes to this and fires TileInvalidated in response.
    /// Raised on the thread that triggered the change — subscribers must
    /// marshal to the UI thread if needed.
    /// </summary>
    event EventHandler<DocumentChangedEventArgs> Changed;
}
