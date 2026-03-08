// LAYER:   AppThere.Loki.App — Entry Point
// KIND:    Program entry point
// PURPOSE: Thin entry point. Configures Avalonia AppBuilder and launches
//          the Avalonia application. All substantive logic lives in
//          AppThere.Loki.Avalonia. This project exists solely to carry
//          the Exe output type and platform-specific dependencies
//          (Avalonia.Desktop on desktop, Avalonia.Android on Android, etc.)
// PHASE:   4

using Avalonia;
using AppThere.Loki.Avalonia.Host;

namespace AppThere.Loki.App;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<LokiApplication>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
