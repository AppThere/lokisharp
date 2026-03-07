// LAYER:   AppThere.Loki.Tools.LokiPrint — Host
// KIND:    Host
// PURPOSE: Entry point for the lokiprint headless CLI.
//          Phase 1 exit criterion: 'lokiprint test-render --output out/test.pdf'
//          produces a valid PDF containing all PaintNode types.
//          Does NOT implement document parsing or LokiKit — those are Phase 2+.
// DEPENDS: TestRenderCommand
// USED BY: CI smoke test, developer testing
// PHASE:   1

using AppThere.Loki.Tools.LokiPrint;

if (args.Length < 1 || args[0] != "test-render")
{
    Console.Error.WriteLine("Usage: lokiprint test-render --output <path> [--tile <col,row>]");
    return 1;
}

return TestRenderCommand.Run(args[1..]);
