// LAYER:   AppThere.Loki.Kernel — Kernel
// KIND:    Interface
// PURPOSE: Contract for decoding and encoding raster images.
//          The Kernel defines the interface; SkiaImageCodec (in Loki.Skia)
//          provides the implementation. This allows the Kernel to remain
//          free of SkiaSharp dependencies.
// DEPENDS: ImageData, PixelFormat
// USED BY: IImageStore (Skia), Format readers (Phase 3+)
// PHASE:   1

namespace AppThere.Loki.Kernel.Images;

public interface IImageCodec
{
    /// <summary>Supported MIME types this codec can decode.</summary>
    IReadOnlyList<string> SupportedMimeTypes { get; }

    /// <summary>
    /// Attempts to decode the stream to an ImageData.
    /// Returns false if the stream is not a recognised format.
    /// Never throws for malformed input — returns false instead.
    /// </summary>
    bool TryDecode(Stream compressed, out ImageData? image);

    /// <summary>Encodes ImageData to the given MIME type. Throws if unsupported.</summary>
    Task EncodeAsync(ImageData image, Stream output, string mimeType,
                     int quality = 90, CancellationToken ct = default);
}
