// LAYER:   AppThere.Loki.Writer — Editing
// KIND:    Implementation (document mutator)
// PURPOSE: Stateless document transformer. Applies IEditCommand operations to
//          a LokiDocument snapshot and returns a new snapshot. Never mutates
//          the input document.
// DEPENDS: LokiDocument, IEditCommand, IDocumentMutator, ILokiLogger
// USED BY: WriterEngine, PendingInputBuffer
// PHASE:   5
// ADR:     ADR-013

using System.Collections.Immutable;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.LokiKit.Commands;
using AppThere.Loki.LokiKit.Document;
using AppThere.Loki.LokiKit.Host;
using AppThere.Loki.Writer.Model;
using AppThere.Loki.Writer.Model.Inlines;
using AppThere.Loki.Writer.Model.Styles;

namespace AppThere.Loki.Writer.Editing;

internal sealed class DocumentMutator : IDocumentMutator
{
    private readonly ILokiLogger _logger;

    public DocumentMutator(ILokiLogger logger)
    {
        _logger = logger;
    }

    public LokiDocument Apply(LokiDocument document, IEditCommand command)
    {
        try
        {
            return command switch
            {
                InsertTextCommand cmd => ApplyInsert(document, cmd),
                DeleteTextCommand cmd => ApplyDelete(document, cmd),
                SplitParagraphCommand cmd => ApplySplit(document, cmd),
                MergeParagraphCommand cmd => ApplyMerge(document, cmd),
                SetCharacterStyleCommand cmd => ApplySetStyle(document, cmd),
                _ => HandleUnknownCommand(document, command)
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to apply {command.GetType().Name}: {ex.Message}");
            return document; // Safe fallback
        }
    }

    private LokiDocument HandleUnknownCommand(LokiDocument document, IEditCommand command)
    {
        _logger.Warn($"Unknown command {command.GetType().Name}");
        return document;
    }

    public (LokiDocument Document, CaretPosition NewCaret) InsertChar(
        LokiDocument document, char character, CaretPosition at)
    {
        // Synthetic command for the insert
        var cmd = new InsertTextCommand(
            SessionId.NewRandom(),
            DocumentVersion.Zero,
            at,
            character.ToString());

        var newDoc = ApplyInsert(document, cmd);
        var newPosition = new CaretPosition(
            at.ParagraphIndex,
            at.RunIndex,
            at.CharOffset + 1,
            false);

        return (newDoc, newPosition);
    }

    private LokiDocument ApplyInsert(LokiDocument document, InsertTextCommand cmd)
    {
        var at = cmd.At;
        
        // Clamp paragraph index
        int paragraphIndex = Math.Clamp(at.ParagraphIndex, 0, document.Body.Count - 1);
        if (at.ParagraphIndex != paragraphIndex)
        {
            _logger.Debug($"ApplyInsert: Clamped paragraph index {at.ParagraphIndex} to {paragraphIndex}");
        }

        if (document.Body[paragraphIndex] is not ParagraphNode paraNode)
        {
            return document;
        }

        var inlines = paraNode.Inlines;
        
        // Handle empty paragraph or out-of-bounds run index
        if (inlines.IsEmpty)
        {
            var newRun = new RunNode(cmd.Text, ParagraphStyle.Default.AsCharStyle(), null);
            var newPara = paraNode with { Inlines = ImmutableList.Create<InlineNode>(newRun) };
            return document with { Body = document.Body.SetItem(paragraphIndex, newPara) };
        }

        // Clamp run index
        int runIndex = Math.Clamp(at.RunIndex, 0, inlines.Count - 1);
        if (at.RunIndex != runIndex)
        {
            _logger.Debug($"ApplyInsert: Clamped run index {at.RunIndex} to {runIndex}");
        }

        if (inlines[runIndex] is not RunNode runNode)
        {
            // If the clamped run is not a text run, we can't easily insert text.
            // For Phase 5, all inlines are RunNodes.
            return document;
        }

        // Clamp char offset
        int charOffset = Math.Clamp(at.CharOffset, 0, runNode.Text.Length);
        if (at.CharOffset != charOffset)
        {
            _logger.Debug($"ApplyInsert: Clamped char offset {at.CharOffset} to {charOffset}");
        }

        var newText = runNode.Text.Insert(charOffset, cmd.Text);
        var modifiedRun = runNode with { Text = newText };
        var newInlines = inlines.SetItem(runIndex, modifiedRun);
        var modifiedPara = paraNode with { Inlines = newInlines };

        return document with { Body = document.Body.SetItem(paragraphIndex, modifiedPara) };
    }

    private LokiDocument ApplyDelete(LokiDocument document, DeleteTextCommand cmd)
    {
        var from = cmd.From;
        if (from.ParagraphIndex < 0 || from.ParagraphIndex >= document.Body.Count)
        {
            _logger.Warn($"Delete out of bounds: paragraph {from.ParagraphIndex}");
            return document;
        }

        if (document.Body[from.ParagraphIndex] is not ParagraphNode paraNode)
        {
            return document;
        }

        var inlines = paraNode.Inlines;
        if (from.RunIndex < 0 || from.RunIndex >= inlines.Count || inlines[from.RunIndex] is not RunNode firstRunNode)
        {
            _logger.Warn($"Delete out of bounds: run {from.RunIndex}");
            return document;
        }

        if (from.CharOffset < 0 || from.CharOffset > firstRunNode.Text.Length)
        {
            _logger.Warn($"Delete out of bounds: char {from.CharOffset}");
            return document;
        }

        var newInlines = inlines.ToBuilder();
        int remainingToDelete = cmd.Length;
        int currentRunIndex = from.RunIndex;
        int currentOffset = from.CharOffset;

        while (remainingToDelete > 0 && currentRunIndex < newInlines.Count)
        {
            if (newInlines[currentRunIndex] is not RunNode currentRun)
            {
                // Cannot delete across non-text nodes like lines breaks or tabs easily using standard text offsets
                // Simple implementation for Phase 5: delete stops at non-text nodes or deletes them as 1 char.
                // Assuming Length is strictly character count. For now, skip non-RunNodes.
                newInlines.RemoveAt(currentRunIndex);
                remainingToDelete--;
                continue;
            }

            int charsToDeleteFromThisRun = Math.Min(remainingToDelete, currentRun.Text.Length - currentOffset);
            
            if (charsToDeleteFromThisRun == currentRun.Text.Length && currentOffset == 0)
            {
                // Delete entire run
                newInlines.RemoveAt(currentRunIndex);
            }
            else
            {
                var newText = currentRun.Text.Remove(currentOffset, charsToDeleteFromThisRun);
                newInlines[currentRunIndex] = currentRun with { Text = newText };
                currentRunIndex++; // Move to next run if we didn't remove this one
            }

            remainingToDelete -= charsToDeleteFromThisRun;
            currentOffset = 0; // For subsequent runs, we always delete from the start
        }

        // Clean up empty runs if there are any remaining (e.g., from partial deletions)
        for (int i = newInlines.Count - 1; i >= 0; i--)
        {
            if (newInlines[i] is RunNode run && string.IsNullOrEmpty(run.Text))
            {
                newInlines.RemoveAt(i);
            }
        }

        // Ensure paragraph has at least one empty run if completely emptied
        if (newInlines.Count == 0)
        {
            newInlines.Add(new RunNode("", ParagraphStyle.Default.AsCharStyle(), null));
        }
        else
        {
            // Finally merge any adjacent matching runs.
            MergeAdjacentRuns(newInlines);
        }

        var modifiedPara = paraNode with { Inlines = newInlines.ToImmutable() };
        return document with { Body = document.Body.SetItem(from.ParagraphIndex, modifiedPara) };
    }

    private LokiDocument ApplySplit(LokiDocument document, SplitParagraphCommand cmd)
    {
        var at = cmd.At;
        if (at.ParagraphIndex < 0 || at.ParagraphIndex >= document.Body.Count) return document;
        if (document.Body[at.ParagraphIndex] is not ParagraphNode paraNode) return document;

        var inlines = paraNode.Inlines;
        
        // Create para 1 inlines
        var para1Inlines = ImmutableList.CreateBuilder<InlineNode>();
        var para2Inlines = ImmutableList.CreateBuilder<InlineNode>();

        for (int i = 0; i < inlines.Count; i++)
        {
            if (i < at.RunIndex)
            {
                para1Inlines.Add(inlines[i]);
            }
            else if (i > at.RunIndex)
            {
                para2Inlines.Add(inlines[i]);
            }
            else
            {
                // Splitting the run itself
                if (inlines[i] is RunNode splitRun)
                {
                    if (at.CharOffset > 0)
                    {
                        para1Inlines.Add(splitRun with { Text = splitRun.Text.Substring(0, at.CharOffset) });
                    }
                    if (at.CharOffset < splitRun.Text.Length)
                    {
                        para2Inlines.Add(splitRun with { Text = splitRun.Text.Substring(at.CharOffset) });
                    }
                }
                else
                {
                    // If it's not a RunNode, just put it in para2
                    para2Inlines.Add(inlines[i]);
                }
            }
        }

        // Ensure neither paragraph is empty
        if (para1Inlines.Count == 0)
        {
            para1Inlines.Add(new RunNode("", ParagraphStyle.Default.AsCharStyle(), null));
        }
        if (para2Inlines.Count == 0)
        {
            var charStyle = inlines.Count > 0 && inlines[at.RunIndex] is RunNode rn ? rn.Style : ParagraphStyle.Default.AsCharStyle();
            para2Inlines.Add(new RunNode("", charStyle, null));
        }

        var para1 = paraNode with { Inlines = para1Inlines.ToImmutable() };
        var para2 = paraNode with { Inlines = para2Inlines.ToImmutable() };

        var newBody = document.Body.RemoveAt(at.ParagraphIndex)
                                   .Insert(at.ParagraphIndex, para1)
                                   .Insert(at.ParagraphIndex + 1, para2);

        return document with { Body = newBody };
    }

    private LokiDocument ApplyMerge(LokiDocument document, MergeParagraphCommand cmd)
    {
        if (cmd.ParagraphIndex <= 0 || cmd.ParagraphIndex >= document.Body.Count) return document;
        
        if (document.Body[cmd.ParagraphIndex - 1] is not ParagraphNode prevPara ||
            document.Body[cmd.ParagraphIndex] is not ParagraphNode currPara)
        {
            return document;
        }

        var mergedInlines = prevPara.Inlines.AddRange(currPara.Inlines);
        
        var newInlinesBuilder = mergedInlines.ToBuilder();
        MergeAdjacentRuns(newInlinesBuilder);
        
        // Remove trailing empty runs that might have been carried over
        for (int i = newInlinesBuilder.Count - 1; i >= 0; i--)
        {
            if (newInlinesBuilder.Count > 1 && newInlinesBuilder[i] is RunNode run && string.IsNullOrEmpty(run.Text))
            {
                newInlinesBuilder.RemoveAt(i);
            }
        }

        if (newInlinesBuilder.Count == 0)
        {
            // Fallback just in case
            newInlinesBuilder.Add(new RunNode("", prevPara.Style.AsCharStyle(), null));
        }

        var mergedPara = prevPara with { Inlines = newInlinesBuilder.ToImmutable() };
        
        var newBody = document.Body.SetItem(cmd.ParagraphIndex - 1, mergedPara)
                                   .RemoveAt(cmd.ParagraphIndex);

        return document with { Body = newBody };
    }

    private LokiDocument ApplySetStyle(LokiDocument document, SetCharacterStyleCommand cmd)
    {
        // Simple implementation for Phase 5.
        // Assuming styling is restricted within a single paragraph for now as per simple use case.
        if (cmd.From.ParagraphIndex != cmd.To.ParagraphIndex || cmd.From.ParagraphIndex < 0 || cmd.From.ParagraphIndex >= document.Body.Count)
        {
            return document;
        }

        if (document.Body[cmd.From.ParagraphIndex] is not ParagraphNode paraNode) return document;

        var inlines = paraNode.Inlines.ToBuilder();
        
        // We will collect new inlines here to replace the old ones
        var newInlines = new List<InlineNode>();

        for (int i = 0; i < inlines.Count; i++)
        {
            if (i < cmd.From.RunIndex || i > cmd.To.RunIndex)
            {
                newInlines.Add(inlines[i]);
                continue;
            }

            if (inlines[i] is not RunNode runNode)
            {
                newInlines.Add(inlines[i]);
                continue;
            }

            // Determine overlap bounds for this run
            int runStart = (i == cmd.From.RunIndex) ? cmd.From.CharOffset : 0;
            int runEnd = (i == cmd.To.RunIndex) ? cmd.To.CharOffset : runNode.Text.Length;

            if (runStart > 0)
            {
                // Unstyled prefix
                newInlines.Add(runNode with { Text = runNode.Text.Substring(0, runStart) });
            }

            if (runStart < runEnd)
            {
                // Styled middle
                var newStyle = runNode.Style;
                if (cmd.Style.Bold.HasValue) newStyle = newStyle with { Bold = cmd.Style.Bold.Value };
                if (cmd.Style.Italic.HasValue) newStyle = newStyle with { Italic = cmd.Style.Italic.Value };
                // Add other style properties here if needed.

                newInlines.Add(runNode with 
                { 
                    Text = runNode.Text.Substring(runStart, runEnd - runStart),
                    Style = newStyle
                });
            }

            if (runEnd < runNode.Text.Length)
            {
                // Unstyled suffix
                newInlines.Add(runNode with { Text = runNode.Text.Substring(runEnd) });
            }
        }

        // Convert List back to builder and merge
        var finalBuilder = ImmutableList.CreateBuilder<InlineNode>();
        finalBuilder.AddRange(newInlines);
        MergeAdjacentRuns(finalBuilder);

        var modifiedPara = paraNode with { Inlines = finalBuilder.ToImmutable() };
        return document with { Body = document.Body.SetItem(cmd.From.ParagraphIndex, modifiedPara) };
    }

    private void MergeAdjacentRuns(ImmutableList<InlineNode>.Builder inlines)
    {
        for (int i = 0; i < inlines.Count - 1; i++)
        {
            if (inlines[i] is RunNode currentRun && inlines[i + 1] is RunNode nextRun)
            {
                if (currentRun.Style == nextRun.Style)
                {
                    inlines[i] = currentRun with { Text = currentRun.Text + nextRun.Text };
                    inlines.RemoveAt(i + 1);
                    i--; // re-check the merged run with the next one
                }
            }
        }
    }
}
