// LAYER:   AppThere.Loki.Avalonia — Host
// KIND:    Static class (LokiHostBuilder extension methods)
// PURPOSE: Registers all Phase 4 Avalonia services with the DI container.
//          Must be called after UseSkiaRenderer() and UseWriterEngine()
//          so that IFontManager and ILokiEngine are already registered.
// DEPENDS: LokiHostBuilder, AvaloniaSurfaceFactory, TileCacheOptions
// USED BY: AppThere.Loki.App (entry point project)
// PHASE:   4
// ADR:     ADR-010, ADR-011

using Microsoft.Extensions.DependencyInjection;
using AppThere.Loki.LokiKit.Host;
using AppThere.Loki.Avalonia.Cache;
using AppThere.Loki.Avalonia.Surfaces;
using AppThere.Loki.Kernel.Surfaces;

namespace AppThere.Loki.Avalonia.Host;

public static class LokiHostBuilderExtensions
{
    /// <summary>
    /// Registers Avalonia surfaces, tile cache, and breakpoint resolver.
    /// options defaults to TileCacheOptions.Desktop if not supplied.
    /// </summary>
    public static LokiHostBuilder UseAvaloniaSurfaces(
        this LokiHostBuilder builder,
        TileCacheOptions?    options = null)
    {
        var resolved = options ?? TileCacheOptions.Desktop;

        builder.ConfigureServices(services =>
        {
            services.AddSingleton(resolved);
            services.AddSingleton<ISurfaceFactory, AvaloniaSurfaceFactory>();
        });

        return builder;
    }
}
