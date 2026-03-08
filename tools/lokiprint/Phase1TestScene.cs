// LAYER:   AppThere.Loki.Tools.LokiPrint — Host
// KIND:    Implementation
// PURPOSE: Builds the Phase 1 exit-criterion PaintScene, exercising all eight
//          PaintNode types (RectNode, RoundRectNode, LineNode, PathNode,
//          GlyphRunNode, ImageNode, ShadowNode, GroupNode) across three A4 bands.
//          Also creates and populates an IImageStore with a 64×64 checkerboard PNG.
//          Build() returns both the immutable scene and the populated store.
//          Does NOT use LokiKit, document parsing, or any Phase 2+ concept.
// DEPENDS: PaintScene, PaintBand, PaintNode, LokiTextShaper, IFontManager,
//          SkiaImageCodec, SkiaImageStore, ImageRef, LokiPath, ILokiLogger
// USED BY: TestRenderCommand
// PHASE:   1

using System.Collections.Immutable;
using AppThere.Loki.Kernel.Color;
using AppThere.Loki.Kernel.Fonts;
using AppThere.Loki.Kernel.Geometry;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.Skia.Fonts;
using AppThere.Loki.Skia.Images;
using AppThere.Loki.Skia.Paths;
using AppThere.Loki.Skia.Scene;
using AppThere.Loki.Skia.Scene.Nodes;
using SkiaSharp;

namespace AppThere.Loki.Tools.LokiPrint;

public static class Phase1TestScene
{
    private const float PageW = 595f;
    private const float PageH = 842f;

    /// <summary>
    /// Builds a three-band A4 scene exercising all eight Phase 1 PaintNode types.
    /// The returned IImageStore is populated with the checkerboard PNG used by
    /// the ImageNode in Band 1. Callers must pass this store to TileRenderer.
    /// </summary>
    /// <param name="externalStore">
    /// Optional existing image store to populate. When non-null the checkerboard
    /// image is registered into it instead of a newly created store. Pass the
    /// caller-owned store (e.g. GoldenTestBase.ImageStore) so the renderer can
    /// resolve images without a separate store hand-off.
    /// </param>
    public static (PaintScene scene, IImageStore imageStore)
        Build(IFontManager fontManager, ILokiLogger logger,
              IImageStore? externalStore = null)
    {
        IImageStore imageStore;
        if (externalStore is not null)
        {
            imageStore = externalStore;
        }
        else
        {
            var codec = new SkiaImageCodec(logger);
            imageStore = new SkiaImageStore(codec, logger);
        }

        var (imageRef, pngBytes) = CreateCheckerboardPng();
        imageStore.Register(imageRef, pngBytes.AsMemory());

        var scene = PaintScene.CreateBuilder(0)
            .WithSize(PageW, PageH)
            .AddBand(BuildBand0())
            .AddBand(BuildBand1(fontManager, logger, imageRef))
            .AddBand(BuildBand2())
            .Build();

        return (scene, imageStore);
    }

    // ── Band 0 — RectNode, RoundRectNode, LineNode, PathNode ─────────────────

    private static PaintBand BuildBand0()
    {
        var rect = new RectNode(
            new RectF(20f, 20f, 120f, 240f),
            PaintStyle.Fill(LokiColor.Blue),
            PaintStyle.Stroke(LokiColor.Black, 2f));

        var roundRect = new RoundRectNode(
            new RectF(165f, 20f, 120f, 240f),
            12f, 12f,
            PaintStyle.Fill(LokiColor.Green));

        var line = new LineNode(
            new PointF(320f, 140f),
            new PointF(430f, 140f),
            new LinePaint(LokiColor.Red, 4f));

        var pathNode = BuildStarPath();

        var nodes = ImmutableArray.Create<PaintNode>(rect, roundRect, line, pathNode);
        return new PaintBand(0f, 280f, nodes, 1L);
    }

