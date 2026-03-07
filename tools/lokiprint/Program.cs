// LAYER:   AppThere.Loki.Tools.LokiPrint — Host
// KIND:    Host
// PURPOSE: Entry point for the lokiprint headless CLI.
//          Phase 1 exit criterion: 'lokiprint test-render --output out/test.pdf'
//          produces a valid PDF containing all PaintNode types.
//          Does NOT implement document parsing or LokiKit — those are Phase 2+.
// DEPENDS: (Microsoft.Extensions.Hosting — wired in Program.cs top-level statements)
// USED BY: CI smoke test, developer testing
// PHASE:   1

Console.WriteLine("lokiprint: Phase 1 stub — test-render not yet implemented.");
