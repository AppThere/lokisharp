// LAYER:   AppThere.Loki.Kernel — Kernel
// KIND:    Enum
// PURPOSE: Pixel layout of ImageData. Matches SkiaSharp SKColorType values
//          for the formats used in Phase 1. Order is deliberate — do not reorder.
// DEPENDS: (none)
// USED BY: ImageData, IImageCodec, SkiaImageCodec
// PHASE:   1

namespace AppThere.Loki.Kernel.Images;

public enum PixelFormat
{
    /// <summary>8 bits per channel, alpha premultiplied. Native Skia format.</summary>
    Rgba8888Premul,
    /// <summary>8 bits per channel, straight (non-premultiplied) alpha.</summary>
    Rgba8888Straight,
    /// <summary>8-bit greyscale, no alpha.</summary>
    Gray8,
    /// <summary>8-bit greyscale with 8-bit alpha.</summary>
    GrayAlpha88,
}

public static class PixelFormatExtensions
{
    public static int BytesPerPixel(this PixelFormat fmt) => fmt switch
    {
        PixelFormat.Rgba8888Premul    => 4,
        PixelFormat.Rgba8888Straight  => 4,
        PixelFormat.Gray8             => 1,
        PixelFormat.GrayAlpha88       => 2,
        _ => throw new ArgumentOutOfRangeException(nameof(fmt))
    };
}
