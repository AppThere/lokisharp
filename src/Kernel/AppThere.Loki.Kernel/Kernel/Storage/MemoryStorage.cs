// LAYER:   AppThere.Loki.Kernel — Kernel
// KIND:    Implementation
// PURPOSE: IStorage backed by an in-memory ConcurrentDictionary.
//          Thread-safe. Data is not persisted across instances.
//          Used in tests and headless rendering pipelines where file I/O is unwanted.
//          Does NOT model directories — all keys are flat strings.
// DEPENDS: IStorage, StorageException
// USED BY: Tests, SkiaImageCodec tests, lokiprint CLI (test mode)
// PHASE:   1

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using AppThere.Loki.Kernel.Errors;

namespace AppThere.Loki.Kernel.Storage;

public sealed class MemoryStorage : IStorage
{
    private readonly ConcurrentDictionary<string, byte[]> _store = new();

    public Task<Stream> OpenReadAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!_store.TryGetValue(path, out var data))
            throw new StorageException($"Not found: {path}", path);
        return Task.FromResult<Stream>(new MemoryStream(data, writable: false));
    }

    public Task<Stream> OpenWriteAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult<Stream>(new CapturingStream(path, _store));
    }

    public Task<bool> ExistsAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_store.ContainsKey(path));
    }

    public Task DeleteAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _store.TryRemove(path, out _);
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<string> ListAsync(
        string prefix,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var key in _store.Keys)
        {
            ct.ThrowIfCancellationRequested();
            if (key.StartsWith(prefix, StringComparison.Ordinal))
            {
                yield return key;
                await Task.Yield();
            }
        }
    }

    private sealed class CapturingStream : MemoryStream
    {
        private readonly string _key;
        private readonly ConcurrentDictionary<string, byte[]> _store;

        public CapturingStream(string key, ConcurrentDictionary<string, byte[]> store)
        {
            _key   = key;
            _store = store;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _store[_key] = ToArray();
            base.Dispose(disposing);
        }
    }
}
