// LAYER:   AppThere.Loki.LokiKit — Commands
// KIND:    Interface + sealed records (edit command contract)
// PURPOSE: IEditCommand extends ILokiCommand with session identity and
//          document version — the two fields required for CRDT-compatible
//          operation attribution and conflict detection.
//          All Phase 5 edit commands are immutable sealed records here,
//          co-located with ILokiCommand (PingCommand, SetActivePartCommand).
//          Implementation (apply/reverse logic) lives in WriterEngine.
// DEPENDS: ILokiCommand, SessionId, DocumentVersion, CaretPosition
// USED BY: WriterEngine, CommandHistory, ILokiEngine.ExecuteAsync
// PHASE:   5
// ADR:     ADR-012, ADR-013

using AppThere.Loki.LokiKit.Host;
using AppThere.Loki.LokiKit.Document;

namespace AppThere.Loki.LokiKit.Commands;

/// <summary>
/// Marker interface for document-mutating commands.
/// Extends ILokiCommand with attribution and versioning fields.
/// </summary>
public interface IEditCommand : ILokiCommand
{
    /// <summary>Session that originated this command.</summary>
    SessionId OriginatorId { get; }

    /// <summary>
    /// Document version at which this command was generated.
    /// Used for conflict detection in Phase 7 multiplayer.
    /// </summary>
    DocumentVersion AtVersion { get; }
}

// ── Undo / Redo ───────────────────────────────────────────────────────────────
// These are NOT IEditCommands — they operate on CommandHistory,
// not on document content directly.

public sealed record UndoCommand : ILokiCommand;
public sealed record RedoCommand  : ILokiCommand;

// ── Phase 5 edit commands ─────────────────────────────────────────────────────

/// <summary>
/// Insert a string at the given caret position.
/// Committed from PendingInputBuffer at word boundaries.
/// </summary>
public sealed record InsertTextCommand(
    SessionId       OriginatorId,
    DocumentVersion AtVersion,
    CaretPosition   At,
    string          Text)
    : IEditCommand;

/// <summary>
/// Delete Length UTF-16 code units starting at From.
/// DeletedText is stored for undo without re-querying document state.
/// </summary>
public sealed record DeleteTextCommand(
    SessionId       OriginatorId,
    DocumentVersion AtVersion,
    CaretPosition   From,
    int             Length,
    string          DeletedText)
    : IEditCommand;

/// <summary>Split the paragraph at the caret position (Enter key).</summary>
public sealed record SplitParagraphCommand(
    SessionId       OriginatorId,
    DocumentVersion AtVersion,
    CaretPosition   At)
    : IEditCommand;

/// <summary>
/// Merge paragraph at ParagraphIndex with the preceding paragraph
/// (Backspace at paragraph start).
/// </summary>
public sealed record MergeParagraphCommand(
    SessionId       OriginatorId,
    DocumentVersion AtVersion,
    int             ParagraphIndex)
    : IEditCommand;

/// <summary>
/// Apply partial character style overrides to a range.
/// Style is a CharacterStyleDef with only the changed properties set;
/// null properties are not applied (not cleared).
/// </summary>
public sealed record SetCharacterStyleCommand(
    SessionId         OriginatorId,
    DocumentVersion   AtVersion,
    CaretPosition     From,
    CaretPosition     To,
    CharacterStyleDef Style)
    : IEditCommand;
