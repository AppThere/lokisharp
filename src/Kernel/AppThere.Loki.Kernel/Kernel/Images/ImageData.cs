// LAYER:   AppThere.Loki.Kernel — Kernel
// KIND:    Record
// PURPOSE: Decoded raster image: raw pixel bytes + dimensions + format.
//          Pixel data is immutable after construction.
//          Does NOT hold a GPU texture or SkiaSharp object — those live in Loki.Skia.
// DEPENDS: PixelFormat
// USED BY: IImageCodec, IImageStore
// PHASE:   1

namespace AppThere.Loki.Kernel.Images;

public sealed record ImageData(
    int                  Width,
    int                  Height,
    PixelFormat          Format,
    ReadOnlyMemory<byte> Pixels)
{
    /// <summary>Bytes per row (stride). Assumes tightly packed rows.</summary>
    public int RowBytes => Width * Format.BytesPerPixel();

    /// <summary>Total byte count of the pixel data.</summary>
    public int TotalBytes => Height * RowBytes;
}
