// LAYER:   AppThere.Loki.Writer — Engine
// KIND:    Interface
// PURPOSE: Port for ODF import. Implemented by Format.Odf.OdfImporter.
//          Defined here (Writer layer) so that WriterEngine can depend on it
//          without creating a circular dependency between Writer and Format.Odf.
//          Format.Odf references Writer to implement this interface.
//          Three-pass import: named styles → automatic styles → document body.
//          Throws InvalidOperationException on structural errors.
// DEPENDS: LokiDocument, IFontManager, ILokiLogger
// USED BY: WriterEngine.InitialiseAsync, LokiHostBuilderExtensions
// PHASE:   3
// ADR:     ADR-009

using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.Skia.Fonts;
using AppThere.Loki.Writer.Model;

namespace AppThere.Loki.Writer.Engine;

public interface IOdfImporter
{
    /// <summary>
    /// Import an ODT or FODT stream into a LokiDocument.
    /// The stream is read fully — the caller may dispose it after this returns.
    /// isFlat: true for FODT (flat XML), false for ODT (ZIP container).
    /// Throws InvalidOperationException if the stream is not valid ODF.
    /// </summary>
    Task<LokiDocument> ImportAsync(
        Stream       source,
        bool         isFlat,
        IFontManager fontManager,
        ILokiLogger  logger,
        CancellationToken ct = default);
}
