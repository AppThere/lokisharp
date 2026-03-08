// LAYER:   AppThere.Loki.Writer — Layout Engine
// KIND:    Class (mutable cache, document-scoped)
// PURPOSE: Caches BrokenParagraph results keyed by (paragraphIndex,
//          documentVersion, lineWidthPts). Allows the layout engine to
//          skip unchanged paragraphs on re-layout after edits.
//          One LayoutCache per open document (owned by WriterEngine).
//          Not thread-safe — accessed only from the layout thread.
// DEPENDS: BrokenParagraph
// USED BY: ILayoutEngine implementation, WriterEngine
// PHASE:   3
// ADR:     ADR-008

namespace AppThere.Loki.Writer.Layout;

public sealed class LayoutCache
{
    private readonly record struct CacheKey(int ParagraphIndex, int DocVersion, float LineWidthPts);
    private readonly Dictionary<CacheKey, BrokenParagraph> _cache = new();

    /// <summary>
    /// Returns the cached result if version and width match; null on miss.
    /// </summary>
    public BrokenParagraph? TryGet(int paragraphIndex, int docVersion, float lineWidthPts)
    {
        var key = new CacheKey(paragraphIndex, docVersion, lineWidthPts);
        return _cache.TryGetValue(key, out var result) ? result : null;
    }

    /// <summary>Store a layout result.</summary>
    public void Store(int paragraphIndex, int docVersion, float lineWidthPts,
                      BrokenParagraph result)
    {
        _cache[new CacheKey(paragraphIndex, docVersion, lineWidthPts)] = result;
    }

    /// <summary>
    /// Invalidate all entries at or after paragraphIndex.
    /// Called when a command modifies a paragraph — all subsequent
    /// paragraphs may reflow due to page break changes.
    /// </summary>
    public void InvalidateFrom(int paragraphIndex)
    {
        var toRemove = _cache.Keys
            .Where(k => k.ParagraphIndex >= paragraphIndex)
            .ToList();
        foreach (var key in toRemove)
            _cache.Remove(key);
    }

    /// <summary>Discard all cached results.</summary>
    public void Clear() => _cache.Clear();

    /// <summary>Number of cached paragraphs (for diagnostics).</summary>
    public int Count => _cache.Count;
}
