// LAYER:   AppThere.Loki.Writer — Engine
// KIND:    Interface contract for WriterEngine
// PURPOSE: WriterEngine is the Phase 3 ILokiEngine implementation for
//          Writer documents (ODT, FODT, DOCX). It owns:
//            - DocumentState (versioned LokiDocument snapshot)
//            - LayoutCache (per-document, invalidated on edit)
//            - ILayoutEngine (performs layout on demand)
//            - OdfImporter (parses source stream into LokiDocument)
//          WriterEngine replaces StubEngine for Writer documents.
//          Registered as Scoped in DI (one per open document).
// DEPENDS: ILokiEngine, LokiDocument, ILayoutEngine, LayoutCache, OdfImporter
// USED BY: LokiHostBuilder (registered as ILokiEngine for Writer formats)
// PHASE:   3
// ADR:     ADR-007, ADR-008

using AppThere.Loki.Writer.Model;

namespace AppThere.Loki.Writer.Engine;

/// <summary>
/// Internal versioned wrapper around LokiDocument.
/// Owned by WriterEngine. Not exposed outside the Writer assembly.
/// </summary>
internal sealed class DocumentState
{
    public LokiDocument Snapshot    { get; private set; }
    public int          Version     { get; private set; }
    public bool         IsModified  { get; private set; }

    public DocumentState(LokiDocument initial)
    {
        Snapshot   = initial;
        Version    = 0;
        IsModified = false;
    }

    /// <summary>
    /// Replace the snapshot with a new version.
    /// Increments Version and sets IsModified = true.
    /// </summary>
    public void Apply(LokiDocument next)
    {
        Snapshot   = next;
        Version    = unchecked(Version + 1);
        IsModified = true;
    }
}
