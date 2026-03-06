// LAYER:   AppThere.Loki.Kernel — Kernel
// KIND:    Interface
// PURPOSE: Abstraction over a readable/writable byte store (file system, memory,
//          cloud). Used by format readers/writers and the image codec.
//          Does NOT model directories or file metadata — only stream access.
// DEPENDS: (none)
// USED BY: IImageCodec, lokiprint CLI, Format layer (Phase 3+)
// PHASE:   1

namespace AppThere.Loki.Kernel.Storage;

public interface IStorage
{
    /// <summary>Opens a stream for reading. Throws StorageException if not found.</summary>
    Task<Stream> OpenReadAsync(string path, CancellationToken ct = default);

    /// <summary>Opens a stream for writing. Creates or overwrites.</summary>
    Task<Stream> OpenWriteAsync(string path, CancellationToken ct = default);

    /// <summary>Returns true if the path exists and is readable.</summary>
    Task<bool> ExistsAsync(string path, CancellationToken ct = default);

    /// <summary>Deletes the item at path. No-op if it does not exist.</summary>
    Task DeleteAsync(string path, CancellationToken ct = default);

    /// <summary>Lists paths with the given prefix/directory.</summary>
    IAsyncEnumerable<string> ListAsync(string prefix, CancellationToken ct = default);
}
