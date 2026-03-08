// LAYER:   AppThere.Loki.Avalonia — Host
// KIND:    Class (Avalonia Application subclass)
// PURPOSE: Avalonia Application entry class. Builds the LokiHost,
//          registers services, and creates the main window.
//          Responsible for platform detection and passing appropriate
//          TileCacheOptions to UseAvaloniaSurfaces().
// DEPENDS: LokiHostBuilder, LokiHostBuilderExtensions, LokiMainWindow
// USED BY: Program.BuildAvaloniaApp()
// PHASE:   4

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Themes.Fluent;
using AppThere.Loki.Avalonia.Cache;
using AppThere.Loki.LokiKit.Host;
using AppThere.Loki.Writer.Engine;

namespace AppThere.Loki.Avalonia.Host;

public sealed class LokiApplication : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var options = SelectTileCacheOptions();

            var host = new LokiHostBuilder()
                .UseHeadlessSurfaces()       // overridden by UseAvaloniaSurfaces below
                .UseSkiaRenderer()
                .UseSkiaFonts()
                .UseConsoleLogger()
                .UseWriterEngine()
                .UseAvaloniaSurfaces(options)
                .Build();

            desktop.MainWindow = new LokiMainWindow(host, options);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static TileCacheOptions SelectTileCacheOptions()
    {
        // Phase 4: desktop only. Phase 5 adds mobile detection.
        var options = TileCacheOptions.Desktop;

        // Override memory cap based on available system memory if possible.
        try
        {
            var gcInfo = System.GC.GetGCMemoryInfo();
            if (gcInfo.TotalAvailableMemoryBytes > 0)
            {
                var cap = gcInfo.TotalAvailableMemoryBytes / 8;
                options = options with { MemoryCapBytes = cap };
            }
        }
        catch { /* non-fatal — use default cap */ }

        return options;
    }
}
