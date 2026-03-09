// LAYER:   AppThere.Loki.Writer — Editing
// KIND:    Implementation (pending input buffer)
// PURPOSE: Decouples keystroke accumulation from command commit.
//          Commits at word boundaries or idle timeout.
// DEPENDS: IPendingInputBuffer, ICommandHistory, IDocumentMutator, SessionId
// USED BY: WriterEngine
// PHASE:   5
// ADR:     ADR-014

using System.Text;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.LokiKit.Commands;
using AppThere.Loki.LokiKit.Document;
using AppThere.Loki.LokiKit.Host;
using AppThere.Loki.Writer.Model;

namespace AppThere.Loki.Writer.Editing;

internal sealed class PendingInputBuffer : IPendingInputBuffer
{
    private readonly ICommandHistory   _history;
    private readonly IDocumentMutator  _mutator;
    private readonly SessionId         _sessionId;
    private readonly int               _idleCommitMs;
    private readonly ILokiLogger       _logger;

    private readonly StringBuilder     _buffer = new();
    private CaretPosition?             _insertionPoint;
    private LokiDocument?              _pendingSnapshot;
    private LokiDocument               _committedSnapshot;
    private CancellationTokenSource?   _idleCts;
    private readonly object            _lock = new();

    public PendingInputBuffer(
        ICommandHistory history, 
        IDocumentMutator mutator, 
        SessionId sessionId, 
        int idleCommitMs, 
        ILokiLogger logger)
    {
        _history      = history;
        _mutator      = mutator;
        _sessionId    = sessionId;
        _idleCommitMs = idleCommitMs;
        _logger       = logger;
        
        // Will be overwritten on first Append or Commit anyway, but needs initialisation
        _committedSnapshot = LokiDocument.Empty;
    }

    public bool HasPending
    {
        get
        {
            lock (_lock) return _buffer.Length > 0;
        }
    }

    public LokiDocument PendingSnapshot
    {
        get
        {
            lock (_lock) return _pendingSnapshot ?? _committedSnapshot;
        }
    }

    public CaretPosition Append(char character, CaretPosition at, LokiDocument baseDocument)
    {
        CaretPosition newCaret;
        
        lock (_lock)
        {
            if (_buffer.Length == 0)
            {
                _insertionPoint    = at;
                _committedSnapshot = baseDocument;
            }
            
            _buffer.Append(character);
            
            var (newDoc, caret) = _mutator.InsertChar(
                _pendingSnapshot ?? _committedSnapshot, character, at);
                
            _pendingSnapshot = newDoc;
            newCaret = caret;
        }
        
        ResetIdleTimer();
        return newCaret;
    }

    public void Commit()
    {
        string text;
        CaretPosition at;
        LokiDocument doc;
        int ver;

        lock (_lock)
        {
            if (_buffer.Length == 0) return;
            
            text = _buffer.ToString();
            at   = _insertionPoint!;
            doc  = _committedSnapshot;
            ver  = _committedSnapshot.LayoutVersion;
            
            _buffer.Clear();
            _insertionPoint  = null;
            _pendingSnapshot = null;
        }
        
        CancelIdleTimer();
        _history.Push(new InsertTextCommand(_sessionId, new DocumentVersion(ver), at, text), doc);
    }

    public void Discard()
    {
        lock (_lock)
        {
            _buffer.Clear();
            _insertionPoint  = null;
            _pendingSnapshot = null;
        }
        CancelIdleTimer();
    }

    private void ResetIdleTimer()
    {
        CancelIdleTimer();
        
        if (_idleCommitMs <= 0)
        {
            Commit(); // Synchronous commit mode
            return;
        }
        
        _idleCts = new CancellationTokenSource();
        var token = _idleCts.Token;
        
        Task.Run(async () => 
        {
            try 
            {
                await Task.Delay(_idleCommitMs, token).ConfigureAwait(false);
                Commit();
            } 
            catch (OperationCanceledException) 
            { 
            }
        });
    }

    private void CancelIdleTimer()
    {
        _idleCts?.Cancel();
        _idleCts?.Dispose();
        _idleCts = null;
    }

    public ValueTask DisposeAsync()
    {
        CancelIdleTimer();
        Commit();
        return ValueTask.CompletedTask;
    }
}
