// LAYER:   AppThere.Loki.LokiKit — Hourglass Waist
// KIND:    Exception types
// PURPOSE: Typed exceptions for LokiKit failure modes.
//          All derive from LokiKitException so callers can catch at
//          the right granularity.
// DEPENDS: LokiException (Kernel)
// USED BY: ILokiHost, ILokiDocument, ILokiEngine implementations
// PHASE:   2

using AppThere.Loki.Kernel.Errors;

namespace AppThere.Loki.LokiKit.Errors;

/// <summary>Base for all LokiKit exceptions.</summary>
public abstract class LokiKitException : LokiException
{
    protected LokiKitException(string message, Exception? inner = null)
        : base(message, inner) { }
}

/// <summary>
/// Thrown by ILokiHost.OpenAsync when the stream cannot be parsed
/// or no engine is registered for the detected format.
/// </summary>
public sealed class LokiOpenException : LokiKitException
{
    public LokiOpenException(string message, Exception? inner = null)
        : base(message, inner) { }
}

/// <summary>
/// Thrown by ILokiDocument.SaveAsync on write failure.
/// </summary>
public sealed class LokiSaveException : LokiKitException
{
    public LokiSaveException(string message, Exception? inner = null)
        : base(message, inner) { }
}

/// <summary>
/// Thrown by ILokiDocument.ExecuteAsync when no handler is registered
/// for the command type.
/// </summary>
public sealed class CommandDispatchException : LokiKitException
{
    public Type CommandType { get; }

    public CommandDispatchException(Type commandType)
        : base($"No handler registered for command type '{commandType.Name}'.")
    {
        CommandType = commandType;
    }
}
