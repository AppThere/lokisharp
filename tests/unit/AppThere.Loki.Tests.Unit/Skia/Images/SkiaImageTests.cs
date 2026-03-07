// LAYER:   AppThere.Loki.Tests.Unit — Tests
// KIND:    Tests
// PURPOSE: Specification tests for SkiaImageCodec and SkiaImageStore.
//          Covers decode/encode round-trips, error paths, cache lifecycle,
//          and eviction semantics. Test images are generated programmatically
//          as 4×4 solid-colour bitmaps; no binary files are committed.
// DEPENDS: SkiaImageCodec, SkiaImageStore, IImageCodec, IImageStore, ImageData,
//          ImageRef, PixelFormat, NullLokiLogger, StorageException
// USED BY: CI
// PHASE:   1

using AppThere.Loki.Kernel.Errors;
using AppThere.Loki.Kernel.Images;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.Skia.Images;
using AppThere.Loki.Skia.Scene;
using AppThere.Loki.Tests.Unit.Skia;
using FluentAssertions;
using SkiaSharp;

namespace AppThere.Loki.Tests.Unit.Skia.Images;

public sealed class SkiaImageTests
{
    private readonly SkiaImageCodec _codec;
    private readonly SkiaImageStore _store;

    static SkiaImageTests()
    {
        SkiaTestInitializer.EnsureSkiaSharpLoaded();
    }

    public SkiaImageTests()
    {
        var logger = NullLokiLogger.Instance;
        _codec = new SkiaImageCodec(logger);
        _store = new SkiaImageStore(_codec, logger);
    }

    // ── SkiaImageCodec: TryDecode ─────────────────────────────────────────────

    [Fact]
    public void TryDecode_ValidPngStream_ReturnsTrue()
    {
        using var stream = new MemoryStream(TestFixtures.Png4x4);

        var result = _codec.TryDecode(stream, out _);

        result.Should().BeTrue();
    }

    [Fact]
    public void TryDecode_ValidPngStream_ReturnsCorrectDimensions()
    {
        using var stream = new MemoryStream(TestFixtures.Png4x4);

        _codec.TryDecode(stream, out var image);

        image.Should().NotBeNull();
        image!.Width.Should().Be(4);
        image.Height.Should().Be(4);
    }

    [Fact]
    public void TryDecode_ValidJpegStream_ReturnsTrue()
    {
        using var stream = new MemoryStream(TestFixtures.Jpeg4x4);

        var result = _codec.TryDecode(stream, out _);

        result.Should().BeTrue();
    }

    [Fact]
    public void TryDecode_ValidWebpStream_ReturnsTrue()
    {
        using var stream = new MemoryStream(TestFixtures.Webp4x4);

        var result = _codec.TryDecode(stream, out _);

        result.Should().BeTrue();
    }

    [Fact]
    public void TryDecode_EmptyStream_ReturnsFalse()
    {
        using var stream = new MemoryStream(Array.Empty<byte>());

        var result = _codec.TryDecode(stream, out var image);

        result.Should().BeFalse();
        image.Should().BeNull();
    }

    [Fact]
    public void TryDecode_GarbageBytes_ReturnsFalse()
    {
        var garbage = new byte[] { 0xFF, 0x00, 0xDE, 0xAD, 0xBE, 0xEF, 0x42, 0x13 };
        using var stream = new MemoryStream(garbage);

        var result = _codec.TryDecode(stream, out var image);

        result.Should().BeFalse();
        image.Should().BeNull();
    }

    [Fact]
    public async Task EncodeAsync_Rgba8888Image_WritesPngBytes()
    {
        var pixels = new byte[4 * 4 * 4];
        for (var i = 0; i < 4 * 4; i++)
        {
            pixels[i * 4 + 0] = 255; pixels[i * 4 + 1] = 0;
            pixels[i * 4 + 2] = 0;   pixels[i * 4 + 3] = 255;
        }
        var image  = new ImageData(4, 4, PixelFormat.Rgba8888Premul, new ReadOnlyMemory<byte>(pixels));
        using var output = new MemoryStream();

        await _codec.EncodeAsync(image, output, "image/png");

        var bytes = output.ToArray();
        bytes.Should().HaveCountGreaterThan(8);
        bytes[0].Should().Be(0x89); // PNG magic
        bytes[1].Should().Be(0x50); // 'P'
        bytes[2].Should().Be(0x4E); // 'N'
        bytes[3].Should().Be(0x47); // 'G'
    }

    [Fact]
    public void SupportedMimeTypes_ContainsPngJpegWebp()
    {
        _codec.SupportedMimeTypes.Should()
            .Contain("image/png")
            .And.Contain("image/jpeg")
            .And.Contain("image/webp");
    }

    // ── SkiaImageStore ────────────────────────────────────────────────────────

