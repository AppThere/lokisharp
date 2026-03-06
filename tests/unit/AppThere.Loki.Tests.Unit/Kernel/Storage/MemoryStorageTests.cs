// LAYER:   AppThere.Loki.Tests.Unit — Tests
// KIND:    Tests
// PURPOSE: Unit tests for MemoryStorage in-memory IStorage implementation.
// DEPENDS: MemoryStorage, IStorage, StorageException
// USED BY: CI
// PHASE:   1

using AppThere.Loki.Kernel.Errors;
using AppThere.Loki.Kernel.Storage;
using FluentAssertions;

namespace AppThere.Loki.Tests.Unit.Kernel.Storage;

public sealed class MemoryStorageTests
{
    private static MemoryStorage Create() => new();

    [Fact]
    public async Task OpenWriteAsync_ThenOpenReadAsync_ReturnsWrittenData()
    {
        var storage = Create();
        var payload = new byte[] { 1, 2, 3, 4, 5 };

        await using (var ws = await storage.OpenWriteAsync("file.bin"))
            await ws.WriteAsync(payload);

        await using var rs = await storage.OpenReadAsync("file.bin");
        var read = new byte[5];
        await rs.ReadExactlyAsync(read);
        read.Should().Equal(payload);
    }

    [Fact]
    public async Task OpenReadAsync_NonExistentPath_ThrowsStorageException()
    {
        var storage = Create();
        var act     = () => storage.OpenReadAsync("missing.txt");
        await act.Should().ThrowAsync<StorageException>();
    }

    [Fact]
    public async Task ExistsAsync_AfterWrite_ReturnsTrue()
    {
        var storage = Create();
        await using (var ws = await storage.OpenWriteAsync("x"))
            ws.WriteByte(42);
        (await storage.ExistsAsync("x")).Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NonExistentPath_ReturnsFalse()
    {
        (await Create().ExistsAsync("nope")).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ExistingPath_RemovesFile()
    {
        var storage = Create();
        await using (var ws = await storage.OpenWriteAsync("del"))
            ws.WriteByte(1);
        await storage.DeleteAsync("del");
        (await storage.ExistsAsync("del")).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentPath_DoesNotThrow()
    {
        var act = async () => await Create().DeleteAsync("ghost");
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ListAsync_WithMatchingPrefix_ReturnsMatchingKeys()
    {
        var storage = Create();
        await using (var ws = await storage.OpenWriteAsync("docs/a.txt"))
            ws.WriteByte(1);
        await using (var ws = await storage.OpenWriteAsync("docs/b.txt"))
            ws.WriteByte(2);
        await using (var ws = await storage.OpenWriteAsync("images/c.png"))
            ws.WriteByte(3);

        var results = new List<string>();
        await foreach (var path in storage.ListAsync("docs/"))
            results.Add(path);

        results.Should().HaveCount(2);
        results.Should().Contain("docs/a.txt");
        results.Should().Contain("docs/b.txt");
    }

    [Fact]
    public async Task OpenWriteAsync_OverwritesExistingContent()
    {
        var storage = Create();
        await using (var ws = await storage.OpenWriteAsync("f"))
            ws.WriteByte(99);
        await using (var ws = await storage.OpenWriteAsync("f"))
            ws.WriteByte(7);

        await using var rs  = await storage.OpenReadAsync("f");
        var result = rs.ReadByte();
        result.Should().Be(7);
    }

    [Fact]
    public async Task OpenReadAsync_ReturnedStream_IsReadOnly()
    {
        var storage = Create();
        await using (var ws = await storage.OpenWriteAsync("ro"))
            ws.WriteByte(1);
        await using var rs = await storage.OpenReadAsync("ro");
        rs.CanRead.Should().BeTrue();
    }
}
