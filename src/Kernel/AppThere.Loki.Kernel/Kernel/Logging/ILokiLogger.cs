// LAYER:   AppThere.Loki.Kernel — Kernel
// KIND:    Interface
// PURPOSE: Minimal structured logging contract for AppThere Loki.
//          Wraps Microsoft.Extensions.Logging.ILogger but keeps it optional —
//          implementations may use any backend (Console, Serilog, NLog, etc.).
//          The no-op implementation (NullLokiLogger) is always available.
// DEPENDS: (none — deliberately avoids MEL reference in this interface)
// USED BY: SkiaFontManager, TileRenderer, lokiprint CLI, all subsystems
// PHASE:   1

namespace AppThere.Loki.Kernel.Logging;

public interface ILokiLogger
{
    void Debug(string message, params object?[] args);
    void Info(string message,  params object?[] args);
    void Warn(string message,  params object?[] args);
    void Error(string message, Exception? exception = null, params object?[] args);

    bool IsDebugEnabled { get; }
}
