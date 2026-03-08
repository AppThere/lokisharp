// LAYER:   AppThere.Loki.Tests.Avalonia — Tests
// KIND:    Tests (test infrastructure)
// PURPOSE: Minimal Avalonia Application for headless unit testing.
//          Initialised once per test assembly via AvaloniaTestFixture.
//          Required so that Avalonia platform types (WriteableBitmap,
//          Dispatcher.UIThread) are available without a display.
// DEPENDS: Avalonia.Headless, Avalonia
// USED BY: LokiTileCacheTests
// PHASE:   4

using Avalonia;
using Avalonia.Headless;

[assembly: CollectionBehavior(DisableTestParallelization = false)]

namespace AppThere.Loki.Tests.Avalonia;

/// <summary>Minimal Avalonia application for headless tests.</summary>
internal sealed class AvaloniaTestApp : Application { }

/// <summary>
/// Assembly-level fixture that boots Avalonia in headless mode.
/// Shared across all test collections that depend on Avalonia types.
/// </summary>
public sealed class AvaloniaTestFixture : IDisposable
{
    private static readonly object _initLock = new();
    private static bool _initialized;

    public AvaloniaTestFixture()
    {
        lock (_initLock)
        {
            if (_initialized) return;
            AppBuilder.Configure<AvaloniaTestApp>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions
                {
                    UseHeadlessDrawing = true,
                })
                .SetupWithoutStarting();
            _initialized = true;
        }
    }

    public void Dispose() { }
}
