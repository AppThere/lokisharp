// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Implementation
// PURPOSE: Implements IImageCodec using SkiaSharp. Decodes PNG, JPEG, and WebP
//          streams into Rgba8888Premul ImageData. Encodes ImageData back to the
//          requested MIME type. Never throws for malformed input during decode —
//          returns false instead. Does NOT handle SVG or other vector formats.
// DEPENDS: IImageCodec, ILokiLogger, ImageData, PixelFormat
// USED BY: SkiaImageStore — injected via DI
// PHASE:   1

using System.Collections.Immutable;
using System.Runtime.InteropServices;
using AppThere.Loki.Kernel.Images;
using AppThere.Loki.Kernel.Logging;
using SkiaSharp;

namespace AppThere.Loki.Skia.Images;

public sealed class SkiaImageCodec : IImageCodec
{
    private readonly ILokiLogger _logger;

    private static readonly IReadOnlyList<string> _supportedMimeTypes =
        ImmutableArray.Create("image/png", "image/jpeg", "image/webp");

    public SkiaImageCodec(ILokiLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<string> SupportedMimeTypes => _supportedMimeTypes;

    public bool TryDecode(Stream compressed, out ImageData? image)
    {
        image = null;
        try
        {
            using var decoded = SKBitmap.Decode(compressed);
            if (decoded == null)
            {
                _logger.Warn("SKBitmap.Decode returned null — unrecognised format or empty stream.");
                return false;
            }

            var info = new SKImageInfo(decoded.Width, decoded.Height,
                SKColorType.Rgba8888, SKAlphaType.Premul);
            using var converted = new SKBitmap(info);
            using (var canvas = new SKCanvas(converted))
            {
                canvas.Clear(SKColors.Transparent);
                canvas.DrawBitmap(decoded, 0, 0);
            }

            var pixels = converted.GetPixelSpan().ToArray();
            image = new ImageData(decoded.Width, decoded.Height,
                PixelFormat.Rgba8888Premul, new ReadOnlyMemory<byte>(pixels));
            return true;
        }
        catch (Exception ex)
        {
            _logger.Warn("TryDecode failed: {0}", ex.Message);
            return false;
        }
    }

    public async Task EncodeAsync(ImageData image, Stream output, string mimeType,
                                  int quality = 90, CancellationToken ct = default)
    {
        var format = mimeType switch
        {
            "image/png"  => SKEncodedImageFormat.Png,
            "image/jpeg" => SKEncodedImageFormat.Jpeg,
            "image/webp" => SKEncodedImageFormat.Webp,
            _ => throw new NotSupportedException($"MIME type '{mimeType}' is not supported by SkiaImageCodec.")
        };

        byte[] encodedBytes;
        var pixelArray = image.Pixels.ToArray();
        var gcHandle = GCHandle.Alloc(pixelArray, GCHandleType.Pinned);
        try
        {
            var info = new SKImageInfo(image.Width, image.Height,
                SKColorType.Rgba8888, SKAlphaType.Premul);
            using var bmp = new SKBitmap();
            bmp.InstallPixels(info, gcHandle.AddrOfPinnedObject(), image.RowBytes);
            using var skImage = SKImage.FromBitmap(bmp);
            using var encoded = skImage.Encode(format, quality)
                ?? throw new InvalidOperationException($"SKImage.Encode returned null for '{mimeType}'.");
            encodedBytes = encoded.ToArray();
        }
        finally
        {
            gcHandle.Free();
        }

        await output.WriteAsync(encodedBytes, ct).ConfigureAwait(false);
    }
}
