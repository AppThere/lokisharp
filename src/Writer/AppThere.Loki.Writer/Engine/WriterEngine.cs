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
using AppThere.Loki.Writer.Editing;
using AppThere.Loki.Writer.Layout;
using AppThere.Loki.Writer.Model;
using AppThere.Loki.Skia.Scene.Nodes;

namespace AppThere.Loki.Writer.Engine;

public sealed class WriterEngine : ILokiEngine
{
    private readonly IFontManager    _fontManager;
    private readonly ILokiLogger     _logger;
    private readonly IOdfImporter    _odfImporter;
    private readonly ILayoutEngine   _layoutEngine;
    private readonly LokiHostOptions _options;

    private DocumentState             _state  = new(LokiDocument.Empty);
    private readonly LayoutCache      _cache  = new();
    private IReadOnlyList<PaintScene> _scenes = [];

    // Phase 5 Editing State
    private readonly CaretRegistry _caretRegistry = new();
    private readonly CommandHistory _history;
    private readonly DocumentMutator _mutator;
    private PendingInputBuffer? _inputBuffer;

    public WriterEngine(
        IFontManager    fontManager,
        ILokiLogger     logger,
        IOdfImporter    odfImporter,
        ILayoutEngine   layoutEngine,
        LokiHostOptions options)
    {
        _fontManager  = fontManager;
        _logger       = logger;
        _odfImporter  = odfImporter;
        _layoutEngine = layoutEngine;
        _options      = options;
        
        _history      = new CommandHistory(_options.MaxUndoDepth);
        _mutator      = new DocumentMutator(_logger);
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
        _inputBuffer = new PendingInputBuffer(_history, _mutator, _options.LocalSessionId, _options.InputIdleCommitMs, _logger);
        _cache.Clear();
        _scenes = await Task.Run(() => _layoutEngine.Layout(
            _state.Snapshot with { LayoutVersion = _state.Version }, _cache)).ConfigureAwait(false);
    }

    public async Task InitialiseNewAsync(DocumentKind kind, CancellationToken ct)
    {
        _state  = new DocumentState(LokiDocument.Empty);
        _inputBuffer = new PendingInputBuffer(_history, _mutator, _options.LocalSessionId, _options.InputIdleCommitMs, _logger);
        _cache.Clear();
        _scenes = await Task.Run(() => _layoutEngine.Layout(
            _state.Snapshot with { LayoutVersion = _state.Version }, _cache)).ConfigureAwait(false);
    }

    // ── Document model ────────────────────────────────────────────────────────

    public int PartCount => _scenes.Count > 0 ? _scenes.Count : 1;

    public ILokiPart GetPart(int partIndex) =>
        new WriterPart(partIndex, _state.Snapshot.DefaultPageStyle);

    public bool IsModified => _state.IsModified;

    // ── Rendering ─────────────────────────────────────────────────────────────

    public LokiDocument GetSnapshot() => 
        _inputBuffer?.HasPending == true ? _inputBuffer.PendingSnapshot : _state.Snapshot;

