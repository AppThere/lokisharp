// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Interface
// PURPOSE: Cache of decoded image bitmaps, keyed by ImageRef.ContentHash.
//          Distinct from IImageCodec: the codec decodes bytes; the store caches.
//          Supports eviction under memory pressure (Android/iOS onTrimMemory).
//          Thread-safety: callers must synchronise access (ConcurrentDictionary inside).
// DEPENDS: ImageRef, IImageCodec
// USED BY: LokiSkiaPainter (looks up bitmaps for ImageNode rendering), TileRenderer
// PHASE:   1
// ADR:     ADR-003

using AppThere.Loki.Skia.Scene;
using SkiaSharp;

namespace AppThere.Loki.Skia.Images;

public interface IImageStore
{
    /// <summary>
    /// Registers compressed image data for a handle.
    /// Idempotent: registering the same ContentHash twice is a no-op.
    /// </summary>
    void Register(ImageRef handle, ReadOnlyMemory<byte> compressedData);

    /// <summary>
    /// Returns a decoded SKBitmap for the handle, decoding and caching if needed.
    /// Throws StorageException if the handle was never registered.
    /// </summary>
    SKBitmap Decode(ImageRef handle);

    /// <summary>Returns a cached bitmap without decoding. Returns false if not cached.</summary>
    bool TryGetDecoded(ImageRef handle, out SKBitmap? bitmap);

    /// <summary>Evicts the decoded bitmap for one handle, freeing memory.</summary>
    void Evict(ImageRef handle);

    /// <summary>Evicts all decoded bitmaps. Compressed source data is retained.</summary>
    void EvictAllDecoded();

    /// <summary>Total count of registered images (decoded or not).</summary>
    int RegisteredCount { get; }
}
