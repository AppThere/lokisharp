// LAYER:   AppThere.Loki.LokiKit — Hourglass Waist
// KIND:    Builder (concrete)
// PURPOSE: Fluent builder that wires the DI container for a given host context.
//          Each Use* call registers the appropriate implementation.
//          Extension methods in platform assemblies add further Use* overloads.
//          Phase 2: UseHeadlessSurfaces, UseSkiaRenderer, UseSkiaFonts,
//                   UseConsoleLogger are the only built-in registrations.
// DEPENDS: ILokiHost, ILokiLogger, IFontManager, ITileRenderer, IImageCodec,
//          IImageStore, IRenderSurfaceFactory, ILokiEngine,
//          Microsoft.Extensions.DependencyInjection
// USED BY: lokiprint CLI, Avalonia app host (Phase 4+), test harnesses
// PHASE:   2
// ADR:     ADR-006

using AppThere.Loki.Kernel.Images;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.LokiKit.Engine;
using AppThere.Loki.Skia.Fonts;
using AppThere.Loki.Skia.Images;
using AppThere.Loki.Skia.Rendering;
using AppThere.Loki.Skia.Surfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AppThere.Loki.LokiKit.Host;

public sealed class LokiHostBuilder
{
    private readonly IServiceCollection _services = new ServiceCollection();
    private bool _surfacesRegistered;
    private bool _rendererRegistered;
    private bool _fontsRegistered;
    private bool _loggerRegistered;
    private LokiHostOptions _options = LokiHostOptions.Default;

    /// <summary>
    /// Set host options. Defaults to LokiHostOptions.Default.
    /// </summary>
    public LokiHostBuilder WithOptions(LokiHostOptions options)
    {
        _options = options;
        return this;
    }

    /// <summary>
    /// Register CPU-only (headless) render surfaces. Required for lokiprint
    /// and all non-GPU hosts.
    /// Registers: IRenderSurfaceFactory → HeadlessSurfaceFactory (Singleton)
    /// </summary>
    public LokiHostBuilder UseHeadlessSurfaces()
    {
        _services.AddSingleton<IRenderSurfaceFactory, HeadlessSurfaceFactory>();
        _surfacesRegistered = true;
        return this;
    }

    /// <summary>
    /// Register the SkiaSharp tile renderer and image pipeline.
    /// Registers: ITileRenderer  → TileRenderer  (Singleton)
    ///            IImageCodec    → SkiaImageCodec (Singleton)
    ///            IImageStore    → SkiaImageStore  (Singleton)
    ///            ILokiEngine    → StubEngine      (Scoped — one per document)
    /// Requires UseHeadlessSurfaces or UseAvaloniaSurfaces to be called first.
    /// </summary>
    public LokiHostBuilder UseSkiaRenderer()
    {
        _services.AddSingleton<ITileRenderer, TileRenderer>();
        _services.AddSingleton<IImageCodec, SkiaImageCodec>();
        _services.AddSingleton<IImageStore, SkiaImageStore>();
        _services.AddScoped<ILokiEngine, StubEngine>();
        _rendererRegistered = true;
        return this;
    }

    /// <summary>
    /// Register the SkiaSharp font manager with bundled variable fonts.
    /// Registers: IFontManager → SkiaFontManager (Singleton)
    /// </summary>
    public LokiHostBuilder UseSkiaFonts()
    {
        _services.AddSingleton<IFontManager, SkiaFontManager>();
        _fontsRegistered = true;
        return this;
    }

    /// <summary>
    /// Register a console logger (stdout/stderr). Default for CLI hosts.
    /// Registers: ILokiLogger → ConsoleLogger (Singleton)
    /// </summary>
    public LokiHostBuilder UseConsoleLogger()
    {
        _services.AddSingleton<ILokiLogger, ConsoleLogger>();
        _loggerRegistered = true;
        return this;
    }

    /// <summary>
    /// Register additional services for test or custom scenarios.
    /// Allows injecting mocks/stubs without subclassing.
    /// </summary>
    public LokiHostBuilder ConfigureServices(Action<IServiceCollection> configure)
    {
        configure(_services);
        return this;
    }

    /// <summary>
    /// Build the ILokiHost. Throws InvalidOperationException if required
    /// registrations are missing (surfaces, renderer, fonts, logger).
    /// </summary>
    public ILokiHost Build()
    {
        if (!_surfacesRegistered)
            throw new InvalidOperationException(
                "No surface factory registered. Call UseHeadlessSurfaces() or UseAvaloniaSurfaces().");
        if (!_rendererRegistered)
            throw new InvalidOperationException(
                "No renderer registered. Call UseSkiaRenderer().");
        if (!_fontsRegistered)
            throw new InvalidOperationException(
                "No font manager registered. Call UseSkiaFonts().");
        if (!_loggerRegistered)
            throw new InvalidOperationException(
                "No logger registered. Call UseConsoleLogger() or UseAvaloniaLogger().");

        _services.AddSingleton<ILokiHost, LokiHostImpl>();
        _services.AddSingleton(_options);

        var provider = _services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true });

        return provider.GetRequiredService<ILokiHost>();
    }
}
