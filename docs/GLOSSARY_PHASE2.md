# LokiKit Glossary — Phase 2 Additions

These terms extend GLOSSARY.md from Phase 1.

## ILokiHost
Process-level entry point. One per process. Created via LokiHostBuilder.
Owns the root DI container (singletons: IFontManager, IImageStore,
ITileRenderer, IRenderSurfaceFactory, ILokiLogger).
Opens documents via OpenAsync; creates views via CreateView.

## ILokiDocument
Handle to one open document. One per open file/untitled.
Lives in a document-scoped DI child scope.
Owns the ILokiEngine for that document.
Fires Changed when content or layout is invalidated.

## ILokiView
Rendering viewport onto one ILokiDocument. Lightweight — holds zoom,
scroll position, and active part index. Does not cache tiles.
Fires TileInvalidated when the document changes.
Created by ILokiHost.CreateView; disposed when the view panel closes.

## ILokiEngine
Implemented by each document engine (WriterEngine, CalcEngine, etc.).
One per open document. Owns the document model and PaintScene cache.
Returns PaintScene for a given part via GetPaintScene(partIndex).
Phase 2: StubEngine returns Phase1TestScene for all parts.

## ILokiCommand
Marker interface for all document mutations. Implemented as sealed records.
Dispatched through ILokiDocument.ExecuteAsync.
Enables undo/redo (Phase 5) and CRDT collaborative editing (Phase 5).

## LokiHostBuilder
Fluent builder for ILokiHost. Registers platform-specific implementations
(UseHeadlessSurfaces / UseAvaloniaSurfaces, UseSkiaFonts, UseSkiaRenderer,
UseConsoleLogger / UseAvaloniaLogger) then calls Build() to create the host.

## StubDocument / StubEngine
Phase 2 implementations that return Phase1TestScene without any real
document model. Used by lokiprint in Phase 2 and by all Phase 2 tests.
Replaced by WriterEngine in Phase 3.

## DocumentKind
Enum: Writer, Calc, Draw, Impress. Identifies which engine handles a document.

## TileInvalidated
Event fired by ILokiView when stale tiles must be re-requested.
Carries a list of TileKeys; empty list means all tiles for the active part.
The UI (Phase 4) subscribes and schedules new RenderTileAsync calls.

## DocumentChanged
Event fired by ILokiDocument. Carries DocumentChangeKind:
  ContentChanged  — layout must be recomputed
  CosmeticChanged — only visual properties changed; layout unchanged
  StructureChanged — parts inserted or removed

## Phase 2 Exit Criterion
  var host = new LokiHostBuilder()
      .UseHeadlessSurfaces().UseSkiaRenderer()
      .UseSkiaFonts().UseConsoleLogger().Build();
  await using var doc  = await host.OpenAsync(Stream.Null, OpenOptions.Default);
  using        var view = host.CreateView(doc);
  var bitmap = await view.RenderTileAsync(TileRequest.ForHeadless(0, 1f, 0, 0));
  // bitmap is a valid 512×512 SKBitmap containing Phase1TestScene content
