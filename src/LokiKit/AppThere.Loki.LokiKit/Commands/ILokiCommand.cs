// LAYER:   AppThere.Loki.LokiKit — Hourglass Waist
// KIND:    Interface + value types
// PURPOSE: Marker interface for all document commands. Commands are immutable
//          value types dispatched through ILokiDocument.ExecuteAsync.
//          Phase 2: only stub commands are defined. Real commands arrive
//          with their respective engines in Phase 3+.
//          The command pattern is established now so the undo stack and
//          CRDT layer (Phase 5) require no infrastructure changes.
// DEPENDS: —
// USED BY: ILokiDocument, ICommandDispatcher, lokiprint CLI, tests
// PHASE:   2
// ADR:     ADR-005

namespace AppThere.Loki.LokiKit.Commands;

/// <summary>
/// Marker interface for all document commands.
/// Implement as a sealed record for value semantics and pattern matching.
/// </summary>
public interface ILokiCommand { }

// ── View commands (safe for all document kinds) ───────────────────────────────

/// <summary>Set the active part (page/sheet/slide) in all views.</summary>
public sealed record SetActivePartCommand(int PartIndex) : ILokiCommand;

/// <summary>
/// No-op command used in tests to verify the command pipeline is wired.
/// ExecuteAsync must complete without throwing.
/// </summary>
public sealed record PingCommand : ILokiCommand;