    [Fact]
    public void Register_NewImage_IncrementsRegisteredCount()
    {
        var (handle, data) = MakePngFixture();

        _store.Register(handle, data);

        _store.RegisteredCount.Should().Be(1);
    }

    [Fact]
    public void Register_SameImageTwice_IsIdempotent()
    {
        var (handle, data) = MakePngFixture();

        _store.Register(handle, data);
        _store.Register(handle, data);

        _store.RegisteredCount.Should().Be(1);
    }

    [Fact]
    public void Decode_RegisteredImage_ReturnsBitmap()
    {
        var (handle, data) = MakePngFixture();
        _store.Register(handle, data);

        var bitmap = _store.Decode(handle);

        bitmap.Should().NotBeNull();
    }

    [Fact]
    public void Decode_RegisteredImage_ReturnsBitmapWithCorrectDimensions()
    {
        var (handle, data) = MakePngFixture();
        _store.Register(handle, data);

        var bitmap = _store.Decode(handle);

        bitmap.Width.Should().Be(4);
        bitmap.Height.Should().Be(4);
    }

    [Fact]
    public void Decode_UnregisteredImage_ThrowsStorageException()
    {
        var handle = new ImageRef("deadbeef", 4, 4, "image/png");

        var act = () => _store.Decode(handle);

        act.Should().Throw<StorageException>();
    }

    [Fact]
    public void TryGetDecoded_BeforeDecode_ReturnsFalse()
    {
        var (handle, data) = MakePngFixture();
        _store.Register(handle, data);

        var result = _store.TryGetDecoded(handle, out var bitmap);

        result.Should().BeFalse();
        bitmap.Should().BeNull();
    }

    [Fact]
    public void TryGetDecoded_AfterDecode_ReturnsTrue()
    {
        var (handle, data) = MakePngFixture();
        _store.Register(handle, data);
        _store.Decode(handle);

        var result = _store.TryGetDecoded(handle, out var bitmap);

        result.Should().BeTrue();
        bitmap.Should().NotBeNull();
    }

    [Fact]
    public void Evict_AfterDecode_TryGetDecodedReturnsFalse()
    {
        var (handle, data) = MakePngFixture();
        _store.Register(handle, data);
        _store.Decode(handle);

        _store.Evict(handle);

        _store.TryGetDecoded(handle, out _).Should().BeFalse();
    }

    [Fact]
    public void Evict_AfterDecode_CompressedDataRetained()
    {
        var (handle, data) = MakePngFixture();
        _store.Register(handle, data);
        _store.Decode(handle);

        _store.Evict(handle);

        // Compressed data still present — decode should succeed without re-registering.
        var bitmap = _store.Decode(handle);
        bitmap.Should().NotBeNull();
    }

    [Fact]
    public void EvictAllDecoded_ClearsDecodedCache()
    {
        var (handle1, data1) = MakePngFixture();
        var (handle2, data2) = MakeJpegFixture();
        _store.Register(handle1, data1);
        _store.Register(handle2, data2);
        _store.Decode(handle1);
        _store.Decode(handle2);

        _store.EvictAllDecoded();

        _store.TryGetDecoded(handle1, out _).Should().BeFalse();
        _store.TryGetDecoded(handle2, out _).Should().BeFalse();
        _store.RegisteredCount.Should().Be(2); // compressed bytes retained
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (ImageRef Handle, ReadOnlyMemory<byte> Data) MakePngFixture()
    {
        var bytes  = TestFixtures.Png4x4;
        var handle = ImageRef.ComputeFrom(bytes, 4, 4, "image/png");
        return (handle, new ReadOnlyMemory<byte>(bytes));
    }

    private static (ImageRef Handle, ReadOnlyMemory<byte> Data) MakeJpegFixture()
    {
        var bytes  = TestFixtures.Jpeg4x4;
        var handle = ImageRef.ComputeFrom(bytes, 4, 4, "image/jpeg");
        return (handle, new ReadOnlyMemory<byte>(bytes));
    }

    // ── Test fixtures ─────────────────────────────────────────────────────────

    private static class TestFixtures
    {
        public static readonly byte[] Png4x4;
        public static readonly byte[] Jpeg4x4;
        public static readonly byte[] Webp4x4;

        static TestFixtures()
        {
            Png4x4  = MakeImage(SKEncodedImageFormat.Png);
            Jpeg4x4 = MakeImage(SKEncodedImageFormat.Jpeg);
            Webp4x4 = MakeImage(SKEncodedImageFormat.Webp);
        }

        private static byte[] MakeImage(SKEncodedImageFormat format)
        {
            using var bmp  = new SKBitmap(4, 4);
            bmp.Erase(SKColors.Crimson);
            using var img  = SKImage.FromBitmap(bmp);
            using var data = img.Encode(format, 90);
            return data.ToArray();
        }
    }
}
