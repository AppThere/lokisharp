// LAYER:   AppThere.Loki.Tests.Unit — Tests
// KIND:    Tests
// PURPOSE: Unit tests for PhysicalStorage file-system IStorage implementation.
// DEPENDS: PhysicalStorage, IStorage, StorageException
// USED BY: CI
// PHASE:   1

using AppThere.Loki.Kernel.Errors;
using AppThere.Loki.Kernel.Storage;
using FluentAssertions;

namespace AppThere.Loki.Tests.Unit.Kernel.Storage;

public sealed class PhysicalStorageTests : IDisposable
{
    private readonly string _root;
    private readonly PhysicalStorage _storage;

    public PhysicalStorageTests()
    {
        _root    = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_root);
        _storage = new PhysicalStorage(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public async Task OpenWriteAsync_ThenOpenReadAsync_ReturnsWrittenData()
    {
        var payload = new byte[] { 10, 20, 30 };
        await using (var ws = await _storage.OpenWriteAsync("file.bin"))
            await ws.WriteAsync(payload);

        await using var rs = await _storage.OpenReadAsync("file.bin");
        var buffer = new byte[3];
        await rs.ReadExactlyAsync(buffer);
        buffer.Should().Equal(payload);
    }

    [Fact]
    public async Task OpenReadAsync_NonExistentFile_ThrowsStorageException()
    {
        var act = () => _storage.OpenReadAsync("ghost.txt");
        await act.Should().ThrowAsync<StorageException>();
    }

    [Fact]
    public async Task ExistsAsync_AfterWrite_ReturnsTrue()
    {
        await using (var ws = await _storage.OpenWriteAsync("present.txt"))
            ws.WriteByte(1);
        (await _storage.ExistsAsync("present.txt")).Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NonExistentFile_ReturnsFalse()
    {
        (await _storage.ExistsAsync("absent.txt")).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ExistingFile_RemovesFile()
    {
        await using (var ws = await _storage.OpenWriteAsync("del.txt"))
            ws.WriteByte(1);
        await _storage.DeleteAsync("del.txt");
        (await _storage.ExistsAsync("del.txt")).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentFile_DoesNotThrow()
    {
        var act = async () => await _storage.DeleteAsync("none.txt");
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task OpenWriteAsync_InSubdirectory_CreatesDirectories()
    {
        await using (var ws = await _storage.OpenWriteAsync("sub/dir/file.bin"))
            ws.WriteByte(42);
        (await _storage.ExistsAsync("sub/dir/file.bin")).Should().BeTrue();
    }

    [Fact]
    public async Task ListAsync_AfterWritingFiles_ReturnsRelativePaths()
    {
        await using (var ws = await _storage.OpenWriteAsync("dir/a.txt"))
            ws.WriteByte(1);
        await using (var ws = await _storage.OpenWriteAsync("dir/b.txt"))
            ws.WriteByte(2);

        var results = new List<string>();
        await foreach (var p in _storage.ListAsync("dir"))
            results.Add(p);

        results.Should().HaveCount(2);
        results.Should().Contain(p => p.EndsWith("a.txt"));
        results.Should().Contain(p => p.EndsWith("b.txt"));
    }

    [Fact]
    public async Task ListAsync_EmptyDirectory_YieldsNothing()
    {
        Directory.CreateDirectory(Path.Combine(_root, "empty"));
        var results = new List<string>();
        await foreach (var p in _storage.ListAsync("empty"))
            results.Add(p);
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task ListAsync_NonExistentDirectory_YieldsNothing()
    {
        var results = new List<string>();
        await foreach (var p in _storage.ListAsync("noexist"))
            results.Add(p);
        results.Should().BeEmpty();
    }
}
