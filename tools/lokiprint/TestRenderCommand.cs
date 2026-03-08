// LAYER:   AppThere.Loki.Tools.LokiPrint — Host
// KIND:    Implementation
// PURPOSE: Implements the 'test-render' sub-command for lokiprint.
//          Parses --output, --tile, and --zoom arguments; builds Phase1TestScene;
//          then exports to PDF (--output *.pdf) or a PNG tile (--output *.png --tile col,row).
//          Creates TileRenderer with the IImageStore returned by Phase1TestScene.Build()
//          so that the ImageNode's checkerboard PNG resolves correctly.
//          Does NOT expose SkiaSharp types outside this file.
// DEPENDS: Phase1TestScene, TileRenderer, HeadlessSurfaceFactory, TileRequest,
//          PdfMetadata, IFontManager, ILokiLogger
// USED BY: Program
// PHASE:   1

using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.Skia.Fonts;
using AppThere.Loki.Skia.Rendering;
using AppThere.Loki.Skia.Surfaces;
using SkiaSharp;

namespace AppThere.Loki.Tools.LokiPrint;

internal static class TestRenderCommand
{
    public static int Run(string[] args, IFontManager fontManager, ILokiLogger logger)
    {
        string? output  = null;
        float   zoom    = 1f;
        int     col     = 0, row = 0;
        bool    hasTile = false;

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--output" && i + 1 < args.Length)
                output = args[++i];
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
            var (scene, imageStore) = Phase1TestScene.Build(fontManager, logger);
            var factory             = new HeadlessSurfaceFactory(logger);
            var renderer            = new TileRenderer(factory, imageStore, logger);

            var ext = Path.GetExtension(output).ToLowerInvariant();

            if (ext == ".pdf" && !hasTile)
                RenderPdf(output, scene, renderer);
            else if (ext == ".png" && hasTile)
                RenderTilePng(output, scene, renderer, zoom, col, row);
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

    private static void RenderPdf(string path, AppThere.Loki.Skia.Scene.PaintScene scene,
                                   TileRenderer renderer)
    {
        var meta = new PdfMetadata("LokiPrint Phase 1 Test", "lokiprint", "AppThere Loki");
        using var fs = File.Create(path);
        renderer.RenderToPdfAsync(new[] { scene }, fs, meta).GetAwaiter().GetResult();
    }

    private static void RenderTilePng(string path, AppThere.Loki.Skia.Scene.PaintScene scene,
                                       TileRenderer renderer, float zoom, int col, int row)
    {
        var request  = TileRequest.ForHeadless(0, zoom, col, row);
        using var bitmap = renderer.RenderTileAsync(scene, request).GetAwaiter().GetResult();

        using var skImage = SKImage.FromBitmap(bitmap);
        using var encoded = skImage.Encode(SKEncodedImageFormat.Png, 100);
        using var fs      = File.Create(path);
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
