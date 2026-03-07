// LAYER:   AppThere.Loki.Tools.LokiPrint — Host
// KIND:    Implementation
// PURPOSE: Implements the 'test-render' sub-command for lokiprint.
//          Builds a sample scene covering all eight Phase 1 PaintNode types and
//          exports it as a PDF (US Letter) or as a PNG tile (512×512 px).
//          Uses HeadlessSurfaceFactory + PdfRenderSurface / BitmapRenderSurface
//          from the Skia layer. Draws directly with SKCanvas; a proper
//          PaintScene → ILokiPainter renderer is a Phase 2 concern.
//          Does NOT expose any SkiaSharp type outside this file.
// DEPENDS: HeadlessSurfaceFactory, PdfRenderSurface, BitmapRenderSurface,
//          PdfMetadata, SizeF, NullLokiLogger
// USED BY: Program (top-level entry point)
// PHASE:   1

using AppThere.Loki.Kernel.Geometry;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.Skia.Surfaces;
using SkiaSharp;

namespace AppThere.Loki.Tools.LokiPrint;

internal static class TestRenderCommand
{
    private const float PageW    = 612f;   // US Letter width  in points
    private const float PageH    = 792f;   // US Letter height in points
    private const int   TileSize = 512;    // tile pixels (square)

    public static int Run(string[] args)
    {
        string? output  = null;
        bool    hasTile = false;

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--output" && i + 1 < args.Length) output  = args[++i];
            if (args[i] == "--tile"   && i + 1 < args.Length) hasTile = ParseTile(args[++i]);
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
            var ext = Path.GetExtension(output).ToLowerInvariant();
            if      (ext == ".pdf" && !hasTile) RenderPdf(output);
            else if (ext == ".png" &&  hasTile) RenderTilePng(output);
            else
            {
                Console.Error.WriteLine(
                    $"test-render: cannot produce '{ext}' with tile={hasTile}.");
                return 1;
            }

            Console.WriteLine($"test-render: wrote {output}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"test-render: {ex.Message}");
            return 1;
        }
    }

    // ── Output renderers ──────────────────────────────────────────────────────

    private static void RenderPdf(string path)
    {
        var factory = new HeadlessSurfaceFactory(NullLokiLogger.Instance);
        var meta    = new PdfMetadata("LokiPrint Phase 1 Test", "lokiprint", "AppThere Loki");

        using var fs      = File.Create(path);
        using var surface = (PdfRenderSurface)
            factory.CreatePdfSurface(fs, new SizeF(PageW, PageH), meta);

        DrawScene(surface.Canvas, PageW, PageH);
        surface.Close();
    }

