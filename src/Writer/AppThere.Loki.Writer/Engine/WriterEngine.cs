// LAYER:   AppThere.Loki.Writer — Engine
// KIND:    Implementation
// PURPOSE: ILokiEngine implementation for Writer documents (ODT, FODT).
//          Owns DocumentState (versioned LokiDocument), LayoutCache, and
//          ILayoutEngine. InitialiseAsync detects ZIP vs flat XML, imports
//          the document via IOdfImporter, then runs the layout pipeline.
//          An unreadable stream (Stream.Null) produces an empty document.
//          Commands are not handled in Phase 3 (returns false for all).
//          LayoutInvalidated is not raised (future Phase 4+ feature).
// DEPENDS: ILokiEngine, DocumentState, LayoutCache, ILayoutEngine, IOdfImporter,
//          LokiDocument, WriterPart, IFontManager, ILokiLogger
// USED BY: LokiHostBuilderExtensions.UseWriterEngine (Scoped DI)
// PHASE:   3
// ADR:     ADR-007, ADR-008

using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.LokiKit.Commands;
using AppThere.Loki.LokiKit.Document;
using AppThere.Loki.LokiKit.Engine;
using AppThere.Loki.LokiKit.Events;
using AppThere.Loki.LokiKit.Host;
using AppThere.Loki.Skia.Fonts;
using AppThere.Loki.Skia.Scene;
using AppThere.Loki.Writer.Layout;
using AppThere.Loki.Writer.Model;

namespace AppThere.Loki.Writer.Engine;

public sealed class WriterEngine : ILokiEngine
{
    private readonly IFontManager  _fontManager;
    private readonly ILokiLogger   _logger;
    private readonly IOdfImporter  _odfImporter;
    private readonly ILayoutEngine _layoutEngine;

    private DocumentState            _state  = new(LokiDocument.Empty);
    private readonly LayoutCache     _cache  = new();
    private IReadOnlyList<PaintScene> _scenes = [];

    public WriterEngine(
        IFontManager  fontManager,
        ILokiLogger   logger,
        IOdfImporter  odfImporter,
        ILayoutEngine layoutEngine)
    {
        _fontManager  = fontManager;
        _logger       = logger;
        _odfImporter  = odfImporter;
        _layoutEngine = layoutEngine;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public async Task InitialiseAsync(
        Stream source, OpenOptions options, CancellationToken ct)
    {
        LokiDocument doc;

        if (!source.CanRead)
        {
            doc = LokiDocument.Empty;
        }
        else
        {
            // Buffer stream to allow seeking for format detection
            using var ms = new MemoryStream();
            await source.CopyToAsync(ms, ct).ConfigureAwait(false);
            ms.Seek(0, SeekOrigin.Begin);

            // Detect format: ZIP (ODT) starts with PK 0x50 0x4B
            var b0 = ms.ReadByte();
            var b1 = ms.ReadByte();
            ms.Seek(0, SeekOrigin.Begin);
            var isFlat = !(b0 == 0x50 && b1 == 0x4B);

            try
            {
                doc = await _odfImporter.ImportAsync(
                    ms, isFlat, _fontManager, _logger, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Warn($"WriterEngine: import failed ({ex.Message}); using empty document.");
                doc = LokiDocument.Empty;
            }
        }

        _state  = new DocumentState(doc);
        _cache.Clear();
        _scenes = _layoutEngine.Layout(
            _state.Snapshot with { LayoutVersion = _state.Version }, _cache);
    }

    public Task InitialiseNewAsync(DocumentKind kind, CancellationToken ct)
    {
        _state  = new DocumentState(LokiDocument.Empty);
        _cache.Clear();
        _scenes = _layoutEngine.Layout(
            _state.Snapshot with { LayoutVersion = _state.Version }, _cache);
        return Task.CompletedTask;
    }

    // ── Document model ────────────────────────────────────────────────────────

    public int PartCount => Math.Max(1, _scenes.Count);

    public ILokiPart GetPart(int partIndex) =>
        new WriterPart(partIndex, _state.Snapshot.DefaultPageStyle);

    public bool IsModified => _state.IsModified;

    // ── Rendering ─────────────────────────────────────────────────────────────

    public PaintScene GetPaintScene(int partIndex)
    {
        if (partIndex < 0 || partIndex >= _scenes.Count)
            throw new ArgumentOutOfRangeException(nameof(partIndex), partIndex,
                $"WriterEngine has {_scenes.Count} part(s).");
        return _scenes[partIndex];
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public Task<bool> ExecuteAsync(ILokiCommand command, CancellationToken ct) =>
        Task.FromResult(false);

    public bool CanExecute(ILokiCommand command) => false;

    // ── Persistence ───────────────────────────────────────────────────────────

    public Task SaveAsync(Stream output, SaveFormat format, CancellationToken ct) =>
        throw new NotSupportedException("WriterEngine.SaveAsync is Phase 4+.");

    // ── Change notifications ──────────────────────────────────────────────────

#pragma warning disable CS0067
    public event EventHandler<EngineLayoutInvalidatedEventArgs>? LayoutInvalidated;
#pragma warning restore CS0067

    // ── Dispose ───────────────────────────────────────────────────────────────

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
