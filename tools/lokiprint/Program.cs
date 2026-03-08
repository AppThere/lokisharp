// LAYER:   AppThere.Loki.Tools.LokiPrint — Host
// KIND:    Host
// PURPOSE: Entry point for the lokiprint headless CLI.
//          Builds an ILokiHost via LokiHostBuilder and dispatches the
//          'test-render' sub-command to TestRenderCommand.ExecuteAsync().
//          Phase 3 exit criterion: 'lokiprint test-render --input <fodt> --output out/phase3.pdf'
//          and 'lokiprint test-render --output out/phase3-blank.pdf' both succeed.
//          OdfImporter is registered to enable real FODT/ODT import with --input.
// DEPENDS: LokiHostBuilder, TestRenderCommand, ILokiHost, OdfImporter
// USED BY: CI smoke test, developer testing
// PHASE:   3

using AppThere.Loki.Format.Odf;
using AppThere.Loki.LokiKit.Host;
using AppThere.Loki.Tools.LokiPrint;
using AppThere.Loki.Writer;
using AppThere.Loki.Writer.Engine;
using Microsoft.Extensions.DependencyInjection;

if (args.Length < 1 || args[0] != "test-render")
{
    Console.Error.WriteLine(
        "Usage: lokiprint test-render --output <path> [--input <path>] [--tile <col,row>] [--zoom <level>]");
    return 1;
}

var host = new LokiHostBuilder()
    .UseHeadlessSurfaces()
    .UseSkiaRenderer()
    .UseSkiaFonts()
    .UseConsoleLogger()
    .UseWriterEngine()
    .ConfigureServices(services =>
    {
        // Override NullOdfImporter with the real production ODF parser
        var existing = services.FirstOrDefault(d => d.ServiceType == typeof(IOdfImporter));
        if (existing is not null) services.Remove(existing);
        services.AddScoped<IOdfImporter, OdfImporter>();
    })
    .Build();

return await new TestRenderCommand(host).ExecuteAsync(args[1..]);
