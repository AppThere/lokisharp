// LAYER:   AppThere.Loki.Writer — Engine
// KIND:    Implementation
// PURPOSE: Extension method for LokiHostBuilder to register the Writer engine.
//          UseWriterEngine() replaces StubEngine with WriterEngine as the
//          ILokiEngine implementation for Writer documents (ODT, FODT).
//          Also registers IOdfImporter → OdfImporter and
//          ILayoutEngine → LayoutEngine as Scoped services.
//          Must be called after UseSkiaRenderer() (which registers StubEngine)
//          to override the default engine registration.
// DEPENDS: LokiHostBuilder, WriterEngine, OdfImporter, LayoutEngine,
//          ILokiEngine, IOdfImporter, ILayoutEngine
// USED BY: lokiprint CLI Program.cs, test harnesses
// PHASE:   3
// ADR:     ADR-007, ADR-008

using AppThere.Loki.LokiKit.Engine;
using AppThere.Loki.LokiKit.Host;
using AppThere.Loki.Writer.Engine;
using AppThere.Loki.Writer.Layout;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AppThere.Loki.Writer;

public static class LokiHostBuilderExtensions
{
    /// <summary>
    /// Replace StubEngine with WriterEngine as the document engine.
    /// Registers LayoutEngine and NullOdfImporter (empty-document fallback).
    /// To enable real ODF parsing, override IOdfImporter with OdfImporter
    /// (from AppThere.Loki.Format.Odf) via builder.ConfigureServices.
    /// Call after UseSkiaRenderer().
    /// </summary>
    public static LokiHostBuilder UseWriterEngine(this LokiHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Register options singleton if not already present
            services.TryAddSingleton(LokiHostOptions.Default);

            // Remove the StubEngine registration added by UseSkiaRenderer()
            var existing = services.FirstOrDefault(
                d => d.ServiceType == typeof(ILokiEngine));
            if (existing is not null) services.Remove(existing);

            // Register Writer-specific services (Scoped: one per document)
            services.AddScoped<ILayoutEngine, LayoutEngine>();
            services.AddScoped<IOdfImporter, NullOdfImporter>();
            services.AddScoped<ILokiEngine, WriterEngine>();
        });

        return builder;
    }
}
