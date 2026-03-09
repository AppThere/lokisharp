// LAYER:   AppThere.Loki.LokiKit — Host
// KIND:    Records (session identity and host configuration)
// PURPOSE: SessionId uniquely identifies a participant (local or remote)
//          in the caret registry and command history. Supplied via
//          LokiHostBuilder.WithOptions(). Defaults to Guid.NewGuid().
//          LokiHostOptions carries all host-level configuration that
//          spans multiple subsystems (session, undo depth, page gap, etc.)
// DEPENDS: —
// USED BY: LokiHostBuilder, WriterEngine, CaretRegistry, CommandHistory
// PHASE:   5
// ADR:     ADR-012, ADR-013, ADR-014, ADR-015

namespace AppThere.Loki.LokiKit.Host;

/// <summary>
/// Uniquely identifies one participant session.
/// Local editing uses LocalSessionId from LokiHostOptions.
/// Phase 7 multiplayer uses a server-assigned SessionId.
/// </summary>
public readonly record struct SessionId(Guid Value)
{
    public static SessionId NewRandom() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString("N")[..8]; // short form for logs
}

/// <summary>
/// Host-level configuration. Supplied via LokiHostBuilder.WithOptions().
/// All properties have sensible defaults — only override what you need.
/// </summary>
public sealed record LokiHostOptions
{
    /// <summary>
    /// Session ID for the local participant. Defaults to a new random Guid
    /// per app launch. Phase 7 multiplayer overrides with server-assigned ID.
    /// </summary>
    public SessionId LocalSessionId { get; init; } = SessionId.NewRandom();

    /// <summary>
    /// Maximum number of commands retained in undo history per document.
    /// Older commands are evicted when the cap is reached.
    /// </summary>
    public int MaxUndoDepth { get; init; } = 500;

    /// <summary>
    /// Idle timeout in milliseconds before the pending input buffer commits
    /// to undo history. Set to 0 for character-level undo granularity.
    /// </summary>
    public int InputIdleCommitMs { get; init; } = 500;

    /// <summary>
    /// Vertical gap between pages in the continuous scroll canvas, in points.
    /// Set to 0 to disable page gaps (single continuous sheet).
    /// </summary>
    public float PageGapPts { get; init; } = 16f;

    public static LokiHostOptions Default { get; } = new();
}
