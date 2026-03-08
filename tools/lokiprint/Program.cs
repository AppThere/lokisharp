// LAYER:   AppThere.Loki.Tools.LokiPrint — Host
// KIND:    Host
// PURPOSE: Entry point for the lokiprint headless CLI.
//          Builds a MEL generic host with full DI registration for Phase 1 services.
//          Dispatches the 'test-render' sub-command to TestRenderCommand.Run().
//          Phase 1 exit criterion: 'lokiprint test-render --output out/test.pdf'
//          produces a valid PDF containing all PaintNode types.
//          Does NOT implement document parsing, LokiKit, or Phase 2+ features.
// DEPENDS: TestRenderCommand, ConsoleLogger, SkiaFontManager, SkiaImageCodec,
//          SkiaImageStore, HeadlessSurfaceFactory, IFontManager, ILokiLogger
// USED BY: CI smoke test, developer testing
// PHASE:   1

using AppThere.Loki.Kernel.Images;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.Skia.Fonts;
using AppThere.Loki.Skia.Images;
using AppThere.Loki.Skia.Surfaces;
using AppThere.Loki.Tools.LokiPrint;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

if (args.Length < 1 || args[0] != "test-render")
{
    Console.Error.WriteLine(
        "Usage: lokiprint test-render --output <path> [--tile <col,row>] [--zoom <level>]");
    return 1;
}

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<ILokiLogger>(_ => ConsoleLogger.Instance);
        services.AddSingleton<IFontManager>(sp =>
            new SkiaFontManager(sp.GetRequiredService<ILokiLogger>()));
        services.AddSingleton<IImageCodec>(sp =>
            new SkiaImageCodec(sp.GetRequiredService<ILokiLogger>()));
        services.AddSingleton<IImageStore>(sp =>
            new SkiaImageStore(
                sp.GetRequiredService<IImageCodec>(),
                sp.GetRequiredService<ILokiLogger>()));
        services.AddSingleton<IRenderSurfaceFactory>(sp =>
            new HeadlessSurfaceFactory(sp.GetRequiredService<ILokiLogger>()));
    })
    .Build();

var logger      = host.Services.GetRequiredService<ILokiLogger>();
var fontManager = host.Services.GetRequiredService<IFontManager>();

return TestRenderCommand.Run(args[1..], fontManager, logger);