    private static void RenderTilePng(string path)
    {
        var factory = new HeadlessSurfaceFactory(NullLokiLogger.Instance);
        using var surface = (BitmapRenderSurface)
            factory.CreateHeadlessBitmapSurface(new SizeF(TileSize, TileSize));

        var bitmap = surface.GetBitmap();
        using (var canvas = new SKCanvas(bitmap))
        {
            DrawScene(canvas, TileSize, TileSize);
            canvas.Flush();
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data  = image.Encode(SKEncodedImageFormat.Png, 100);
        using var fs    = File.Create(path);
        data.SaveTo(fs);
    }

    // ── Scene — one cell per PaintNode type ──────────────────────────────────

    private static void DrawScene(SKCanvas c, float w, float h)
    {
        c.Clear(SKColors.WhiteSmoke);

        const int cols = 4, rows = 2;
        var px = w * 0.03f;
        var py = h * 0.04f;
        var cw = (w - px * (cols + 1)) / cols;
        var ch = (h - py * (rows + 1)) / rows;

        DrawRectNode     (c, Cell(0, 0, cols, cw, ch, px, py));
        DrawRoundRectNode(c, Cell(1, 0, cols, cw, ch, px, py));
        DrawLineNode     (c, Cell(2, 0, cols, cw, ch, px, py));
        DrawPathNode     (c, Cell(3, 0, cols, cw, ch, px, py));
        DrawGlyphRunNode (c, Cell(0, 1, cols, cw, ch, px, py));
        DrawImageNode    (c, Cell(1, 1, cols, cw, ch, px, py));
        DrawShadowNode   (c, Cell(2, 1, cols, cw, ch, px, py));
        DrawGroupNode    (c, Cell(3, 1, cols, cw, ch, px, py));
    }

    private static SKRect Cell(int col, int row, int cols,
                               float cw, float ch, float px, float py) =>
        new(px + col * (cw + px), py + row * (ch + py),
            px + col * (cw + px) + cw, py + row * (ch + py) + ch);

    // ── One painter per PaintNode type ────────────────────────────────────────

    private static void DrawRectNode(SKCanvas c, SKRect b)
    {
        using var f = Fill(0x42, 0x85, 0xF4); c.DrawRect(b, f);
        using var s = Stroke(0x1A, 0x56, 0xC8, 2f); c.DrawRect(b, s);
        Label(c, b, "RectNode");
    }

    private static void DrawRoundRectNode(SKCanvas c, SKRect b)
    {
        using var f = Fill(0x34, 0xA8, 0x53); c.DrawRoundRect(b, 14f, 14f, f);
        Label(c, b, "RoundRectNode");
    }

    private static void DrawLineNode(SKCanvas c, SKRect b)
    {
        using var bg = Fill(0xF0, 0xF4, 0xFF); c.DrawRect(b, bg);
        using var ln = new SKPaint
            { Color = new SKColor(0xEA, 0x43, 0x35), StrokeWidth = 4f,
              IsAntialias = true, IsStroke = true };
        c.DrawLine(b.Left + 10f, b.MidY, b.Right - 10f, b.MidY, ln);
        Label(c, b, "LineNode");
    }

    private static void DrawPathNode(SKCanvas c, SKRect b)
    {
        using var f    = Fill(0xFB, 0xBC, 0x04);
        using var path = new SKPath();
        var r = Math.Min(b.Width, b.Height) * 0.4f;
        path.MoveTo(b.MidX, b.MidY - r);
        for (var i = 1; i < 5; i++)
        {
            var a = Math.PI * 2 * i / 5 - Math.PI / 2;
            path.LineTo(b.MidX + r * (float)Math.Cos(a),
                        b.MidY + r * (float)Math.Sin(a));
        }
        path.Close();
        c.DrawPath(path, f);
        Label(c, b, "PathNode");
    }

    private static void DrawGlyphRunNode(SKCanvas c, SKRect b)
    {
        using var bg = Fill(0xEE, 0xF2, 0xFF); c.DrawRect(b, bg);
        using var tf = SKTypeface.FromFamilyName("sans-serif") ?? SKTypeface.Default;
        using var tx = new SKPaint
            { Color = new SKColor(0x37, 0x37, 0x99), IsAntialias = true,
              Typeface = tf, TextSize = b.Height * 0.28f,
              TextAlign = SKTextAlign.Center };
        c.DrawText("Ag", b.MidX, b.MidY + tx.TextSize * 0.35f, tx);
        Label(c, b, "GlyphRunNode");
    }

    private static void DrawImageNode(SKCanvas c, SKRect b)
    {
        using var sky = Fill(0x87, 0xCE, 0xEB);
        using var gnd = Fill(0x22, 0x8B, 0x22);
        var mid = (b.Top + b.Bottom) * 0.5f;
        c.DrawRect(new SKRect(b.Left, b.Top,  b.Right, mid), sky);
        c.DrawRect(new SKRect(b.Left, mid,    b.Right, b.Bottom), gnd);
        using var bdr = Stroke(0x66, 0x66, 0x66, 1.5f); c.DrawRect(b, bdr);
        Label(c, b, "ImageNode");
    }

    private static void DrawShadowNode(SKCanvas c, SKRect b)
    {
        var inner = new SKRect(b.Left + 14f, b.Top + 14f,
                               b.Right - 14f, b.Bottom - 28f);
        using var sh = new SKPaint
            { Color = new SKColor(0, 0, 0, 80), IsAntialias = true,
              ImageFilter = SKImageFilter.CreateBlur(5f, 5f) };
        c.DrawRect(new SKRect(inner.Left + 5f, inner.Top + 7f,
                              inner.Right + 5f, inner.Bottom + 7f), sh);
        using var wh = Fill(0xFF, 0xFF, 0xFF); c.DrawRect(inner, wh);
        Label(c, b, "ShadowNode");
    }

    private static void DrawGroupNode(SKCanvas c, SKRect b)
    {
        c.Save();
        c.ClipRect(b);
        var r  = Math.Min(b.Width, b.Height) * 0.32f;
        using var p0 = new SKPaint
            { Color = new SKColor(0x42, 0x85, 0xF4, 180), IsAntialias = true };
        using var p1 = new SKPaint
            { Color = new SKColor(0xEA, 0x43, 0x35, 180), IsAntialias = true };
        using var p2 = new SKPaint
            { Color = new SKColor(0x34, 0xA8, 0x53, 180), IsAntialias = true };
        c.DrawOval(new SKRect(b.MidX - r * 1.5f, b.MidY - r,
                              b.MidX + r * 0.5f, b.MidY + r), p0);
        c.DrawOval(new SKRect(b.MidX - r * 0.5f, b.MidY - r,
                              b.MidX + r * 1.5f, b.MidY + r), p1);
        c.DrawOval(new SKRect(b.MidX - r,        b.MidY - r * 0.5f,
                              b.MidX + r,         b.MidY + r * 1.5f), p2);
        c.Restore();
        Label(c, b, "GroupNode");
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    private static void Label(SKCanvas c, SKRect b, string text)
    {
        var fs = Math.Max(7f, b.Height * 0.09f);
        using var bg = new SKPaint { Color = new SKColor(0, 0, 0, 160) };
        using var tx = new SKPaint
            { Color = SKColors.White, IsAntialias = true, TextSize = fs };
        c.DrawRect(new SKRect(b.Left, b.Bottom - fs * 1.6f, b.Right, b.Bottom), bg);
        c.DrawText(text, b.Left + 4f, b.Bottom - fs * 0.4f, tx);
    }

    private static SKPaint Fill(byte r, byte g, byte b) =>
        new() { Color = new SKColor(r, g, b), IsAntialias = true };

    private static SKPaint Stroke(byte r, byte g, byte b, float w) =>
        new() { Color = new SKColor(r, g, b), IsAntialias = true,
                IsStroke = true, StrokeWidth = w };

    private static bool ParseTile(string value)
    {
        var p = value.Split(',');
        return p.Length == 2
            && int.TryParse(p[0], out _)
            && int.TryParse(p[1], out _);
    }
}
