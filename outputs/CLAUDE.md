# AppThere Loki — Claude Code Guide

This file is read by Claude Code at the start of every session.
It is the authoritative orientation for AI-assisted development on this project.

---

## What This Project Is

AppThere Loki is a cross-platform office suite (.NET 9 / Avalonia UI / SkiaSharp).
It targets Windows, macOS, Linux, Android, and iOS from one codebase.
Current status: **Phase 1 — Kernel & Skia Renderer** (no UI, no document engine yet).

The exit criterion for Phase 1 is: `lokiprint test-render --output out/test.pdf`
produces a valid PDF containing all PaintNode types, and all CI jobs pass.

---

## Architecture in One Paragraph

The stack is layered: **UI → LokiKit → Engine → Format/Export → Skia → Kernel**.
No layer may import from a layer above it — enforced by architecture xUnit tests.
**LokiKit** (Phase 2+) is the hourglass waist: the only interface the UI touches.
**Skia** contains the renderer, font manager, and surface factory.
**Kernel** contains geometry, colour, font descriptors, storage, and codec contracts.
Everything above Kernel is Phase 2+. Do not create stubs for it in Phase 1.

---

## Non-Negotiable Rules

These apply to every file you create or modify. CI enforces them all.

1. **File header required.** Every `.cs` file starts with:
   ```
   // LAYER:   <project> — <layer label>
   // KIND:    <Interface|Implementation|Record|Builder|Registry|Adapter|Host|Tests|Enum>
   // PURPOSE: <what this type does; what it explicitly does NOT do>
   // DEPENDS: <direct dependency type names; omit BCL>
   // USED BY: <direct consumers>
   // PHASE:   1
   // ADR:     <ADR-NNN if applicable>
   ```

2. **One primary type per file.** File name matches type name exactly.

3. **300-line ceiling.** No `.cs` file exceeds 300 non-blank, non-comment lines.
   If a type grows beyond 300 lines, decompose it — do not use partial classes.

4. **Interfaces only in constructors.** Constructor parameters are always interfaces,
   never concrete classes.

5. **ImmutableArray<T> for immutable collections.** Never `List<T>` or `T[]` for
   data that must not change after construction.

6. **No forbidden imports.** Never import from a layer above your current layer.
   `AppThere.Loki.Kernel` must not reference `AppThere.Loki.Skia` or anything above.
   `AppThere.Loki.Skia` must not reference any Engine, Format, Export, LokiKit, or UI project.

7. **No GNU licences.** Never introduce a dependency licensed under GPL, LGPL, AGPL,
   EUPL, CDDL, or any copyleft licence. This is a hard rule — App Store distribution
   depends on it. Permitted: MIT, BSD-2/3, Apache-2.0, ISC, OFL-1.1, Zlib, CC0.

8. **DCO sign-off.** Every commit must have `Signed-off-by:` (use `git commit -s`).

---

## Project Structure

```
AppThere.Loki.sln
├─ src/
│   ├─ Kernel/
│   │   ├─ AppThere.Loki.Kernel/          ← geometry, colour, fonts, storage, codec contracts
│   │   │   ├─ Geometry/                  ← PointF, SizeF, RectF, Thickness, Matrix3x2F
│   │   │   ├─ Color/                     ← Color, DpiScale
│   │   │   ├─ Fonts/                     ← FontDescriptor, FontWeight, FontSlant, FontStretch
│   │   │   ├─ Storage/                   ← IStorage, PhysicalStorage, MemoryStorage
│   │   │   ├─ Images/                    ← IImageCodec, ImageData, PixelFormat
│   │   │   └─ Logging/                   ← ILokiLogger
│   │   └─ AppThere.Loki.Skia/            ← Skia renderer (depends on Kernel only)
│   │       ├─ Surfaces/                  ← IRenderSurface, IRenderSurfaceFactory, implementations
│   │       ├─ Painting/                  ← ILokiPainter, LokiSkiaPainter, paint value types
│   │       ├─ Scene/                     ← PaintScene, PaintBand, PaintNode, Nodes/
│   │       ├─ Fonts/                     ← IFontManager, SkiaFontManager, LokiTextShaper
│   │       ├─ Images/                    ← IImageStore, SkiaImageStore, SkiaImageCodec
│   │       └─ Paths/                     ← LokiPath, PathVerb
│   └─ (Engine/, LokiKit/, UI/ — Phase 2+, do not create)
├─ tools/
│   └─ lokiprint/                         ← headless CLI (Phase 1 exit criterion)
├─ tests/
│   ├─ unit/AppThere.Loki.Tests.Unit/     ← mirrors src/ folder structure
│   └─ rendering/AppThere.Loki.Tests.Rendering/  ← golden PNG tests
└─ docs/
    ├─ adr/                               ← ADR-001 through ADR-004 (already written)
    └─ *.md / *.docx                      ← project documentation
```

