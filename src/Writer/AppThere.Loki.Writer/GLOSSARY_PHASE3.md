# LokiKit Glossary — Phase 3 Additions

These terms extend GLOSSARY_PHASE2.md.

## LokiDocument
Immutable snapshot of a Writer document. Contains Body (list of BlockNode),
StyleRegistry, DefaultPageStyle, and document metadata.
Produced by OdfImporter; consumed by LayoutEngine.
Never mutated — WriterEngine wraps it in DocumentState.

## BlockNode
Abstract base for block-level content. Phase 3 concrete types:
  ParagraphNode — body text, headings, list items.

## ParagraphNode
A block container holding an ordered list of InlineNodes and a
fully-resolved ParagraphStyle. The primary block type in Phase 3.

## InlineNode
Abstract base for inline content within a paragraph. Concrete types:
  RunNode — shaped text with CharacterStyle.
  HardLineBreakNode — forced line break within a paragraph.
  TabNode — tab character (fixed-width space in Phase 3).

## RunNode
A text run: a string of text with a fully-resolved CharacterStyle.
Produced by OdfImporter from text:span or bare text content.
Shaped into GlyphClusters by LokiTextShaper in Stage 1 of layout.

## ParagraphStyle / CharacterStyle
Computed style structs stored on each model node. All measurements in pts.
Resolved from the full cascade by StyleResolver at import time.
The layout engine reads these only — never the StyleRegistry.

## StyleRegistry
Holds raw (pre-cascade) ParagraphStyleDef and CharacterStyleDef entries
from the source document. Populated in ODF import Passes 1 and 2.
Used by StyleResolver in Pass 3. Ignored by the layout engine.

## StyleResolver (IStyleResolver)
Resolves the full CSS-style cascade: document default → named style chain
→ direct formatting. Produces ParagraphStyle / CharacterStyle computed values.
Called by OdfImporter during Pass 3 only.

## DocumentState
Internal mutable wrapper in WriterEngine. Holds the current LokiDocument
snapshot and an integer Version that increments on each mutation.
Version is used as the LayoutCache key to detect stale entries.

## LayoutEngine (ILayoutEngine)
Four-stage pipeline: measure → K-P line break → page break → PaintScene.
Stateless except for the LayoutCache it receives as a parameter.
Returns one PaintScene per page.

## LayoutCache
Per-document cache of BrokenParagraph results keyed by
(paragraphIndex, docVersion, lineWidthPts). InvalidateFrom(n) evicts
all entries at or after paragraph n. Owned by WriterEngine.

## Box / Glue / Penalty
K-P primitive types (ADR-008).
  Box: fixed-width content that cannot break.
  Glue: flexible space with ideal/stretch/shrink widths.
  Penalty: break opportunity with cost (0=neutral, +Inf=forbidden, -Inf=forced).

## MeasuredParagraph
Stage 1 output. A paragraph as a flat sequence of LayoutItems (Box/Glue/Penalty),
ready for the K-P line breaker.

## BrokenParagraph
Stage 2 output. Optimal line break points and per-line adjustment ratios,
produced by KnuthPlassBreaker from a MeasuredParagraph.

## PageLayout / PlacedParagraph
Stage 3 output. BrokenParagraphs assigned to pages with y-offsets,
produced by PageBreaker.

## GlyphCluster
Shaped glyph sequence for one Box. Carries glyph IDs, advances, typeface,
and font size. Produced by LokiTextShaper in Stage 1.

## OdfImporter (IOdfImporter)
Parses ODT or FODT streams into LokiDocument via three-pass import.
Pass 1: named styles. Pass 2: automatic styles. Pass 3: document body.
Single public entry point: ImportAsync(stream, isFlat, fontManager, logger).

## Phase 3 Exit Criterion
  var host = new LokiHostBuilder()
      .UseHeadlessSurfaces().UseSkiaRenderer()
      .UseSkiaFonts().UseConsoleLogger()
      .UseWriterEngine()   // Phase 3 extension method
      .Build();
  await using var doc = await host.OpenAsync(
      File.OpenRead("sample.fodt"), OpenOptions.Default);
  using var view = host.CreateView(doc);
  // Rendered PDF contains visible text from sample.fodt
  await view.RenderToPdfAsync(File.OpenWrite("out.pdf"), PdfMetadata.Default);
