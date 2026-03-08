// LAYER:   AppThere.Loki.Tools.LokiPrint — Host
// KIND:    Host
// PURPOSE: Entry point for the lokiprint headless CLI.
//          Builds an ILokiHost via LokiHostBuilder and dispatches the
//          'test-render' sub-command to TestRenderCommand.ExecuteAsync().
//          Phase 2 exit criterion: 'lokiprint test-render --output out/phase2.pdf'
//          and 'lokiprint test-render --output out/phase2-tile.png --tile 0,0'
//          both produce output files identical in appearance to Phase 1.
// DEPENDS: LokiHostBuilder, TestRenderCommand, ILokiHost
// USED BY: CI smoke test, developer testing
// PHASE:   2

using AppThere.Loki.LokiKit.Host;
using AppThere.Loki.Tools.LokiPrint;

if (args.Length < 1 || args[0] != "test-render")
{
    Console.Error.WriteLine(
        "Usage: lokiprint test-render --output <path> [--tile <col,row>] [--zoom <level>]");
    return 1;
}

var host = new LokiHostBuilder()
    .UseHeadlessSurfaces()
    .UseSkiaRenderer()
    .UseSkiaFonts()
    .UseConsoleLogger()
    .Build();

return await new TestRenderCommand(host).ExecuteAsync(args[1..]);
