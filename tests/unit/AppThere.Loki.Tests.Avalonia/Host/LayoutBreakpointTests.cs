// LAYER:   AppThere.Loki.Tests.Avalonia — Tests
// KIND:    Tests
// PURPOSE: Verifies LayoutBreakpointResolver.Resolve() and NearestImplemented()
//          for all five breakpoints and boundary widths.
//          Pure logic tests — no Avalonia platform initialisation required.
// DEPENDS: LayoutBreakpointResolver, FluentAssertions
// USED BY: CI
// PHASE:   4

using FluentAssertions;
using AppThere.Loki.Avalonia.Host;

namespace AppThere.Loki.Tests.Avalonia.Host;

public sealed class LayoutBreakpointTests
{
    // ── Resolve ───────────────────────────────────────────────────────────────

    [Fact]
    public void Resolve_Width0_ReturnsPhone()
        => LayoutBreakpointResolver.Resolve(0).Should().Be(LayoutBreakpoint.Phone);

    [Fact]
    public void Resolve_Width599_ReturnsPhone()
        => LayoutBreakpointResolver.Resolve(599).Should().Be(LayoutBreakpoint.Phone);

    [Fact]
    public void Resolve_Width600_ReturnsCompact()
        => LayoutBreakpointResolver.Resolve(600).Should().Be(LayoutBreakpoint.Compact);

    [Fact]
    public void Resolve_Width900_ReturnsNormal()
        => LayoutBreakpointResolver.Resolve(900).Should().Be(LayoutBreakpoint.Normal);

    [Fact]
    public void Resolve_Width1200_ReturnsWide()
        => LayoutBreakpointResolver.Resolve(1200).Should().Be(LayoutBreakpoint.Wide);

    [Fact]
    public void Resolve_Width1800_ReturnsUltraWide()
        => LayoutBreakpointResolver.Resolve(1800).Should().Be(LayoutBreakpoint.UltraWide);

    // ── NearestImplemented ────────────────────────────────────────────────────

    [Fact]
    public void NearestImplemented_Phone_ReturnsPhone()
        => LayoutBreakpointResolver.NearestImplemented(LayoutBreakpoint.Phone)
            .Should().Be(LayoutBreakpoint.Phone);

    [Fact]
    public void NearestImplemented_Compact_ReturnsPhone()
        => LayoutBreakpointResolver.NearestImplemented(LayoutBreakpoint.Compact)
            .Should().Be(LayoutBreakpoint.Phone);

    [Fact]
    public void NearestImplemented_Normal_ReturnsNormal()
        => LayoutBreakpointResolver.NearestImplemented(LayoutBreakpoint.Normal)
            .Should().Be(LayoutBreakpoint.Normal);

    [Fact]
    public void NearestImplemented_Wide_ReturnsNormal()
        => LayoutBreakpointResolver.NearestImplemented(LayoutBreakpoint.Wide)
            .Should().Be(LayoutBreakpoint.Normal);

    [Fact]
    public void NearestImplemented_UltraWide_ReturnsNormal()
        => LayoutBreakpointResolver.NearestImplemented(LayoutBreakpoint.UltraWide)
            .Should().Be(LayoutBreakpoint.Normal);
}