    public PaintScene GetPaintScene(int partIndex)
    {
        if (_scenes == null || _scenes.Count == 0)
            return PaintScene.CreateBuilder(0).Build();

        if (_inputBuffer?.HasPending == true)
        {
            var pScenes = _layoutEngine.Layout(
                _inputBuffer.PendingSnapshot with { LayoutVersion = _state.Version }, _cache);
            if (pScenes.Count == 0) return _scenes[0];
            return partIndex >= 0 && partIndex < pScenes.Count ? pScenes[partIndex] : pScenes[^1];
        }

        int clamped = Math.Clamp(partIndex, 0, _scenes.Count - 1);
        return _scenes[clamped];
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public Task<bool> ExecuteAsync(ILokiCommand command, CancellationToken ct)
    {
        if (_inputBuffer == null) return Task.FromResult(false);

        if (command is UndoCommand)
        {
            _inputBuffer.Commit();
            var entry = _history.PopUndo();
            if (entry == null) return Task.FromResult(false);
            
            _state.Apply(entry.Value.StateBefore);
            _cache.InvalidateFrom(0);
            _scenes = _layoutEngine.Layout(_state.Snapshot with { LayoutVersion = _state.Version }, _cache);
            LayoutInvalidated?.Invoke(this, new EngineLayoutInvalidatedEventArgs(new[] { 0 }));
            return Task.FromResult(true);
        }

        if (command is RedoCommand)
        {
            _inputBuffer.Commit();
            var entry = _history.PopRedo();
            if (entry == null) return Task.FromResult(false);
            
            var newDoc = _mutator.Apply(_state.Snapshot, entry.Value.Command);
            _state.Apply(newDoc);
            _cache.InvalidateFrom(0);
            _scenes = _layoutEngine.Layout(_state.Snapshot with { LayoutVersion = _state.Version }, _cache);
            LayoutInvalidated?.Invoke(this, new EngineLayoutInvalidatedEventArgs(new[] { 0 }));
            return Task.FromResult(true);
        }

        if (command is IEditCommand editCmd)
        {
            bool shouldCommitFirst = command is not InsertTextCommand;
            if (shouldCommitFirst) _inputBuffer.Commit();

            var stateBefore = _state.Snapshot;
            var newDoc = _mutator.Apply(_state.Snapshot, editCmd);
            _state.Apply(newDoc);
            _history.Push(editCmd, stateBefore);
            
            var firstAffected = AffectedParagraphIndex(editCmd);
            _cache.InvalidateFrom(firstAffected);
            _scenes = _layoutEngine.Layout(_state.Snapshot with { LayoutVersion = _state.Version }, _cache);
            LayoutInvalidated?.Invoke(this, new EngineLayoutInvalidatedEventArgs(new[] { firstAffected }));
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    private int AffectedParagraphIndex(IEditCommand command) => command switch
    {
        InsertTextCommand c => c.At.ParagraphIndex,
        DeleteTextCommand c => c.From.ParagraphIndex,
        SplitParagraphCommand c => c.At.ParagraphIndex,
        MergeParagraphCommand c => Math.Max(0, c.ParagraphIndex - 1),
        SetCharacterStyleCommand c => c.From.ParagraphIndex,
        _ => 0
    };

    public bool CanExecute(ILokiCommand command) => false;

    // ── Persistence ───────────────────────────────────────────────────────────

    public Task SaveAsync(Stream output, SaveFormat format, CancellationToken ct) =>
        throw new NotSupportedException("WriterEngine.SaveAsync is Phase 4+.");

    // ── Editing ───────────────────────────────────────────────────────────────

    public void SetCaret(SessionId sessionId, Selection selection)
    {
        _caretRegistry.Set(sessionId, selection);
        LayoutInvalidated?.Invoke(this, new EngineLayoutInvalidatedEventArgs(new[] { selection.Focus.ParagraphIndex }));
    }

    public IReadOnlyList<CaretEntry> GetCarets() => _caretRegistry.GetAll();

    public CaretPosition? HitTest(int partIndex, float xPts, float yPts)
    {
        var scene = GetPaintScene(partIndex);
        var nodes = scene.Bands.SelectMany(b => b.Nodes).OfType<GlyphRunNode>().ToList();
        if (nodes.Count == 0) return CaretPosition.DocumentStart;

        var inBand = nodes.Where(n => yPts >= n.Bounds.Top && yPts <= n.Bounds.Bottom).ToList();
        if (inBand.Count == 0)
        {
            var nearest = nodes.OrderBy(n => Math.Abs(n.Bounds.Center.Y - yPts)).First();
            return new CaretPosition(nearest.ParagraphIndex, 0, 0, false);
        }
        
        var hit = inBand.FirstOrDefault(n => xPts >= n.Bounds.Left && xPts <= n.Bounds.Right);
        if (hit != null)
        {
            float relativeX = xPts - hit.Bounds.Left;
            float charWidth = hit.Bounds.Width / Math.Max(1, hit.Text.Length);
            int charOffset = (int)Math.Round(relativeX / charWidth);
            charOffset = Math.Clamp(charOffset, 0, hit.Text.Length);
            return new CaretPosition(hit.ParagraphIndex, hit.RunIndex, hit.RunOffset + charOffset, false);
        }
        
        return new CaretPosition(inBand.First().ParagraphIndex, 0, 0, false);
    }

    // ── Change notifications ──────────────────────────────────────────────────

#pragma warning disable CS0067
    public event EventHandler<EngineLayoutInvalidatedEventArgs>? LayoutInvalidated;
#pragma warning restore CS0067

    // ── Dispose ───────────────────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        if (_inputBuffer != null)
        {
            await _inputBuffer.DisposeAsync();
        }
        _caretRegistry.Clear();
        _history.Clear();
    }
}
