using AppThere.Loki.Kernel.Color;
using AppThere.Loki.LokiKit.Document;
using AppThere.Loki.LokiKit.Host;
using AppThere.Loki.Writer.Editing;
using Xunit;

namespace AppThere.Loki.Tests.Writer.Editing;

public class CaretRegistryTests
{
    private static CaretPosition At(int p, int r, int c) => new(p, r, c, false);
    private static Selection Sel(CaretPosition at) => Selection.Collapsed(at);

    [Fact]
    public void Set_NewSession_AddsEntry()
    {
        var registry = new CaretRegistry();
        var session = SessionId.NewRandom();
        var selection = Sel(At(0, 0, 5));

        registry.Set(session, selection);

        var entry = registry.Get(session);
        Assert.NotNull(entry);
        Assert.Equal(session, entry.SessionId);
        Assert.Equal(selection, entry.Selection);
    }

    [Fact]
    public void Set_ExistingSession_UpdatesSelection()
    {
        var registry = new CaretRegistry();
        var session = SessionId.NewRandom();
        
        registry.Set(session, Sel(At(0, 0, 0)));
        var firstColor = registry.Get(session)!.Color;

        registry.Set(session, Sel(At(0, 0, 5)));

        var entry = registry.Get(session);
        Assert.NotNull(entry);
        Assert.Equal(5, entry.Selection.Focus.CharOffset);
        Assert.Equal(firstColor, entry.Color); // Color should be retained
    }

    [Fact]
    public void Get_UnknownSession_ReturnsNull()
    {
        var registry = new CaretRegistry();
        Assert.Null(registry.Get(SessionId.NewRandom()));
    }

    [Fact]
    public void GetAll_MultipleEntries_ReturnsAll()
    {
        var registry = new CaretRegistry();
        registry.Set(SessionId.NewRandom(), Sel(At(0, 0, 0)));
        registry.Set(SessionId.NewRandom(), Sel(At(0, 0, 1)));
        registry.Set(SessionId.NewRandom(), Sel(At(0, 0, 2)));

        var all = registry.GetAll();
        Assert.Equal(3, all.Count);
    }

    [Fact]
    public void Remove_ExistingSession_RemovesEntry()
    {
        var registry = new CaretRegistry();
        var session = SessionId.NewRandom();
        
        registry.Set(session, Sel(At(0, 0, 0)));
        Assert.NotNull(registry.Get(session));

        registry.Remove(session);
        Assert.Null(registry.Get(session));
    }

    [Fact]
    public void CaretChanged_FiredOnSet()
    {
        var registry = new CaretRegistry();
        var session = SessionId.NewRandom();
        bool fired = false;

        registry.CaretChanged += (s, e) => { if (e == session) fired = true; };
        
        registry.Set(session, Sel(At(0, 0, 0)));
        
        Assert.True(fired);
    }

    [Fact]
    public void CaretChanged_FiredOnRemove()
    {
        var registry = new CaretRegistry();
        var session = SessionId.NewRandom();
        registry.Set(session, Sel(At(0, 0, 0)));
        
        bool fired = false;
        registry.CaretChanged += (s, e) => { if (e == session) fired = true; };
        
        registry.Remove(session);
        
        Assert.True(fired);
    }

    [Fact]
    public void CaretChanged_NotFiredOnGetAll()
    {
        var registry = new CaretRegistry();
        registry.Set(SessionId.NewRandom(), Sel(At(0, 0, 0)));
        
        bool fired = false;
        registry.CaretChanged += (s, e) => fired = true;
        
        var all = registry.GetAll();
        
        Assert.False(fired);
    }

    [Fact]
    public void AssignColor_DifferentSessions_DifferentColors()
    {
        var registry = new CaretRegistry();
        var colors = new List<LokiColor>();

        for (int i = 0; i < 9; i++)
        {
            var session = SessionId.NewRandom();
            registry.Set(session, Sel(At(0, 0, 0)));
            colors.Add(registry.Get(session)!.Color);
        }

        // Palette has 8 colors, 9th should loop back to 1st
        Assert.Equal(colors[0], colors[8]);
        
        // Ensure the first 8 are distinct
        var uniqueColors = new HashSet<LokiColor>(colors.Take(8));
        Assert.Equal(8, uniqueColors.Count);
    }
}
