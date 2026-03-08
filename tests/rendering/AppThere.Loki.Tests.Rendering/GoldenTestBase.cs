// LAYER:   AppThere.Loki.Tests.Rendering — Tests
// KIND:    Tests
// PURPOSE: Abstract base class for golden PNG rendering tests.
//          Owns and initialises the full Phase 1 DI stack:
//            HeadlessSurfaceFactory, SkiaFontManager, SkiaImageCodec,
//            SkiaImageStore, TileRenderer.
//          RenderTile renders one tile using TileRequest.ForHeadless.
//          AssertMatchesGolden: if the golden PNG does not exist, saves actual
//          and fails with "Golden did not exist — created. Re-run to verify."
//          If it exists, compares pixel-by-pixel with ±3 tolerance per channel.
//          On mismatch saves actual to TestResults/ and fails with diff stats.
//          Tolerance rationale: ±3 per channel accommodates sub-pixel rounding
//          differences in FreeType/font rendering across Linux kernel versions
//          while catching genuine rendering regressions.
// DEPENDS: ITileRenderer, TileRenderer, TileRequest, HeadlessSurfaceFactory,
//          SkiaFontManager, SkiaImageCodec, SkiaImageStore,
//          NullLokiLogger, IFontManager, IImageStore, PaintScene
// USED BY: Phase1SceneGoldenTests
// PHASE:   1

using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.Skia.Fonts;
using AppThere.Loki.Skia.Images;
using AppThere.Loki.Skia.Rendering;
using AppThere.Loki.Skia.Scene;
using AppThere.Loki.Skia.Surfaces;
using SkiaSharp;
using System.Runtime.InteropServices;
using Xunit;

namespace AppThere.Loki.Tests.Rendering;

public abstract class GoldenTestBase
{
    private const int PixelTolerance = 3;

    private static readonly string GoldensDir =
        Path.Combine(AppContext.BaseDirectory, "Goldens");

    private static readonly string ResultsDir =
        Path.Combine(AppContext.BaseDirectory, "TestResults");

    protected readonly ILokiLogger  Logger;
    protected readonly IFontManager FontManager;
    protected readonly IImageStore  ImageStore;
    protected readonly ITileRenderer Renderer;

    protected GoldenTestBase()
    {
        Logger      = NullLokiLogger.Instance;
        var codec   = new SkiaImageCodec(Logger);
        ImageStore  = new SkiaImageStore(codec, Logger);
        FontManager = new SkiaFontManager(Logger);
        var factory = new HeadlessSurfaceFactory(Logger);
        Renderer    = new TileRenderer(factory, ImageStore, Logger);
    }

    // ── Render helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Builds the full DI stack and renders one tile from <paramref name="scene"/>.
    /// Uses <see cref="TileRequest.ForHeadless"/> at the requested zoom level.
    /// The returned <see cref="SKBitmap"/> is owned by the caller.
    /// </summary>
    protected SKBitmap RenderTile(PaintScene scene, int col, int row, float zoom = 1f)
    {
        var request = TileRequest.ForHeadless(scene.PartIndex, zoom, col, row);
        return Renderer.RenderTileAsync(scene, request).GetAwaiter().GetResult();
    }

    // ── Golden assertion ──────────────────────────────────────────────────────

    /// <summary>
    /// Loads the golden PNG from the <c>Goldens/</c> folder (copied to output directory).
    /// If the golden does not exist: saves <paramref name="actual"/> as the new golden
    /// and fails with "Golden did not exist — created. Re-run to verify."
    /// If it exists: compares pixel-by-pixel with tolerance ±3 per channel.
    /// On mismatch: saves <paramref name="actual"/> to <c>TestResults/</c> and fails
    /// with the number of differing pixels and their percentage of the total.
    /// </summary>
    protected void AssertMatchesGolden(SKBitmap actual, string goldenName)
    {
        // Golden PNGs are generated on Linux (FreeType rasteriser).
        // CoreText (macOS) and DirectWrite (Windows) produce sub-pixel differences
        // that cause false failures. Only perform the pixel comparison on Linux so
        // CI is the authoritative golden checker; the full render path still runs.
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Golden PNGs are Linux-only baselines. Pixel comparison skipped on
            // macOS/Windows — rendering still exercised above.
            Console.WriteLine(
                $"[golden skip] {goldenName} — not Linux ({RuntimeInformation.OSDescription})");
            return;
        }

        Directory.CreateDirectory(GoldensDir);
        var goldenPath = Path.Combine(GoldensDir, goldenName);

        if (!File.Exists(goldenPath))
        {
            SaveBitmapToFile(actual, goldenPath);
            Assert.Fail(
                $"Golden '{goldenName}' did not exist — created. Re-run to verify.");
            return; // unreachable — Assert.Fail throws
        }

        using var golden   = LoadBitmapFromFile(goldenPath);
        var       diffCount = CountDifferingPixels(actual, golden);

        if (diffCount > 0)
        {
            var actualName = goldenName.Replace(".png", "_actual.png",
                StringComparison.OrdinalIgnoreCase);
            SaveActual(actual, actualName);
            var total = actual.Width * actual.Height;
            var pct   = diffCount * 100.0 / total;
            Assert.Fail(
                $"Golden '{goldenName}' mismatch: {diffCount} pixel(s) differ " +
                $"({pct:F2}% of {total}) with tolerance ±{PixelTolerance}. " +
                $"Actual saved to TestResults/{actualName}.");
        }
    }

    /// <summary>Writes <paramref name="bitmap"/> as PNG to <c>TestResults/</c>.</summary>
    protected void SaveActual(SKBitmap bitmap, string name)
    {
        Directory.CreateDirectory(ResultsDir);
        SaveBitmapToFile(bitmap, Path.Combine(ResultsDir, name));
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private int CountDifferingPixels(SKBitmap actual, SKBitmap expected)
    {
        if (actual.Width != expected.Width || actual.Height != expected.Height)
            Assert.Fail(
                $"Bitmap size mismatch: actual {actual.Width}×{actual.Height} " +
                $"vs expected {expected.Width}×{expected.Height}.");

        var count = 0;
        for (var y = 0; y < actual.Height; y++)
        for (var x = 0; x < actual.Width;  x++)
        {
            var a = actual.GetPixel(x, y);
            var e = expected.GetPixel(x, y);
            if (Math.Abs(a.Red   - e.Red)   > PixelTolerance ||
                Math.Abs(a.Green - e.Green) > PixelTolerance ||
                Math.Abs(a.Blue  - e.Blue)  > PixelTolerance)
                count++;
        }
        return count;
    }

    private static SKBitmap LoadBitmapFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return SKBitmap.Decode(stream)
               ?? throw new InvalidOperationException(
                   $"Failed to decode golden PNG: {path}");
    }

    private static void SaveBitmapToFile(SKBitmap bitmap, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var image   = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream  = File.OpenWrite(path);
        encoded.SaveTo(stream);
    }
}
