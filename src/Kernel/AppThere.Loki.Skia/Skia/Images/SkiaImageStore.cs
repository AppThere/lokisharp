// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Implementation
// PURPOSE: Implements IImageStore. Holds compressed source bytes permanently (keyed
//          by ContentHash) and caches decoded SKBitmaps evictably. Uses Lazy<SKBitmap>
//          in a ConcurrentDictionary to prevent redundant concurrent decodes.
//          Compressed data survives eviction so callers can re-decode at any time.
//          Does NOT decode eagerly — decoding is deferred to the first Decode() call.
// DEPENDS: IImageStore, IImageCodec, ILokiLogger, ImageRef, StorageException, SKBitmap
// USED BY: LokiSkiaPainter, TileRenderer — injected via DI
// PHASE:   1
// ADR:     ADR-003

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using AppThere.Loki.Kernel.Errors;
using AppThere.Loki.Kernel.Images;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.Skia.Scene;
using SkiaSharp;

namespace AppThere.Loki.Skia.Images;

public sealed class SkiaImageStore : IImageStore
{
    private readonly IImageCodec _codec;
    private readonly ILokiLogger _logger;

    private readonly ConcurrentDictionary<string, ReadOnlyMemory<byte>> _compressed = new();
    private readonly ConcurrentDictionary<string, Lazy<SKBitmap>>       _decoded    = new();

    public SkiaImageStore(IImageCodec codec, ILokiLogger logger)
    {
        _codec  = codec  ?? throw new ArgumentNullException(nameof(codec));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public int RegisteredCount => _compressed.Count;

    public void Register(ImageRef handle, ReadOnlyMemory<byte> compressedData)
    {
        _compressed.TryAdd(handle.ContentHash, compressedData);
    }

    public SKBitmap Decode(ImageRef handle)
    {
        if (!_compressed.TryGetValue(handle.ContentHash, out var data))
            throw new StorageException(
                $"Image '{handle.ContentHash}' has not been registered.",
                handle.ContentHash);

        var lazy = _decoded.GetOrAdd(handle.ContentHash,
            key => new Lazy<SKBitmap>(() => DecodeInternal(key, data)));

        return lazy.Value;
    }

    public bool TryGetDecoded(ImageRef handle, out SKBitmap? bitmap)
    {
        if (_decoded.TryGetValue(handle.ContentHash, out var lazy) && lazy.IsValueCreated)
        {
            bitmap = lazy.Value;
            return true;
        }

        bitmap = null;
        return false;
    }

    public void Evict(ImageRef handle)
    {
        if (_decoded.TryRemove(handle.ContentHash, out var lazy) && lazy.IsValueCreated)
            lazy.Value.Dispose();
    }

    public void EvictAllDecoded()
    {
        foreach (var key in _decoded.Keys.ToArray())
        {
            if (_decoded.TryRemove(key, out var lazy) && lazy.IsValueCreated)
                lazy.Value.Dispose();
        }
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private SKBitmap DecodeInternal(string hash, ReadOnlyMemory<byte> data)
    {
        using var stream = new MemoryStream(data.ToArray());

        if (!_codec.TryDecode(stream, out var imageData) || imageData == null)
            throw new StorageException($"Failed to decode image '{hash}'.", hash);

        var pixelArray = imageData.Pixels.ToArray();
        var gcHandle   = GCHandle.Alloc(pixelArray, GCHandleType.Pinned);

        var info = new SKImageInfo(imageData.Width, imageData.Height,
            SKColorType.Rgba8888, SKAlphaType.Premul);
        var bmp = new SKBitmap();
        try
        {
            bmp.InstallPixels(info, gcHandle.AddrOfPinnedObject(), imageData.RowBytes,
                (_, ctx) => ((GCHandle)ctx!).Free(), gcHandle);
        }
        catch
        {
            gcHandle.Free();
            bmp.Dispose();
            throw;
        }

        _logger.Debug("Decoded image '{0}' ({1}×{2}).", hash[..8], imageData.Width, imageData.Height);
        return bmp;
    }
}
