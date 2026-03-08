# LokiKit Glossary — Phase 4 Additions

These terms extend GLOSSARY_PHASE3.md.

## AppThere.Loki.Avalonia
Shared Avalonia UI library. Contains all controls, the tile cache,
surface factory, and host wiring. Referenced by all platform entry points
(AppThere.Loki.App on desktop, future Android/iOS heads).

## AppThere.Loki.App
Thin desktop entry point. OutputType=Exe. References AppThere.Loki.Avalonia
and AppThere.Loki.Format.Odf. Contains only Program.cs and app manifests.
Phase 5 adds AppThere.Loki.App.Android and AppThere.Loki.App.iOS.

## LokiApplication
Avalonia Application subclass. Builds LokiHost, selects TileCacheOptions
based on available system memory, and creates LokiMainWindow.

## LokiMainWindow
Root Avalonia window. Implements two layouts (Phone, Normal).
Responds to SizeChanged by re-evaluating LayoutBreakpoint.
Owns the tab strip of open ILokiDocument instances.

## LayoutBreakpoint
Five-value enum: Phone / Compact / Normal / Wide / UltraWide.
Resolved from window width in DIPs by LayoutBreakpointResolver.
Phase 4: Phone and Normal implemented. Others fall through.

## LayoutBreakpointResolver
Static helper. Resolve(widthDips) → LayoutBreakpoint.
NearestImplemented(bp) → nearest Phase 4 implemented breakpoint.

## LokiTileControl
Avalonia Control (not UserControl) that renders a document view as a
scrollable tiled grid. Owns ILokiTileCache. Handles touch pan and
pinch-to-zoom via Avalonia gesture recognizers. Calls InvalidateVisual()
when TileReady fires.

## ILokiTileCache / LokiTileCache
Viewport-aware tile cache. Four zones: Hot / Warm / Cool / Cold.
UpdateViewport() drives zone reclassification and eviction.
In-flight deduplication via Dictionary<TileKey, Task<SKBitmap>>.
TileReady event fires on the UI thread when a render completes.

## TileKey
readonly record struct — (PartIndex, TileX, TileY, Zoom).
Zoom included so tiles are invalidated automatically on zoom change.

## TileCacheOptions
Configuration record for the tile cache. Static defaults:
  TileCacheOptions.Desktop — 256MB cap, 2×/4× multipliers.
  TileCacheOptions.Mobile  — 64MB cap, 1.5×/3× multipliers.
Runtime-overridable via with-expression (e.g. MemoryCapBytes = ram/8).

## TileZone
Eviction classification: Hot (visible), Warm (keep), Cool (retain),
Cold (evict). Assigned during UpdateViewport().

## CachedTile
Internal cache entry. Holds WriteableBitmap, ByteCost, LastAccessed, Zone.

## ViewportGeometry
Immutable snapshot of scroll position, viewport size, zoom, and tile size.
Passed to ILokiTileCache.UpdateViewport on every scroll/resize.

## LokiCompositionDrawOp
Implements Avalonia ICustomDrawOperation. Holds a PositionedTile snapshot.
Called on the render thread by Avalonia. Draws white background + tiles.
No tile rendering inside — all bitmaps are pre-computed on the thread pool.

## PositionedTile
Record: (TileKey, ScreenRect in DIPs, WriteableBitmap?). Null bitmap
means tile not yet rendered; LokiCompositionDrawOp draws a placeholder.

## AvaloniaSurfaceFactory
Implements ISurfaceFactory. Creates CPU-backed SKSurfaces for tile
rendering on the thread pool. GPU-backed surfaces deferred to Phase 6.

## UseAvaloniaSurfaces()
LokiHostBuilder extension method. Registers AvaloniaSurfaceFactory,
LokiTileCache (transient), and TileCacheOptions (singleton).
Must be called after UseSkiaRenderer() and UseWriterEngine().

## Phase 4 Exit Criterion
The Avalonia desktop app opens simple.fodt, displays it in a scrollable
tiled view, and the rendered text matches the Phase 3 PDF visually.
Window resize triggers layout breakpoint switch between Phone and Normal.
