// LAYER:   AppThere.Loki.Avalonia — Host
// KIND:    Class (Avalonia Window)
// PURPOSE: Root window. Owns the LokiHost and the tab strip of open
//          documents. Responds to window resize by computing the active
//          LayoutBreakpoint and switching between Phone and Normal layouts.
//          Phase 4 implements Phone and Normal only; Compact/Wide/UltraWide
//          fall through to the nearest implemented breakpoint.
//
//          Phone layout  (<900 DIPs wide):
//            - Full-screen LokiTileControl
//            - Bottom navigation bar (document tabs as icons)
//            - No persistent toolbar (toolbar slides in from top on tap)
//
//          Normal layout (≥900 DIPs wide):
//            - Toolbar across the top
//            - LokiTileControl fills remaining space
//            - Collapsed sidebar (toggle button, sidebar Phase 5)
//
// DEPENDS: ILokiHost, LokiTileControl, TileCacheOptions, LayoutBreakpoint
// USED BY: LokiApplication
// PHASE:   4

using Avalonia.Controls;
using AppThere.Loki.LokiKit.Host;
using AppThere.Loki.Avalonia.Cache;

namespace AppThere.Loki.Avalonia.Host;

public sealed class LokiMainWindow : Window
{
    private readonly ILokiHost        _host;
    private readonly TileCacheOptions _cacheOptions;
    private LayoutBreakpoint          _currentBreakpoint;

    public LokiMainWindow(ILokiHost host, TileCacheOptions cacheOptions)
    {
        _host         = host;
        _cacheOptions = cacheOptions;
        // Implementation: InitializeComponent(), subscribe to SizeChanged,
        // create initial layout, set Title = "AppThere Loki".
        throw new NotImplementedException("Implemented by Claude Code");
    }

    /// <summary>
    /// Recomputes breakpoint from current ClientSize.Width and switches
    /// layout template if the breakpoint changed.
    /// </summary>
    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
        => throw new NotImplementedException("Implemented by Claude Code");

    /// <summary>
    /// Applies the Phone layout (full-screen tile control + bottom nav).
    /// </summary>
    private void ApplyPhoneLayout()
        => throw new NotImplementedException("Implemented by Claude Code");

    /// <summary>
    /// Applies the Normal desktop layout (toolbar + tile control).
    /// </summary>
    private void ApplyNormalLayout()
        => throw new NotImplementedException("Implemented by Claude Code");

    /// <summary>
    /// Opens a document from a file path and adds a tab.
    /// Called from File → Open and from drag-and-drop.
    /// </summary>
    public Task OpenDocumentAsync(string filePath)
        => throw new NotImplementedException("Implemented by Claude Code");
}
