// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Record
// PURPOSE: Content-addressed handle to an image. Images are never embedded as
//          raw bytes in PaintNodes — they are referenced by SHA-256 hash.
//          Pixel data lives in IImageStore, keyed by ContentHash.
//          A PaintScene containing images is compact and portable;
//          the IImageStore must be populated before rendering begins.
// DEPENDS: (none)
// USED BY: ImageNode, ILokiPainter.DrawImage, IImageStore
// PHASE:   1
// ADR:     ADR-003

namespace AppThere.Loki.Skia.Scene;

public sealed record ImageRef(
    string ContentHash,  // hex-encoded SHA-256 of compressed source bytes
    int    Width,        // natural width in pixels
    int    Height,       // natural height in pixels
    string MimeType)     // "image/png", "image/jpeg", "image/webp", "image/svg+xml"
{
    public static ImageRef ComputeFrom(ReadOnlySpan<byte> compressedData,
                                        int width, int height, string mimeType)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(compressedData);
        return new(Convert.ToHexString(hash).ToLowerInvariant(), width, height, mimeType);
    }
}
