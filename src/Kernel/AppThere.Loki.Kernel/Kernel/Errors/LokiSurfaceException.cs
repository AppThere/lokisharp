// LAYER:   AppThere.Loki.Kernel — Kernel
// KIND:    Implementation
// PURPOSE: Thrown when a render surface cannot be created or configured.
//          Wraps platform-specific failures from Skia or GPU APIs.
//          Does not expose any platform type — Kernel stays dependency-free.
// DEPENDS: LokiException
// USED BY: AvaloniaSurfaceFactory
// PHASE:   4
// ADR:     ADR-010

namespace AppThere.Loki.Kernel.Errors;

public sealed class LokiSurfaceException : LokiException
{
    public LokiSurfaceException(string message, Exception? inner = null)
        : base(message, inner) { }
}
