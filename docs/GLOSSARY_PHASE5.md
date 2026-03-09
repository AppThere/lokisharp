# LokiKit Glossary — Phase 5 Additions

These terms extend GLOSSARY_PHASE4.md.

## SessionId
readonly record struct(Guid Value). Identifies one participant session
(local or remote). Supplied via LokiHostOptions.LocalSessionId.
Defaults to Guid.NewGuid() per app launch.
Phase 7 multiplayer: server assigns a stable SessionId.

## LokiHostOptions
Configuration record on LokiHostBuilder. Properties:
  LocalSessionId   — Guid identifying the local participant.
  MaxUndoDepth     — maximum commands in undo history (default 500).
  InputIdleCommitMs — idle timeout before pending buffer commits (default 500ms).
  PageGapPts       — inter-page gap in the continuous scroll canvas (default 16pt).

## DocumentVersion
readonly record struct(int Value). Monotonically increasing integer
incremented by WriterEngine on each committed command. Stored on
LokiDocument.LayoutVersion (renamed/aliased). Phase 7: replaced by
VectorClock without changing command fields.

## CaretPosition
Logical insertion point: (ParagraphIndex, RunIndex, CharOffset, IsAtLineEnd).
Indexes into LokiDocument.Body → ParagraphNode.Inlines → RunNode.Text.
IsAtLineEnd disambiguates soft-wrapped line boundary visual placement.

## Selection
Anchor + Focus pair of CaretPositions. Collapsed when Anchor == Focus
(caret only). Either direction valid (Focus < Anchor = backwards selection).

## CaretEntry
One participant's caret snapshot: (SessionId, Selection, LastActivity,
DisplayName, Color). Immutable — CaretRegistry.Set produces a new entry.

## CaretRegistry
Internal sealed class in WriterEngine. Dictionary<SessionId, CaretEntry>.
ICaretRegistry is the read-only interface exposed for rendering.
CaretChanged event fires after Set/Remove.

## ICaretRegistry
Read-only interface over CaretRegistry. GetAll() → snapshot of all entries.
Get(SessionId) → single entry or null. CaretChanged event.
Exposed via ILokiEngine.GetCarets() for tile renderer.

## IEditCommand
Extends ILokiCommand with OriginatorId (SessionId) and AtVersion
(DocumentVersion). All document-mutating commands implement this.
Phase 5 commands: InsertText, DeleteText, SplitParagraph,
MergeParagraph, SetCharacterStyle.

## UndoCommand / RedoCommand
ILokiCommand (not IEditCommand). Operate on CommandHistory, not document
content. WriterEngine handles them by popping the history stacks.

## ICommandHistory / CommandHistory
Undo/redo stacks of (IEditCommand, LokiDocument stateBefore) pairs.
MaxUndoDepth from LokiHostOptions. Push clears redo stack.
PopUndo / PopRedo return command + pre-state for engine to restore.

## IDocumentMutator
Stateless: Apply(document, command) → new LokiDocument.
InsertChar(document, char, at) → (document, newCaret).
Used by WriterEngine (committed commands) and PendingInputBuffer (preview).

## IPendingInputBuffer / PendingInputBuffer
Owned by WriterEngine. Accumulates keystrokes in _bufferedText.
Append(char, at, doc) → updates PendingSnapshot, resets idle timer.
Commit() → pushes InsertTextCommand to CommandHistory.
Discard() → drops buffer (Phase 7 remote edit hook).
HasPending / PendingSnapshot for render path.

## IFieldEvaluator / StaticFieldEvaluator
Phase 5: StaticFieldEvaluator returns FieldNode.StaticText unchanged.
Phase 6: live implementation computes page numbers, dates, metadata.
Injected into InlineMeasurer.

## FieldNode
InlineNode subtype for ODF field elements. Carries FieldKind and
StaticText (value LibreOffice last saved). Treated as RunNode by
InlineMeasurer in Phase 5.

## FieldKind
Enum: Unknown, Title, Description, Subject, Author, PageNumber,
PageCount, Date, Time, FileName, Chapter.

## OdfFieldMap
Internal static class in Format.Odf. Maps ODF element local names to
FieldKind. Used by BodyParser in the inline node parsing switch.

## Multi-page virtual canvas
LokiTileControl renders all pages as a single tall canvas.
Height = pageCount × (pageHeightPts + pageGapPts).
TileKey.PartIndex identifies which page a tile belongs to.
TileGridMath.PageForCanvasY / LocalYOnPage map canvas coords to pages.
Inter-page gap tiles have IsPageGap=true on PositionedTile —
rendered as grey (#808080) by LokiCompositionDrawOp.

## Phase 5 Exit Criterion
1. Open a multi-page document → all pages visible in continuous scroll.
2. Click in the document → caret appears at clicked position.
3. Type text → characters appear immediately at caret.
4. Press Backspace → character before caret deleted.
5. Press Enter → paragraph splits at caret.
6. Ctrl+Z → last word (or action) undone.
7. Ctrl+Y → redo restores undone change.
8. ODF field elements (text:title etc.) display their static text.
9. Bold/italic toggle via keyboard shortcut applies to selected text.
