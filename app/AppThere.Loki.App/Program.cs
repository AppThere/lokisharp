// LAYER:   AppThere.Loki.App — Entry Point
// KIND:    Host
// PURPOSE: Desktop entry point. Boots the Avalonia application with
//          platform-specific rendering and the LokiApplication class.
//          Registers the OdfImporter for real file I/O. This is the
//          only file in this project — all logic lives in
//          AppThere.Loki.Avalonia (LokiApplication, LokiMainWindow).
// DEPENDS: LokiApplication, Avalonia.Desktop, OdfImporter
// USED BY: Platform runtime (dotnet run / published executable)
// PHASE:   4

using Avalonia;
using AppThere.Loki.Avalonia.Host;
using AppThere.Loki.Format.Odf;
using AppThere.Loki.Writer.Engine;
using Microsoft.Extensions.DependencyInjection;

namespace AppThere.Loki.App;

internal static class Program
{
    [System.STAThread]
    public static void Main(string[] args)
    {
        // Register the real ODF importer so FODT/ODT files can be opened.
        LokiApplication.HostConfigure = builder =>
        {
            builder.ConfigureServices(services =>
            {
                var existing = services.FirstOrDefault(
                    d => d.ServiceType == typeof(IOdfImporter));
                if (existing is not null) services.Remove(existing);
                services.AddScoped<IOdfImporter, OdfImporter>();
            });
        };

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<LokiApplication>()
            .UsePlatformDetect()
            .LogToTrace();
}
