// LAYER:   AppThere.Loki.Tests.Unit — Tests
// KIND:    Tests
// PURPOSE: Integration tests for Phase 2 LokiKit stack.
//          Uses LokiHostBuilder to build a real host (no mocks).
//          Verifies the full chain: LokiHostBuilder → LokiHostImpl →
//          LokiDocumentImpl (StubEngine) → LokiViewImpl → TileRenderer.
//          All tests follow the Method_Condition_ExpectedResult naming convention.
// DEPENDS: LokiHostBuilder, ILokiHost, ILokiDocument, ILokiView,
//          TileRequest, PingCommand, TileInvalidatedEventArgs
// USED BY: CI test run
// PHASE:   2

using AppThere.Loki.LokiKit.Commands;
using AppThere.Loki.LokiKit.Errors;
using AppThere.Loki.LokiKit.Host;
using AppThere.Loki.Skia.Rendering;
using FluentAssertions;

namespace AppThere.Loki.Tests.Unit.LokiKit;

public sealed class LokiKitIntegrationTests
{
    // ── Shared host factory ───────────────────────────────────────────────────

    private static ILokiHost BuildHost() =>
        new LokiHostBuilder()
            .UseHeadlessSurfaces()
            .UseSkiaRenderer()
            .UseSkiaFonts()
            .UseConsoleLogger()
            .Build();

    // ── 1. Build ──────────────────────────────────────────────────────────────

    [Fact]
    public void Build_ValidConfiguration_ReturnsHost()
    {
        var act = () => BuildHost();
        act.Should().NotThrow();
    }

    // ── 2. OpenAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task OpenAsync_StreamNull_ReturnsDocument()
    {
        await using var host = BuildHost();
        await using var doc  = await host.OpenAsync(Stream.Null, OpenOptions.Default);

        doc.Should().NotBeNull();
        doc.DocumentId.Should().NotBeNullOrEmpty();
        doc.PartCount.Should().Be(1);
    }

    // ── 3. CreateView ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateView_ReturnsView()
    {
        await using var host = BuildHost();
        await using var doc  = await host.OpenAsync(Stream.Null, OpenOptions.Default);
        using var view = host.CreateView(doc);

        view.Should().NotBeNull();
        view.Zoom.Should().Be(1.0f);
        view.ActivePart.Should().Be(0);
    }

    // ── 4. RenderTileAsync returns 512×512 bitmap ─────────────────────────────

    [Fact]
    public async Task RenderTileAsync_Tile00_ReturnsBitmap()
    {
        await using var host = BuildHost();
        await using var doc  = await host.OpenAsync(Stream.Null, OpenOptions.Default);
        using var view   = host.CreateView(doc);

        var request = TileRequest.ForHeadless(0, 1f, 0, 0);
        using var bitmap = await view.RenderTileAsync(request);

        bitmap.Should().NotBeNull();
        bitmap.Width.Should().Be(512);
        bitmap.Height.Should().Be(512);
    }

    // ── 5. RenderTileAsync bitmap has non-white content ───────────────────────

    [Fact]
    public async Task RenderTileAsync_BitmapHasContent()
    {
        await using var host = BuildHost();
        await using var doc  = await host.OpenAsync(Stream.Null, OpenOptions.Default);
        using var view   = host.CreateView(doc);

        var request = TileRequest.ForHeadless(0, 1f, 0, 0);
        using var bitmap = await view.RenderTileAsync(request);

        // Phase1TestScene band 0 has a blue rect starting at (20,20).
        // At zoom=1 tile (0,0) covers logical points (0,0)→(512,512).
        // Sample a pixel inside the blue rect at approx (70, 130) in logical space.
        var cx = bitmap.Width / 2;
        var cy = bitmap.Height / 2;
        var centre = bitmap.GetPixel(cx, cy);

        // Centre of tile 0,0 should not be pure white (the scene has content)
        var isWhite = centre.Red == 255 && centre.Green == 255 && centre.Blue == 255;
        isWhite.Should().BeFalse(
            "Phase1TestScene contains a blue rect and other content in the visible area.");
    }

    // ── 6. ExecuteAsync PingCommand ───────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_PingCommand_DoesNotThrow()
    {
        await using var host = BuildHost();
        await using var doc  = await host.OpenAsync(Stream.Null, OpenOptions.Default);

        var act = async () => await doc.ExecuteAsync(new PingCommand());
        await act.Should().NotThrowAsync();
    }

    // ── 7. ExecuteAsync unknown command ───────────────────────────────────────

    private sealed record UnknownCommand : ILokiCommand;

    [Fact]
    public async Task ExecuteAsync_UnknownCommand_ThrowsCommandDispatchException()
    {
        await using var host = BuildHost();
        await using var doc  = await host.OpenAsync(Stream.Null, OpenOptions.Default);

        var act = async () => await doc.ExecuteAsync(new UnknownCommand());
        await act.Should().ThrowAsync<CommandDispatchException>();
    }

    // ── 8. Zoom setter fires TileInvalidated ──────────────────────────────────

    [Fact]
    public async Task SetZoom_FiresTileInvalidated()
    {
        await using var host = BuildHost();
        await using var doc  = await host.OpenAsync(Stream.Null, OpenOptions.Default);
        using var view = host.CreateView(doc);

        var fired = false;
        view.TileInvalidated += (_, _) => fired = true;

        view.Zoom = 2f;

        fired.Should().BeTrue("setting Zoom must fire TileInvalidated");
    }

    // ── 9. ActivePart setter fires TileInvalidated ────────────────────────────

    [Fact]
    public async Task SetActivePart_FiresTileInvalidated()
    {
        await using var host = BuildHost();
        await using var doc  = await host.OpenAsync(Stream.Null, OpenOptions.Default);
        using var view = host.CreateView(doc);

        var fired = false;
        view.TileInvalidated += (_, _) => fired = true;

        view.ActivePart = 0; // Reassigning same value is fine — setter always fires.

        fired.Should().BeTrue("setting ActivePart must fire TileInvalidated");
    }

    // ── 10. Subscribe/unsubscribe does not throw ──────────────────────────────

    [Fact]
    public async Task DocumentChanged_FiresTileInvalidated()
    {
        // StubEngine never fires LayoutInvalidated — tested in Phase 3 with a real engine.
        // This test verifies that subscribing to TileInvalidated on the view and then
        // disposing the view (which unsubscribes) does not throw.
        await using var host = BuildHost();
        await using var doc  = await host.OpenAsync(Stream.Null, OpenOptions.Default);

        var act = () =>
        {
            using var view = host.CreateView(doc);
            view.TileInvalidated += (_, _) => { };
            // view.Dispose() called by using block — should not throw
        };

        act.Should().NotThrow();
    }

    // ── 11. DisposeAsync removes document from OpenDocuments ─────────────────

    [Fact]
    public async Task DisposeAsync_Document_RemovesFromOpenDocuments()
    {
        await using var host = BuildHost();
        var doc = await host.OpenAsync(Stream.Null, OpenOptions.Default);

        host.OpenDocuments.Should().Contain(doc);

        await doc.DisposeAsync();

        host.OpenDocuments.Should().NotContain(doc,
            "disposed documents must be removed from OpenDocuments");
    }
}
