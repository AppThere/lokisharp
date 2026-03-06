// LAYER:   AppThere.Loki.Kernel — Kernel
// KIND:    Implementation
// PURPOSE: IStorage backed by the physical file system.
//          Resolves all paths relative to a root directory.
//          Throws StorageException (never FileNotFoundException) for missing files.
//          Does NOT model metadata, permissions, or directory creation beyond what
//          is needed for OpenWriteAsync.
// DEPENDS: IStorage, StorageException
// USED BY: lokiprint CLI, SkiaImageCodec, Format readers (Phase 3+)
// PHASE:   1

using System.Runtime.CompilerServices;
using AppThere.Loki.Kernel.Errors;

namespace AppThere.Loki.Kernel.Storage;

public sealed class PhysicalStorage : IStorage
{
    private readonly string _root;

    public PhysicalStorage(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root, nameof(root));
        _root = Path.GetFullPath(root);
    }

    private string Resolve(string path) =>
        Path.GetFullPath(Path.Combine(_root, path));

    public Task<Stream> OpenReadAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var full = Resolve(path);
        if (!File.Exists(full))
            throw new StorageException($"File not found: {path}", path);
        return Task.FromResult<Stream>(File.OpenRead(full));
    }

    public Task<Stream> OpenWriteAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var full = Resolve(path);
        var dir  = Path.GetDirectoryName(full)!;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        return Task.FromResult<Stream>(
            File.Open(full, FileMode.Create, FileAccess.Write, FileShare.None));
    }

    public Task<bool> ExistsAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(File.Exists(Resolve(path)));
    }

    public Task DeleteAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var full = Resolve(path);
        if (File.Exists(full))
            File.Delete(full);
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<string> ListAsync(
        string prefix,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var dir = Resolve(prefix);
        if (!Directory.Exists(dir))
            yield break;

        foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            yield return Path.GetRelativePath(_root, file);
            await Task.Yield();
        }
    }
}