---

## Key Decisions Already Made (read the ADRs)

| Decision | ADR | Summary |
|---|---|---|
| Font pipeline | ADR-001 | Two-tier: bundled variable fonts first, then system fonts. Inter + Newsreader + Noto bundled in Phase 1. |
| Skia surfaces | ADR-002 | CPU BitmapRenderSurface in Phase 1 only. GPU (Metal/GL) deferred to Phase 4. HeadlessSurfaceFactory implements IRenderSurfaceFactory. |
| PaintScene design | ADR-003 | Sealed immutable record hierarchy. ImmutableArray<T> throughout. Band model for partial invalidation. System.Text.Json source-generated serialisation. |
| Error handling | ADR-004 | Three tiers: TryGet (expected failures), typed LokiException subclasses (unexpected), Result<T,E> (async pipelines). Never throw Exception directly. |

---

## NuGet Package Versions (Phase 1, pinned)

| Package | Version |
|---|---|
| SkiaSharp | 2.88.8 |
| SkiaSharp.HarfBuzz | 2.88.8 |
| SkiaSharp.NativeAssets.Win32 | 2.88.8 |
| SkiaSharp.NativeAssets.macOS | 2.88.8 |
| SkiaSharp.NativeAssets.Linux | 2.88.8 |
| Microsoft.Extensions.DependencyInjection | 9.0.0 |
| Microsoft.Extensions.Hosting | 9.0.0 |
| Microsoft.Extensions.Logging.Abstractions | 9.0.0 |
| xunit | 2.9.0 |
| xunit.runner.visualstudio | 2.8.2 |
| Microsoft.NET.Test.Sdk | 17.11.0 |
| FluentAssertions | 6.12.0 |
| NSubstitute | 5.1.0 |
| BenchmarkDotNet | 0.14.0 |

---

## Claude Code Session Protocol

When starting a task, load files in this order:
1. The interface file(s) you are implementing against
2. The input/return type files those interfaces use
3. The corresponding test file (this is the specification)
4. The relevant ADR(s)

Always state at the start of a session:
- Which Track and task number you are working on (e.g. "Track A, Task 3")
- Which files you will create or modify
- Which interfaces you depend on

When you finish a task:
- Confirm all new files have the standard file header
- Confirm no file exceeds 300 lines
- Confirm tests follow Method_Condition_ExpectedResult naming
- Run `dotnet build` and `dotnet test` before declaring done

---

## What NOT to Do

- Do not create any project in Engine/, LokiKit/, or UI/ — those are Phase 2+
- Do not add GPU surface implementations — HeadlessSurfaceFactory is CPU-only in Phase 1
- Do not add Avalonia, any UI framework, or any rendering-to-screen capability
- Do not use partial classes — decompose instead
- Do not return null from public APIs — use TryGet, Result<T,E>, or empty collections
- Do not catch `Exception` (base) except at top-level CLI boundary
- Do not use `List<T>` for collections that must be immutable after construction
- Do not introduce any dependency not in the pinned NuGet list above without discussion

---

## Glossary (quick reference — full version in docs/GLOSSARY.md)

| Term | Meaning |
|---|---|
| **Tile** | A 512×512 px rendered bitmap representing one rectangular region of a document part. The atomic unit of display. |
| **TileRequest** | (partIndex, zoomLevel, tileCol, tileRow) — the key identifying one tile. |
| **Part** | A single logical page/sheet/slide/drawing within a document. Indexed from 0. |
| **PaintScene** | Immutable record tree describing all visual elements of one Part. Input to TileRenderer. |
| **PaintBand** | A horizontal strip of a Part containing a subset of PaintNodes. Unit of partial invalidation. |
| **PaintNode** | One visual primitive in a PaintScene (RectNode, GlyphRunNode, ImageNode, etc.). |
| **ILokiPainter** | The platform-neutral drawing API. Wraps SKCanvas. Never exposes Skia types. |
| **IRenderSurface** | An opaque render target (CPU bitmap, PDF page, GPU texture). Created by IRenderSurfaceFactory. |
| **LokiKit** | Phase 2+. The hourglass waist between UI and engine. Do not reference in Phase 1. |
| **lokiprint** | The headless CLI tool. Phase 1 exit criterion. |
| **Golden PNG** | A committed reference image used for pixel-comparison rendering tests. |
| **Band** | See PaintBand. |
| **LayoutVersion** | A monotonically increasing counter on each PaintBand. TileRenderer uses it to detect which tiles need re-rendering. |
