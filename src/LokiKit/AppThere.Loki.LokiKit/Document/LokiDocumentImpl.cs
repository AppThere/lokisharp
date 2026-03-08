// LAYER:   AppThere.Loki.LokiKit — Hourglass Waist
// KIND:    Implementation
// PURPOSE: ILokiDocument concrete implementation. Wraps an ILokiEngine instance
//          that lives in a document-scoped DI child scope.
//          Delegates Kind/PartCount/GetPart/IsModified/ExecuteAsync/CanExecute to
//          the engine. Fires Changed when the engine fires LayoutInvalidated.
//          SaveAsync is not implemented in Phase 2.
//          Exposes internal Engine property so LokiViewImpl can reach GetPaintScene.
// DEPENDS: ILokiDocument, ILokiEngine, IServiceScope, ILokiLogger,
//          CommandDispatchException, DocumentChangedEventArgs
// USED BY: LokiHostImpl, LokiViewImpl
// PHASE:   2

using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.LokiKit.Commands;
using AppThere.Loki.LokiKit.Engine;
using AppThere.Loki.LokiKit.Errors;
using AppThere.Loki.LokiKit.Events;
using AppThere.Loki.LokiKit.Host;
using Microsoft.Extensions.DependencyInjection;

namespace AppThere.Loki.LokiKit.Document;

public sealed class LokiDocumentImpl : ILokiDocument
{
    private readonly IServiceScope    _scope;
    private readonly ILokiLogger      _logger;
    private readonly Action<string>?  _onDisposed;

    // Exposed internally so LokiViewImpl can call Engine.GetPaintScene.
    internal ILokiEngine Engine { get; }

    public string      DocumentId { get; } = Guid.NewGuid().ToString("N");
    public DocumentKind Kind       => Engine.PartCount > 0
                                       ? DocumentKind.Writer   // Phase 2: always Writer
                                       : DocumentKind.Writer;
    public string?     SourcePath  => null;                    // Phase 2: no file I/O
    public bool        IsModified  => Engine.IsModified;
    public int         PartCount   => Engine.PartCount;

    public event EventHandler<DocumentChangedEventArgs>? Changed;

    public LokiDocumentImpl(ILokiEngine engine, IServiceScope scope, ILokiLogger logger,
                             Action<string>? onDisposed = null)
    {
        Engine      = engine;
        _scope      = scope;
        _logger     = logger;
        _onDisposed = onDisposed;

        Engine.LayoutInvalidated += OnLayoutInvalidated;
    }

    // ── Parts ─────────────────────────────────────────────────────────────────

    public ILokiPart GetPart(int partIndex) => Engine.GetPart(partIndex);

    // ── Commands ──────────────────────────────────────────────────────────────

    public async Task ExecuteAsync(ILokiCommand command, CancellationToken ct = default)
    {
        var handled = await Engine.ExecuteAsync(command, ct).ConfigureAwait(false);
        if (!handled)
            throw new CommandDispatchException(command.GetType());
    }

    public bool CanExecute(ILokiCommand command) => Engine.CanExecute(command);

    // ── Persistence ───────────────────────────────────────────────────────────

    public Task SaveAsync(Stream output, SaveFormat format = SaveFormat.Native,
                          CancellationToken ct = default) =>
        throw new NotSupportedException("Save not implemented in Phase 2.");

    // ── Change propagation ────────────────────────────────────────────────────

    private void OnLayoutInvalidated(object? sender, EngineLayoutInvalidatedEventArgs e)
    {
        _logger.Debug("Document {0}: layout invalidated for {1} part(s).",
                      DocumentId, e.AffectedParts.Count);
        Changed?.Invoke(this,
            new DocumentChangedEventArgs(DocumentChangeKind.ContentChanged, e.AffectedParts));
    }

    // ── Dispose ───────────────────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        Engine.LayoutInvalidated -= OnLayoutInvalidated;
        await Engine.DisposeAsync().ConfigureAwait(false);
        // Use DisposeAsync on scope when available — prevents DI runtime
        // warning for services (e.g. StubEngine) that only implement IAsyncDisposable.
        if (_scope is IAsyncDisposable asyncScope)
            await asyncScope.DisposeAsync().ConfigureAwait(false);
        else
            _scope.Dispose();
        _onDisposed?.Invoke(DocumentId);
    }
}
