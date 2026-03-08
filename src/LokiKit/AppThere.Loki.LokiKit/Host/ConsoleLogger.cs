// LAYER:   AppThere.Loki.LokiKit — Hourglass Waist
// KIND:    Implementation
// PURPOSE: ILokiLogger implementation that writes to Console.Out (Info/Debug)
//          and Console.Error (Warn/Error). Activated by LokiHostBuilder.UseConsoleLogger().
//          IsDebugEnabled reads the LOKI_DEBUG environment variable at call time;
//          no caching, so the variable can be set/cleared at runtime in tests.
//          Thread-safe: Console writes are serialised by .NET's TextWriter lock.
//          Does NOT depend on MEL or any framework beyond BCL Console.
// DEPENDS: ILokiLogger
// USED BY: LokiHostBuilder.UseConsoleLogger(), LokiKit integration tests
// PHASE:   2

using AppThere.Loki.Kernel.Logging;

namespace AppThere.Loki.LokiKit.Host;

public sealed class ConsoleLogger : ILokiLogger
{
    public bool IsDebugEnabled =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LOKI_DEBUG"));

    public void Debug(string message, params object?[] args)
    {
        if (!IsDebugEnabled) return;
        Console.Out.WriteLine("[DEBUG] " + string.Format(message, args));
    }

    public void Info(string message, params object?[] args) =>
        Console.Out.WriteLine("[INFO] " + string.Format(message, args));

    public void Warn(string message, params object?[] args) =>
        Console.Error.WriteLine("[WARN] " + string.Format(message, args));

    public void Error(string message, Exception? exception = null, params object?[] args)
    {
        Console.Error.WriteLine("[ERROR] " + string.Format(message, args));
        if (exception is not null)
            Console.Error.WriteLine(exception.ToString());
    }
}
