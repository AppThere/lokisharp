// LAYER:   AppThere.Loki.LokiKit — Hourglass Waist
// KIND:    Implementation
// PURPOSE: ILokiHost concrete implementation. Process-level entry point created
//          by LokiHostBuilder.Build(). Owns the root DI container.
//          Opens documents via OpenAsync (creates child scopes, resolves engines).
//          Creates views via CreateView. Tracks all open documents.
//          DisposeAsync shuts down all documents then the root provider.
// DEPENDS: ILokiHost, ILokiDocument, ILokiView, LokiDocumentImpl, LokiViewImpl,
//          ILokiEngine, ITileRenderer, ILokiLogger, LokiOpenException
// USED BY: LokiHostBuilder.Build(), lokiprint CLI, LokiKit integration tests
// PHASE:   2
// ADR:     ADR-005, ADR-006

using System.Collections.Concurrent;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.LokiKit.Document;
using AppThere.Loki.LokiKit.Engine;
using AppThere.Loki.LokiKit.Errors;
using AppThere.Loki.LokiKit.Host;
using AppThere.Loki.LokiKit.View;
using AppThere.Loki.Skia.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace AppThere.Loki.LokiKit.Host;

public sealed class LokiHostImpl : ILokiHost
{
    private readonly IServiceProvider _rootProvider;
    private readonly ILokiLogger      _logger;
    private readonly ConcurrentDictionary<string, ILokiDocument> _documents = new();

    public IReadOnlyList<ILokiDocument> OpenDocuments =>
        _documents.Values.ToList().AsReadOnly();

    public LokiHostImpl(IServiceProvider rootProvider, ILokiLogger logger)
    {
        _rootProvider = rootProvider;
        _logger       = logger;
    }

    // ── OpenAsync ─────────────────────────────────────────────────────────────

    public async Task<ILokiDocument> OpenAsync(
        Stream source, OpenOptions options, CancellationToken ct = default)
    {
        var scope = _rootProvider.CreateScope();
        try
        {
            var engine = scope.ServiceProvider.GetRequiredService<ILokiEngine>();
            await engine.InitialiseAsync(source, options, ct).ConfigureAwait(false);

            var doc = new LokiDocumentImpl(engine, scope, _logger, RemoveDocument);
            _documents[doc.DocumentId] = doc;
            _logger.Info("Opened document {0}.", doc.DocumentId);
            return doc;
        }
        catch (Exception ex) when (ex is not LokiOpenException)
        {
            scope.Dispose();
            throw new LokiOpenException("Failed to initialise document engine.", ex);
        }
    }

    // ── NewAsync ──────────────────────────────────────────────────────────────

    public async Task<ILokiDocument> NewAsync(
        DocumentKind kind, CancellationToken ct = default)
    {
        var scope = _rootProvider.CreateScope();
        try
        {
            var engine = scope.ServiceProvider.GetRequiredService<ILokiEngine>();
            await engine.InitialiseNewAsync(kind, ct).ConfigureAwait(false);

            var doc = new LokiDocumentImpl(engine, scope, _logger, RemoveDocument);
            _documents[doc.DocumentId] = doc;
            _logger.Info("Created new document {0} (kind={1}).", doc.DocumentId, kind);
            return doc;
        }
        catch (Exception ex) when (ex is not LokiOpenException)
        {
            scope.Dispose();
            throw new LokiOpenException("Failed to create new document.", ex);
        }
    }

    // ── CreateView ────────────────────────────────────────────────────────────

    public ILokiView CreateView(ILokiDocument document)
    {
        var renderer = _rootProvider.GetRequiredService<ITileRenderer>();
        var options  = _rootProvider.GetRequiredService<LokiHostOptions>();
        return new LokiViewImpl(document, renderer, _logger, options);
    }

    // ── DisposeAsync ──────────────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        // Remove from dictionary while collecting to avoid concurrent iteration.
        var docs = _documents.Values.ToList();
        _documents.Clear();

        await Task.WhenAll(docs.Select(async d =>
        {
            try   { await d.DisposeAsync().ConfigureAwait(false); }
            catch (Exception ex)
            {
                _logger.Error("Error disposing document: {0}", ex);
            }
        })).ConfigureAwait(false);

        if (_rootProvider is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        else if (_rootProvider is IDisposable disposable)
            disposable.Dispose();
    }

    // ── Document removal (called by LokiDocumentImpl on dispose) ─────────────

    internal void RemoveDocument(string documentId) =>
        _documents.TryRemove(documentId, out _);
}
