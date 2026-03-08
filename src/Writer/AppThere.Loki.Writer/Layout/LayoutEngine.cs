// LAYER:   AppThere.Loki.Writer — Layout Engine
// KIND:    Implementation
// PURPOSE: Implements ILayoutEngine. Orchestrates the four-stage pipeline:
//          Stage 1 (InlineMeasurer) → Stage 2 (KnuthPlassBreaker) →
//          Stage 3 (PageBreaker) → Stage 4 (PaintSceneBuilder).
//          Uses LayoutCache to skip unchanged paragraphs. Cache key is
//          (paragraphIndex, document.LayoutVersion, lineWidthPts).
//          Always returns at least one PaintScene (blank page for empty docs).
//          Thread-safe for read-only concurrent calls with separate caches.
// DEPENDS: ILayoutEngine, InlineMeasurer, KnuthPlassBreaker, PageBreaker,
//          PaintSceneBuilder, LayoutCache, LokiDocument, IFontManager, ILokiLogger
// USED BY: WriterEngine
// PHASE:   3
// ADR:     ADR-008

using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.Skia.Fonts;
using AppThere.Loki.Skia.Scene;
using AppThere.Loki.Writer.Model;

namespace AppThere.Loki.Writer.Layout;

internal sealed class LayoutEngine : ILayoutEngine
{
    private readonly IFontManager _fontManager;
    private readonly ILokiLogger  _logger;

    public LayoutEngine(IFontManager fontManager, ILokiLogger logger)
    {
        _fontManager = fontManager;
        _logger      = logger;
    }

    // ── ILayoutEngine ─────────────────────────────────────────────────────────

    public IReadOnlyList<PaintScene> Layout(LokiDocument document, LayoutCache cache)
    {
        var measurer      = new InlineMeasurer(_fontManager, _logger);
        var breaker       = new KnuthPlassBreaker();
        var pageBreaker   = new PageBreaker();
        var sceneBuilder  = new PaintSceneBuilder(_fontManager, _logger);

        var pageStyle   = document.DefaultPageStyle;
        var lineWidth   = pageStyle.ContentWidthPts;
        var docVersion  = document.LayoutVersion;

        // Stages 1 + 2: measure and break each paragraph
        var broken = new List<BrokenParagraph>();
        var idx    = 0;

        foreach (var block in document.Body)
        {
            if (block is ParagraphNode para)
            {
                var cached = cache.TryGet(idx, docVersion, lineWidth);
                if (cached is not null)
                {
                    broken.Add(cached);
                }
                else
                {
                    var measured = measurer.Measure(para, lineWidth, idx);
                    var result   = breaker.Break(measured);
                    cache.Store(idx, docVersion, lineWidth, result);
                    broken.Add(result);
                }
                idx++;
            }
        }

        // Stage 3: page breaking
        var pages = pageBreaker.Break(broken, pageStyle);

        // Stage 4: PaintScene per page
        var scenes = new List<PaintScene>(pages.Length);
        foreach (var page in pages)
            scenes.Add(sceneBuilder.Build(page, pageStyle));

        if (scenes.Count == 0)
            scenes.Add(EmptyPage(0, pageStyle));

        return scenes;
    }

    private static PaintScene EmptyPage(int partIndex, PageStyle pageStyle) =>
        PaintScene.CreateBuilder(partIndex)
            .WithSize(pageStyle.WidthPts, pageStyle.HeightPts)
            .Build();
}
