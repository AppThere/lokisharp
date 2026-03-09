// LAYER:   AppThere.Loki.Tests.Avalonia — Tests
// KIND:    Tests
// PURPOSE: Verifies LokiMainWindow constructor, layout breakpoint switching,
//          and title initialisation using Avalonia headless infrastructure.
//          Uses the internal ApplyBreakpointForWidth() method (exposed via
//          InternalsVisibleTo) to simulate resize without a running event loop.
// DEPENDS: LokiMainWindow, ILokiHost, TileCacheOptions, AvaloniaTestFixture,
//          NSubstitute, FluentAssertions
// USED BY: CI
// PHASE:   4

using Avalonia.Controls;
using FluentAssertions;
using NSubstitute;
using AppThere.Loki.Avalonia.Cache;
using AppThere.Loki.Avalonia.Host;
using AppThere.Loki.LokiKit.Host;

namespace AppThere.Loki.Tests.Avalonia.Host;

[Collection("AvaloniaHost")]
public sealed class LokiMainWindowTests : IClassFixture<AvaloniaTestFixture>
{
    private static ILokiHost BuildMockHost() => Substitute.For<ILokiHost>();

    // ── Constructor_SetsTitle ─────────────────────────────────────────────────

    [Fact(Skip = "Avalonia Window instantiation fails on xUnit parallel background threads. TODO: Add [AvaloniaFact].")]
    public void Constructor_SetsTitle()
    {
        var window = new LokiMainWindow(BuildMockHost(), TileCacheOptions.Desktop);
        window.Title.Should().Be("AppThere Loki");
    }

    // ── Constructor_DefaultSize_IsNormalBreakpoint ────────────────────────────

    [Fact(Skip = "Avalonia Window instantiation fails on xUnit parallel background threads. TODO: Add [AvaloniaFact].")]
    public void Constructor_DefaultSize_IsNormalBreakpoint()
    {
        // Width=1024 → LayoutBreakpoint.Normal → ApplyNormalLayout → DockPanel root.
        var window = new LokiMainWindow(BuildMockHost(), TileCacheOptions.Desktop);
        window.Content.Should().BeOfType<DockPanel>(
            "Normal layout at 1024 DIPs must set Content to a DockPanel");
    }

    // ── Constructor_NarrowWidth_IsPhoneBreakpoint ─────────────────────────────

    [Fact(Skip = "Avalonia Window instantiation fails on xUnit parallel background threads. TODO: Add [AvaloniaFact].")]
    public void Constructor_NarrowWidth_IsPhoneBreakpoint()
    {
        // Start at Normal (1024 DIPs), then simulate resize to 400 DIPs.
        var window = new LokiMainWindow(BuildMockHost(), TileCacheOptions.Desktop);
        window.ApplyBreakpointForWidth(400);
        window.Content.Should().BeOfType<Grid>(
            "Phone layout at 400 DIPs must set Content to a Grid");
    }

    // ── SizeChanged_CrossesBreakpoint_SwitchesLayout ──────────────────────────

    [Fact(Skip = "Avalonia Window instantiation fails on xUnit parallel background threads. TODO: Add [AvaloniaFact].")]
    public void SizeChanged_CrossesBreakpoint_SwitchesLayout()
    {
        var window = new LokiMainWindow(BuildMockHost(), TileCacheOptions.Desktop);

        // Initial: Normal at 1024.
        window.Content.Should().BeOfType<DockPanel>("starts at Normal layout");

        // Resize to 500 DIPs → Phone.
        window.ApplyBreakpointForWidth(500);
        window.Content.Should().BeOfType<Grid>("500 DIPs activates Phone layout");

        // Resize back to 1000 DIPs → Normal.
        window.ApplyBreakpointForWidth(1000);
        window.Content.Should().BeOfType<DockPanel>("1000 DIPs restores Normal layout");
    }
}

[CollectionDefinition("AvaloniaHost")]
public sealed class AvaloniaHostCollection : ICollectionFixture<AvaloniaTestFixture> { }
