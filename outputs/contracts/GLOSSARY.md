# AppThere Loki — Glossary

Canonical definitions for terms used throughout the codebase, ADRs, and documentation.
When a term appears in a file header (DEPENDS, USED BY, PURPOSE), its meaning is defined here.

---

## Core Rendering Terms

**Tile**
A 512×512 physical-pixel rendered bitmap representing one rectangular region of a document Part.
The atomic unit of on-screen display and headless export.
Tiles are identified by TileKey = (partIndex, zoomLevel, tileCol, tileRow).

**TileRequest**
A value type containing (partIndex, zoomLevel, tileCol, tileRow, pixelSize, dpiScale).
The input to TileRenderer.RenderTile. Fully describes which tile to render and at what scale.

**TileKey**
The identity portion of a TileRequest: (partIndex, zoomLevel, tileCol, tileRow).
Used as the cache key in the tile cache.

**Part**
A single logical page, sheet, slide, or drawing within a document. Indexed from 0.
A Writer document with 10 pages has 10 Parts (Part 0 through Part 9).

**PaintScene**
An immutable, sealed record tree describing all visual elements of one Part.
Produced by the layout engine (Phase 3+) and consumed by TileRenderer and PdfRenderer.
Contains a list of PaintBands. See ADR-003.

**PaintBand**
A horizontal strip of a Part. Contains a list of PaintNodes and a LayoutVersion counter.
The unit of partial invalidation: only Bands with a changed LayoutVersion trigger tile re-renders.

**PaintNode**
One visual primitive in a PaintScene. Abstract base; concrete subtypes:
RectNode, RoundRectNode, GlyphRunNode, ImageNode, GroupNode, ShadowNode, PathNode, LineNode.

**LayoutVersion**
A monotonically increasing integer on each PaintBand.
When the layout engine re-runs, it increments LayoutVersion for affected Bands.
TileRenderer compares LayoutVersion against its cache to detect stale tiles.

**GlyphRun**
A sequence of glyph IDs, advances, and offsets sharing a single typeface, size, and colour.
The output of text shaping (HarfBuzz). Input to ILokiPainter.DrawGlyphRun.

**Golden PNG**
A committed reference image used for pixel-comparison rendering tests.
Stored in tests/rendering/Goldens/. Updated manually via the update-goldens workflow.
See CONTRIBUTING.md §6.

---

## Surfaces and Rendering

**IRenderSurface**
An opaque render target. May be backed by a CPU SKBitmap, a PDF page, or (Phase 4+) a GPU texture.
Always created by IRenderSurfaceFactory — never directly by callers.

**IRenderSurfaceFactory**
The single point of platform selection in the rendering stack.
Phase 1 implementation: HeadlessSurfaceFactory (CPU BitmapRenderSurface only).
Phase 4 implementation: AvaloniaSurfaceFactory (Metal/GL/CPU fallback).

**BitmapRenderSurface**
CPU-only render surface backed by SKBitmap. Used in Phase 1 and as Phase 4+ fallback.
Thread-safe: multiple BitmapRenderSurfaces can render in parallel on the ThreadPool.

**PdfRenderSurface**
Render surface backed by SKDocument.CreatePdf(). Produces vector PDF output.
Rendering is single-threaded and sequential (one page at a time).

**ILokiPainter**
The platform-neutral drawing API. Wraps SKCanvas without exposing SkiaSharp types.
All rendering goes through ILokiPainter — no direct SKCanvas access outside Loki.Skia.

---

## Font Terms

**FontDescriptor**
An immutable record describing a requested font (family, weight, slant, stretch, size).
The input to IFontManager.TryGetTypeface. Describes what is wanted, not what was resolved.

**ILokiTypeface**
A platform-neutral handle to a resolved typeface (font + style). Wraps SKTypeface.
Callers hold ILokiTypeface; only SkiaFontManager holds the underlying SKTypeface.

**IFontManager**
Resolves FontDescriptors to ILokiTypeface instances using the two-tier strategy:
bundled fonts first, then system fonts. See ADR-001.

**Bundled font**
A variable font embedded as an assembly resource in AppThere.Loki.Skia.
Phase 1 bundle: Inter, Newsreader, NotoSansArabic, NotoSansSC, NotoColorEmoji.

**LokiTextShaper**
Wraps HarfBuzz (via SkiaSharp.HarfBuzz) to perform OpenType text shaping.
Input: string + FontDescriptor. Output: IReadOnlyList<GlyphRun>.

**FontAxis**
A variable font design axis (e.g. wght, opsz, wdth). Exposed by IFontManager.TryGetVariableAxes.

---

## Architecture Terms

**LokiKit**
Phase 2+. The hourglass waist between the UI and the document engines.
The only interface the UI layer ever touches. Based on LibreOfficeKit.
Do not reference in Phase 1.

**Kernel**
AppThere.Loki.Kernel. Contains geometry, colour, font descriptors, storage abstractions,
image codec contracts, logging, and error types. Has zero third-party runtime dependencies.
Every other layer depends on Kernel; Kernel depends on nothing above it.

**lokiprint**
The headless CLI tool (tools/lokiprint). Phase 1 exit criterion:
`lokiprint test-render --output out/test.pdf` must succeed.

**ImageRef**
A content-addressed handle to an image: SHA-256 hash + dimensions + MIME type.
Images are never embedded as byte arrays in PaintNodes — they are referenced by ImageRef.
Pixel data lives in IImageStore, keyed by ImageRef.ContentHash.

**IImageStore**
Manages decoded image bitmaps, keyed by ImageRef. Supports eviction under memory pressure.
Distinct from IImageCodec: the codec decodes; the store caches.

**LokiPath**
An immutable sequence of PathVerb values (MoveTo, LineTo, CubicTo, etc.).
Built via LokiPath.Builder; frozen at Build(). Lazily converts to SKPath on first render.

**HeadlessSurfaceFactory**
Phase 1 implementation of IRenderSurfaceFactory. Always creates CPU surfaces.
IsGpuAvailable = false. PreferredBackend = RenderBackend.Cpu.

---

## Error Handling Terms (ADR-004)

**TryGet pattern**
bool + out parameter. Used for expected, frequent failures (font not found, cache miss).
Never throws. Caller always handles the false case inline.

**LokiException**
Abstract base for all domain exceptions. Never thrown directly.
Typed subclasses: StorageException, FontLoadException, TileRenderException, etc.

**Result<T,E>**
Discriminated union for async pipelines where multiple steps can fail with typed errors.
Used for font download, PDF export, EPUB export. Not the default — use TryGet first.

---

## Process Terms

**DCO**
Developer Certificate of Origin. Every commit must include Signed-off-by: (git commit -s).
Enforced by the DCO GitHub App on all PRs to main.

**RFC**
Request for Comments. Required before implementing any significant architectural change.
See GOVERNANCE.md §3 for the full lifecycle and template.

**ADR**
Architecture Decision Record. Documents architectural decisions and their rationale.
Stored in docs/adr/. Produced after an RFC is accepted (or directly for Phase 1 decisions).

**Phase 1**
The current implementation phase. Scope: AppThere.Loki.Kernel + AppThere.Loki.Skia + lokiprint CLI.
Exit criterion: `lokiprint test-render --output out/test.pdf` produces a valid PDF, all CI passes.
