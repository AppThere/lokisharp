// LAYER:   AppThere.Loki.LokiKit — Hourglass Waist
// KIND:    Interface
// PURPOSE: Contract that all document engines must implement.
//          One engine instance per open document (document-scoped DI lifetime).
//          The engine owns: document model, layout computation, PaintScene
//          construction, and command handling.
//          Phase 2: StubEngine returns a fixed Phase1TestScene.
//          Phase 3: WriterEngine provides real ODT/DOCX loading and layout.
// DEPENDS: ILokiCommand, ILokiPart, PaintScene
// USED BY: ILokiDocument (implementation delegates to engine)
// PHASE:   2
// ADR:     ADR-005

using AppThere.Loki.LokiKit.Commands;
using AppThere.Loki.LokiKit.Document;
using AppThere.Loki.LokiKit.Host;
using AppThere.Loki.Skia.Scene;
using AppThere.Loki.LokiKit.Events;

namespace AppThere.Loki.LokiKit.Engine;

public interface ILokiEngine : IAsyncDisposable
{
    // ── Lifecycle ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Initialise the engine from a stream.
    /// For StubEngine: ignores the stream and loads Phase1TestScene.
    /// For real engines: parses the document format.
    /// </summary>
    Task InitialiseAsync(Stream source, OpenOptions options, CancellationToken ct);

    /// <summary>
    /// Initialise the engine with a new empty document.
    /// </summary>
    Task InitialiseNewAsync(DocumentKind kind, CancellationToken ct);

    // ── Document model ────────────────────────────────────────────────────────

    /// <summary>Number of parts.</summary>
    int PartCount { get; }

    /// <summary>Returns part metadata at the given zero-based index.</summary>
    ILokiPart GetPart(int partIndex);

    /// <summary>True if any unsaved changes exist.</summary>
    bool IsModified { get; }

    // ── Rendering ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the current PaintScene for the given part.
    /// Called by the document when a view requests a tile.
    /// The returned PaintScene is immutable — the engine creates a new one
    /// when layout changes and fires LayoutInvalidated.
    /// </summary>
    PaintScene GetPaintScene(int partIndex);

    // ── Commands ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Dispatch a command to the engine.
    /// Returns true if the command was handled, false if unsupported.
    /// Phase 2 (StubEngine): handles PingCommand (returns true), returns
    /// false for all others.
    /// </summary>
    Task<bool> ExecuteAsync(ILokiCommand command, CancellationToken ct);

    /// <summary>Returns true if this engine can execute the command.</summary>
    bool CanExecute(ILokiCommand command);

    // ── Persistence ───────────────────────────────────────────────────────────

    /// <summary>
    /// Serialise the document model to the given stream.
    /// Phase 2: throws NotSupportedException.
    /// </summary>
    Task SaveAsync(Stream output, SaveFormat format, CancellationToken ct);

    // ── Change notifications ──────────────────────────────────────────────────

    /// <summary>
    /// Fired when layout for one or more parts has been recomputed and callers
    /// should request a fresh PaintScene via GetPaintScene.
    /// Payload: set of invalidated part indices.
    /// </summary>
    event EventHandler<EngineLayoutInvalidatedEventArgs> LayoutInvalidated;
}
