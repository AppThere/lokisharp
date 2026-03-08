// LAYER:   AppThere.Loki.Tools.LokiPrint — Host
// KIND:    Implementation
// PURPOSE: ILokiLogger implementation that writes to Console.
//          Info writes to stdout. Warn and Error write to stderr.
//          Debug calls are no-ops (IsDebugEnabled is false).
//          Does NOT depend on MEL or any framework beyond BCL Console.
// DEPENDS: ILokiLogger
// USED BY: Program
// PHASE:   1

using AppThere.Loki.Kernel.Logging;

namespace AppThere.Loki.Tools.LokiPrint;

internal sealed class ConsoleLogger : ILokiLogger
{
    public static readonly ConsoleLogger Instance = new();

    public bool IsDebugEnabled => false;

    public void Debug(string message, params object?[] args) { }

    public void Info(string message, params object?[] args) =>
        Console.WriteLine(string.Format(message, args));

    public void Warn(string message, params object?[] args) =>
        Console.Error.WriteLine("[WARN] " + string.Format(message, args));

    public void Error(string message, Exception? exception = null, params object?[] args)
    {
        Console.Error.WriteLine("[ERROR] " + string.Format(message, args));
        if (exception is not null)
            Console.Error.WriteLine(exception.ToString());
    }
}
