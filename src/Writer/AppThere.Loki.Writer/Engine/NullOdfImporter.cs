// LAYER:   AppThere.Loki.Writer — Engine
// KIND:    Implementation
// PURPOSE: No-op IOdfImporter that always returns LokiDocument.Empty.
//          Used as the default registration in UseWriterEngine() so that
//          WriterEngine can be resolved without a real ODF implementation.
//          Callers that need actual ODF parsing must override this registration
//          with OdfImporter from AppThere.Loki.Format.Odf after calling
//          UseWriterEngine() via ConfigureServices.
// DEPENDS: IOdfImporter, LokiDocument, IFontManager, ILokiLogger
// USED BY: LokiHostBuilderExtensions.UseWriterEngine (default DI registration)
// PHASE:   3
// ADR:     ADR-009

using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.Skia.Fonts;
using AppThere.Loki.Writer.Model;

namespace AppThere.Loki.Writer.Engine;

internal sealed class NullOdfImporter : IOdfImporter
{
    public Task<LokiDocument> ImportAsync(
        Stream            source,
        bool              isFlat,
        IFontManager      fontManager,
        ILokiLogger       logger,
        CancellationToken ct = default)
        => Task.FromResult(LokiDocument.Empty);
}
