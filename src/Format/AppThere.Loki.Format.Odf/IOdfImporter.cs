// LAYER:   AppThere.Loki.Format.Odf — ODF Import
// KIND:    Interface
// PURPOSE: Single public entry point for ODF import. Parses ODT or FODT
//          streams into an immutable LokiDocument. Three-pass import:
//          Pass 1 — named styles (styles.xml or office:styles section)
//          Pass 2 — automatic styles (content.xml / office:automatic-styles)
//          Pass 3 — document body (office:body / office:text)
//          StyleResolver is invoked in Pass 3 to compute cascaded styles.
//          Throws LokiOpenException on structural errors.
//          Logs and continues on content errors (unknown elements, bad units).
// DEPENDS: LokiDocument, IFontManager, ILokiLogger
// USED BY: WriterEngine.InitialiseAsync
// PHASE:   3
// ADR:     ADR-009

using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.Skia.Fonts;
using AppThere.Loki.Writer.Model;

namespace AppThere.Loki.Format.Odf;

// This interface is superseded by AppThere.Loki.Writer.Engine.IOdfImporter.
// Kept internal to avoid conflicts. OdfImporter implements the Writer-defined interface.
internal interface IOdfImporter
{
    /// <summary>
    /// Import an ODT or FODT stream into a LokiDocument.
    /// The stream is read fully — the caller may dispose it after this returns.
    /// isFlat: true for FODT (flat XML), false for ODT (ZIP container).
    /// Throws LokiOpenException if the stream is not valid ODF.
    /// </summary>
    Task<LokiDocument> ImportAsync(
        Stream       source,
        bool         isFlat,
        IFontManager fontManager,
        ILokiLogger  logger,
        CancellationToken ct = default);
}
