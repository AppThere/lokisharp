// LAYER:   AppThere.Loki.Kernel — Kernel
// KIND:    Implementation
// PURPOSE: Base class for all domain exceptions in AppThere Loki.
//          Never thrown directly — always throw a typed subclass.
//          See ADR-004 for the full three-tier error handling strategy.
// DEPENDS: (none)
// USED BY: All subsystems — caught at top-level boundaries in lokiprint and UI
// PHASE:   1
// ADR:     ADR-004

namespace AppThere.Loki.Kernel.Errors;

public abstract class LokiException : Exception
{
    protected LokiException(string message, Exception? inner = null)
        : base(message, inner) { }
}

// ── Kernel exceptions ──────────────────────────────────────────────────────

public sealed class StorageException : LokiException
{
    public string? Path { get; }
    public StorageException(string message, string? path = null, Exception? inner = null)
        : base(message, inner) => Path = path;
}

public sealed class FontLoadException : LokiException
{
    public string FamilyName { get; }
    public FontLoadException(string familyName, string message, Exception? inner = null)
        : base(message, inner) => FamilyName = familyName;
}

public sealed class FontResolutionException : LokiException
{
    public string FamilyName { get; }
    public FontResolutionException(string familyName, string message)
        : base(message) => FamilyName = familyName;
}