    private static PathNode BuildStarPath()
    {
        const float cx = 515f, cy = 140f, outerR = 100f, innerR = 42f;
        var builder = LokiPath.CreateBuilder();

        for (var i = 0; i < 5; i++)
        {
            var outerAngle = MathF.PI * 2f * i / 5f - MathF.PI / 2f;
            var innerAngle = outerAngle + MathF.PI / 5f;
            var outerPt    = new PointF(cx + outerR * MathF.Cos(outerAngle),
                                         cy + outerR * MathF.Sin(outerAngle));
            var innerPt    = new PointF(cx + innerR * MathF.Cos(innerAngle),
                                         cy + innerR * MathF.Sin(innerAngle));
            if (i == 0) builder.MoveTo(outerPt); else builder.LineTo(outerPt);
            builder.LineTo(innerPt);
        }

        builder.Close();
        var lokiPath = builder.Build();
        var yellow   = LokiColor.FromArgb32(255, 251, 188, 4);
        return new PathNode(lokiPath.Bounds, lokiPath, PaintStyle.Fill(yellow));
    }

    // ── Band 1 — GlyphRunNode, ImageNode ─────────────────────────────────────

    private static PaintBand BuildBand1(IFontManager fontManager, ILokiLogger logger,
                                         ImageRef imageRef)
    {
        var shaper = new LokiTextShaper(fontManager, logger);
        var font   = new FontDescriptor("Inter", SizeInPoints: 36f);
        var runs   = shaper.Shape("Hello Loki", font);

        var glyphRunNode = new GlyphRunNode(
            new RectF(30f, 285f, 260f, 50f),
            new PointF(30f, 335f),
            ImmutableArray.CreateRange(runs),
            new TextPaint(LokiColor.Black));

        var imageNode = new ImageNode(
            new RectF(310f, 285f, 255f, 271f),
            imageRef, 1f, ImageFit.Contain);

        var nodes = ImmutableArray.Create<PaintNode>(glyphRunNode, imageNode);
        return new PaintBand(280f, 281f, nodes, 1L);
    }

    // ── Band 2 — ShadowNode, GroupNode ───────────────────────────────────────

    private static PaintBand BuildBand2()
    {
        var content = new RectNode(
            new RectF(30f, 580f, 230f, 200f),
            PaintStyle.Fill(LokiColor.White),
            PaintStyle.Stroke(LokiColor.Black, 1f));

        var shadow = new ShadowNode(
            new RectF(18f, 568f, 254f, 224f),
            content,
            new PointF(8f, 8f),
            10f,
            LokiColor.Black);

        var blue   = LokiColor.FromArgb32(180, 66,  133, 244);
        var red    = LokiColor.FromArgb32(180, 234, 67,  53);
        var child0 = new RectNode(new RectF(355f, 575f, 150f, 150f), PaintStyle.Fill(blue));
        var child1 = new RectNode(new RectF(410f, 630f, 150f, 150f), PaintStyle.Fill(red));

        var group = new GroupNode(
            new RectF(350f, 570f, 220f, 220f),
            ImmutableArray.Create<PaintNode>(child0, child1),
            0.8f);

        var nodes = ImmutableArray.Create<PaintNode>(shadow, group);
        return new PaintBand(561f, 281f, nodes, 1L);
    }

    // ── Checkerboard PNG ──────────────────────────────────────────────────────

    private static (ImageRef imageRef, byte[] pngBytes) CreateCheckerboardPng()
    {
        const int size     = 64;
        const int tileSize = 8;

        using var bmp    = new SKBitmap(size, size);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(SKColors.White);

        using var paint = new SKPaint { IsAntialias = false };

        for (var row = 0; row < size / tileSize; row++)
        for (var col = 0; col < size / tileSize; col++)
        {
            paint.Color = (row + col) % 2 == 0 ? SKColors.Black : SKColors.White;
            canvas.DrawRect(col * tileSize, row * tileSize, tileSize, tileSize, paint);
        }

        canvas.Flush();

        using var skImage = SKImage.FromBitmap(bmp);
        using var encoded = skImage.Encode(SKEncodedImageFormat.Png, 100);
        var bytes    = encoded.ToArray();
        var imageRef = ImageRef.ComputeFrom(bytes.AsSpan(), size, size, "image/png");
        return (imageRef, bytes);
    }
}
