// LAYER:   AppThere.Loki.LokiKit — Hourglass Waist
// KIND:    Implementation
// PURPOSE: ILokiEngine implementation for Phase 2. Returns Phase1TestScene for
//          all parts without any real document parsing or layout computation.
//          Serves as the engine registered by UseSkiaRenderer() until WriterEngine
//          (Phase 3) replaces it. ExecuteAsync handles only PingCommand.
//          LayoutInvalidated is never raised in Phase 2.
// DEPENDS: ILokiEngine, IFontManager, IImageStore, ILokiLogger,
//          StubPart, Phase1TestScene, PaintScene, PingCommand
// USED BY: LokiHostBuilder.UseSkiaRenderer (Scoped), LokiDocumentImpl
// PHASE:   2

using AppThere.Loki.Kernel.Geometry;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.Skia.Fonts;
using AppThere.Loki.Skia.Images;
using AppThere.Loki.Skia.Scene;
using AppThere.Loki.Skia.Testing;
using AppThere.Loki.LokiKit.Commands;
using AppThere.Loki.LokiKit.Document;
using AppThere.Loki.LokiKit.Events;
using AppThere.Loki.LokiKit.Host;

namespace AppThere.Loki.LokiKit.Engine;

public sealed class StubEngine : ILokiEngine
{
    private readonly IFontManager _fontManager;
    private readonly IImageStore  _imageStore;
    private readonly ILokiLogger  _logger;
    private PaintScene?           _scene;

    private static readonly StubPart Page1 =
        new(0, new SizeF(595f, 842f), "Page 1");

    public StubEngine(IFontManager fontManager, IImageStore imageStore, ILokiLogger logger)
    {
        _fontManager = fontManager;
        _imageStore  = imageStore;
        _logger      = logger;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public Task InitialiseAsync(Stream source, OpenOptions options, CancellationToken ct)
    {
        (_scene, _) = Phase1TestScene.Build(_fontManager, _logger, _imageStore);
        return Task.CompletedTask;
    }

    public Task InitialiseNewAsync(DocumentKind kind, CancellationToken ct)
    {
        (_scene, _) = Phase1TestScene.Build(_fontManager, _logger, _imageStore);
        return Task.CompletedTask;
    }

    // ── Document model ────────────────────────────────────────────────────────

    public int PartCount => 1;

    public ILokiPart GetPart(int partIndex)
    {
        if (partIndex != 0)
            throw new ArgumentOutOfRangeException(nameof(partIndex),
                partIndex, "StubEngine has exactly one part (index 0).");
        return Page1;
    }

    public bool IsModified => false;

    // ── Rendering ─────────────────────────────────────────────────────────────

    public PaintScene GetPaintScene(int partIndex)
    {
        if (partIndex != 0)
            throw new ArgumentOutOfRangeException(nameof(partIndex),
                partIndex, "StubEngine has exactly one part (index 0).");
        return _scene ?? throw new InvalidOperationException(
            "InitialiseAsync must be called before GetPaintScene.");
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public Task<bool> ExecuteAsync(ILokiCommand command, CancellationToken ct) =>
        Task.FromResult(command is PingCommand);

    public bool CanExecute(ILokiCommand command) => command is PingCommand;

    // ── Persistence ───────────────────────────────────────────────────────────

    public Task SaveAsync(Stream output, SaveFormat format, CancellationToken ct) =>
        throw new NotSupportedException("StubEngine does not support Save.");

    // ── Change notifications ──────────────────────────────────────────────────

    // Never raised in Phase 2 — event must exist per interface contract.
#pragma warning disable CS0067  // event never used — intentional in Phase 2
    public event EventHandler<EngineLayoutInvalidatedEventArgs>? LayoutInvalidated;
#pragma warning restore CS0067

    // ── Dispose ───────────────────────────────────────────────────────────────

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
