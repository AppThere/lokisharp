// LAYER:   AppThere.Loki.Tools.LokiPrint — Host
// KIND:    Implementation
// PURPOSE: Implements the 'test-render' sub-command for lokiprint.
//          Parses --output, --input, --tile, and --zoom arguments; opens a
//          document via ILokiHost; creates a view; exports to PDF or PNG tile.
//          --input <path>: open an ODF/FODT file; omit to use an empty document.
//          PDF: uses view.RenderToPdfAsync — no casting required.
//          PNG: uses view.RenderTileAsync(TileRequest.ForHeadless(...)).
//          Disposes view and document on completion.
//          Does NOT expose SkiaSharp types outside this file.
// DEPENDS: ILokiHost, ILokiView, ILokiDocument, TileRequest, PdfMetadata, OpenOptions
// USED BY: Program
// PHASE:   3

using AppThere.Loki.LokiKit.Host;
using AppThere.Loki.LokiKit.View;
using AppThere.Loki.Skia.Rendering;
using AppThere.Loki.Skia.Surfaces;
using SkiaSharp;

namespace AppThere.Loki.Tools.LokiPrint;

internal sealed class TestRenderCommand
{
    private readonly ILokiHost _host;

    public TestRenderCommand(ILokiHost host) => _host = host;

    public async Task<int> ExecuteAsync(string[] args)
    {
        string? output  = null;
        string? input   = null;
        float   zoom    = 1f;
        int     col     = 0, row = 0;
        bool    hasTile = false;

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--output" && i + 1 < args.Length)
                output = args[++i];
            else if (args[i] == "--input" && i + 1 < args.Length)
                input = args[++i];
            else if (args[i] == "--zoom" && i + 1 < args.Length
                     && float.TryParse(args[++i], out var z))
                zoom = z;
            else if (args[i] == "--tile" && i + 1 < args.Length)
                hasTile = ParseTile(args[++i], out col, out row);
        }

        if (output is null)
        {
            Console.Error.WriteLine("test-render: --output <path> is required.");
            return 1;
        }

        var dir = Path.GetDirectoryName(Path.GetFullPath(output));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        try
        {
            Stream sourceStream = input is not null
                ? File.OpenRead(input)
                : Stream.Null;

            await using var document = await _host.OpenAsync(
                sourceStream, OpenOptions.Default).ConfigureAwait(false);

            if (input is not null) await sourceStream.DisposeAsync().ConfigureAwait(false);

            using var view = _host.CreateView(document);

            var ext = Path.GetExtension(output).ToLowerInvariant();

            if (ext == ".pdf" && !hasTile)
                await RenderPdfAsync(output, view).ConfigureAwait(false);
            else if (ext == ".png" && hasTile)
                await RenderTilePngAsync(output, view, zoom, col, row).ConfigureAwait(false);
            else
            {
                Console.Error.WriteLine(
                    $"test-render: cannot produce '{ext}' with tile={hasTile}. " +
                    "Use .pdf without --tile, or .png with --tile.");
                return 1;
            }

            Console.WriteLine($"test-render: wrote {output}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"test-render: {ex.Message}");
            if (ex.InnerException is not null)
                Console.Error.WriteLine($"  Caused by: {ex.InnerException.Message}");
            return 1;
        }
    }

    // ── Output renderers ──────────────────────────────────────────────────────

    private static async Task RenderPdfAsync(string path, ILokiView view)
    {
        var meta = new PdfMetadata("LokiPrint Phase 2 Test", "lokiprint", "AppThere Loki");
        await using var fs = File.Create(path);
        await view.RenderToPdfAsync(fs, meta).ConfigureAwait(false);
    }

    private static async Task RenderTilePngAsync(
        string path, ILokiView view, float zoom, int col, int row)
    {
        var request = TileRequest.ForHeadless(0, zoom, col, row);
        using var bitmap = await view.RenderTileAsync(request).ConfigureAwait(false);

        using var skImage = SKImage.FromBitmap(bitmap);
        using var encoded = skImage.Encode(SKEncodedImageFormat.Png, 100);
        await using var fs = File.Create(path);
        encoded.SaveTo(fs);
    }

    // ── Arg parsing ───────────────────────────────────────────────────────────

    private static bool ParseTile(string value, out int col, out int row)
    {
        col = row = 0;
        var parts = value.Split(',');
        return parts.Length == 2
            && int.TryParse(parts[0].Trim(), out col)
            && int.TryParse(parts[1].Trim(), out row);
    }
}
