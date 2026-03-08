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
//
//          Normal layout (≥900 DIPs wide):
//            - Toolbar across the top
//            - LokiTileControl fills remaining space
//
// DEPENDS: ILokiHost, LokiTileControl, TileCacheOptions, LayoutBreakpoint
// USED BY: LokiApplication
// PHASE:   4

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using AppThere.Loki.Avalonia.Cache;
using AppThere.Loki.Avalonia.Controls;
using AppThere.Loki.LokiKit.Document;
using AppThere.Loki.LokiKit.Host;
using AppThere.Loki.LokiKit.View;

namespace AppThere.Loki.Avalonia.Host;

public sealed class LokiMainWindow : Window
{
    private readonly ILokiHost        _host;
    private readonly TileCacheOptions _cacheOptions;
    private readonly LokiTileControl  _tileControl;
    private LayoutBreakpoint          _currentBreakpoint;
    private ILokiDocument?            _currentDoc;
    private ILokiView?                _currentView;

    public LokiMainWindow(ILokiHost host, TileCacheOptions cacheOptions)
    {
        _host        = host;
        _cacheOptions = cacheOptions;
        _tileControl = new LokiTileControl(_cacheOptions);

        Title     = "AppThere Loki";
        Width     = 1024;
        Height    = 768;
        MinWidth  = 320;
        MinHeight = 480;
        Background = Brushes.White;

        _currentBreakpoint = LayoutBreakpointResolver.Resolve(Width);
        ApplyLayout(LayoutBreakpointResolver.NearestImplemented(_currentBreakpoint));

        SizeChanged += OnSizeChanged;
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e) =>
        ApplyBreakpointForWidth(e.NewSize.Width);

    // Internal so tests can simulate resize without a running Avalonia event loop.
    internal void ApplyBreakpointForWidth(double widthDips)
    {
        var bp          = LayoutBreakpointResolver.Resolve(widthDips);
        var implemented = LayoutBreakpointResolver.NearestImplemented(bp);
        if (implemented == _currentBreakpoint) return;
        _currentBreakpoint = implemented;
        ApplyLayout(implemented);
    }

    private void ApplyLayout(LayoutBreakpoint bp)
    {
        switch (bp)
        {
            case LayoutBreakpoint.Phone:
                ApplyPhoneLayout();
                break;
            default:
                ApplyNormalLayout();
                break;
        }
    }

    private void ApplyPhoneLayout()
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Star));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        Grid.SetRow(_tileControl, 0);
        var nav = BuildBottomNav();
        Grid.SetRow(nav, 1);

        grid.Children.Add(_tileControl);
        grid.Children.Add(nav);
        Content = grid;
    }

    private void ApplyNormalLayout()
    {
        var dock    = new DockPanel();
        var toolbar = BuildToolbar();
        DockPanel.SetDock(toolbar, Dock.Top);
        dock.Children.Add(toolbar);
        dock.Children.Add(_tileControl);
        Content = dock;
    }

    private Control BuildToolbar()
    {
        var titleLabel = new TextBlock
        {
            Text = "AppThere Loki",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0),
        };

        var openBtn = new Button { Content = "Open…", Margin = new Thickness(4, 0) };
        openBtn.Click += async (_, _) => await OpenDocumentAsync();

        var zoomLabel = new TextBlock
        {
            Text = "100%",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0),
        };
        _tileControl.GetObservable(LokiTileControl.ZoomProperty)
            .Subscribe(z => zoomLabel.Text = $"{(int)(z * 100)}%");

        var bar = new DockPanel
        {
            LastChildFill = false,
            Height        = 40,
            Background    = Brushes.LightGray,
        };
        DockPanel.SetDock(titleLabel, Dock.Left);
        DockPanel.SetDock(openBtn,    Dock.Left);
        DockPanel.SetDock(zoomLabel,  Dock.Right);
        bar.Children.Add(titleLabel);
        bar.Children.Add(openBtn);
        bar.Children.Add(zoomLabel);
        return bar;
    }

    private Control BuildBottomNav()
    {
        var openBtn = new Button { Content = "Open", Margin = new Thickness(8) };
        openBtn.Click += async (_, _) => await OpenDocumentAsync();

        return new Border
        {
            Height     = 56,
            Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
            Child      = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children    = { openBtn },
            },
        };
    }

    public async Task OpenDocumentAsync(string? filePath = null)
    {
        if (filePath is null)
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title         = "Open Document",
                AllowMultiple = false,
                FileTypeFilter = new List<FilePickerFileType>
                {
                    new FilePickerFileType("ODF Writer")
                    {
                        Patterns = new[] { "*.fodt", "*.odt" },
                    },
                },
            });
            if (files.Count == 0) return;
            filePath = files[0].Path.LocalPath;
        }

        _currentView?.Dispose();
        if (_currentDoc is not null)
            await _currentDoc.DisposeAsync();

        _currentDoc  = await _host.OpenAsync(File.OpenRead(filePath), OpenOptions.Default);
        _currentView = _host.CreateView(_currentDoc);

        _tileControl.DocumentView = _currentView;
        _tileControl.Zoom         = 1.0f;
        _tileControl.ScrollOffset = new Vector(0, 0);
        Title = $"{Path.GetFileName(filePath)} — AppThere Loki";
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        var first = e.Data.GetFileNames()?.FirstOrDefault();
        if (first is not null)
            await OpenDocumentAsync(first);
    }
}
