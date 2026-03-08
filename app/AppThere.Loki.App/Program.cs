// LAYER:   AppThere.Loki.App — Entry Point
// KIND:    Host
// PURPOSE: Desktop entry point. Boots the Avalonia application with
//          platform-specific rendering and the LokiApplication class.
//          This is the only file in this project — all logic lives in
//          AppThere.Loki.Avalonia (LokiApplication, LokiMainWindow).
// DEPENDS: LokiApplication, Avalonia.Desktop
// USED BY: Platform runtime (dotnet run / published executable)
// PHASE:   4

using Avalonia;
using AppThere.Loki.Avalonia.Host;

namespace AppThere.Loki.App;

internal static class Program
{
    [System.STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<LokiApplication>()
            .UsePlatformDetect()
            .LogToTrace();
}
