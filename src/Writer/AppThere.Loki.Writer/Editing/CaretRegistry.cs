// LAYER:   AppThere.Loki.Writer — Editing
// KIND:    Implementation (caret registry)
// PURPOSE: Thread-safe repository for all active caret positions and selections
//          in the document. Implements ICaretRegistry for the view layer.
//          Assigns default colours from a fixed 8-colour palette.
// DEPENDS: SessionId, Selection, CaretEntry, LokiColor, ICaretRegistry
// USED BY: WriterEngine
// PHASE:   5
// ADR:     ADR-012

using AppThere.Loki.Kernel.Color;
using AppThere.Loki.LokiKit.Document;
using AppThere.Loki.LokiKit.Engine;
using AppThere.Loki.LokiKit.Host;

namespace AppThere.Loki.Writer.Editing;

internal sealed class CaretRegistry : ICaretRegistry
{
    private readonly Dictionary<SessionId, CaretEntry> _entries = new();
    private readonly object _lock = new();

    public event EventHandler<SessionId>? CaretChanged;

    public void Set(SessionId sessionId, Selection selection, LokiColor? colorOverride = null)
    {
        lock (_lock)
        {
            _entries.TryGetValue(sessionId, out var existing);
            var color = colorOverride ?? existing?.Color ?? AssignColor(sessionId);
            
            _entries[sessionId] = new CaretEntry(
                sessionId,
                selection,
                DateTime.UtcNow,
                existing?.DisplayName,
                color);
        }
        
        CaretChanged?.Invoke(this, sessionId);
    }

    public CaretEntry? Get(SessionId sessionId)
    {
        lock (_lock)
        {
            return _entries.TryGetValue(sessionId, out var entry) ? entry : null;
        }
    }

    public IReadOnlyList<CaretEntry> GetAll()
    {
        lock (_lock)
        {
            return _entries.Values.ToList();
        }
    }

    public void Remove(SessionId sessionId)
    {
        bool removed;
        lock (_lock)
        {
            removed = _entries.Remove(sessionId);
        }
        
        if (removed)
        {
            CaretChanged?.Invoke(this, sessionId);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _entries.Clear();
        }
    }

    private LokiColor AssignColor(SessionId sessionId)
    {
        // 8-colour cycle
        int index = _entries.Count % 8;
        return index switch
        {
            0 => LokiColor.FromHex("E53935"),
            1 => LokiColor.FromHex("8E24AA"),
            2 => LokiColor.FromHex("1E88E5"),
            3 => LokiColor.FromHex("00897B"),
            4 => LokiColor.FromHex("F4511E"),
            5 => LokiColor.FromHex("039BE5"),
            6 => LokiColor.FromHex("7CB342"),
            7 => LokiColor.FromHex("FB8C00"),
            _ => LokiColor.FromHex("1565C0")
        };
    }
}
