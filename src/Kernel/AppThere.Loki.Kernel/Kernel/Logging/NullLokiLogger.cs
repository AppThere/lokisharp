// LAYER:   AppThere.Loki.Kernel — Kernel
// KIND:    Implementation
// PURPOSE: No-op ILokiLogger that silently discards all messages.
//          Used as the safe default when no logger is wired up, eliminating
//          null checks throughout the codebase.
//          Does NOT forward to any backend — use ConsoleLokiLogger (Phase 2+) for output.
// DEPENDS: ILokiLogger
// USED BY: All subsystems that accept ILokiLogger, tests, lokiprint CLI
// PHASE:   1

namespace AppThere.Loki.Kernel.Logging;

public sealed class NullLokiLogger : ILokiLogger
{
    public static readonly NullLokiLogger Instance = new();

    private NullLokiLogger() { }

    public bool IsDebugEnabled => false;

    public void Debug(string message, params object?[] args) { }
    public void Info(string message,  params object?[] args) { }
    public void Warn(string message,  params object?[] args) { }
    public void Error(string message, Exception? exception = null, params object?[] args) { }
}
