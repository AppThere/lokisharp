// LAYER:   AppThere.Loki.Tests.Unit — Tests
// KIND:    Tests
// PURPOSE: Unit tests for LokiColor RGBA value type.
// DEPENDS: LokiColor
// USED BY: CI
// PHASE:   1

using AppThere.Loki.Kernel.Color;
using FluentAssertions;

namespace AppThere.Loki.Tests.Unit.Kernel.Color;

public sealed class LokiColorTests
{
    [Fact]
    public void Black_Always_HasZeroRgbAndFullAlpha()
    {
        LokiColor.Black.R.Should().Be(0f);
        LokiColor.Black.G.Should().Be(0f);
        LokiColor.Black.B.Should().Be(0f);
        LokiColor.Black.A.Should().Be(1f);
    }

    [Fact]
    public void Transparent_Always_HasZeroAlpha()
    {
        LokiColor.Transparent.A.Should().Be(0f);
    }

    [Fact]
    public void FromArgb32_Opaque_ConvertsChannelsCorrectly()
    {
        var c = LokiColor.FromArgb32(255, 255, 0, 0);
        c.R.Should().BeApproximately(1f, 0.005f);
        c.G.Should().BeApproximately(0f, 0.005f);
        c.B.Should().BeApproximately(0f, 0.005f);
        c.A.Should().BeApproximately(1f, 0.005f);
    }

    [Fact]
    public void FromArgb32_ZeroAlpha_ProducesTransparent()
    {
        var c = LokiColor.FromArgb32(0, 0, 0, 0);
        c.A.Should().Be(0f);
    }

    [Fact]
    public void FromHex_SixDigitHex_ParsesAsOpaqueColor()
    {
        var c = LokiColor.FromHex("FF0000");
        c.R8.Should().Be(255);
        c.G8.Should().Be(0);
        c.B8.Should().Be(0);
        c.A8.Should().Be(255);
    }

    [Fact]
    public void FromHex_WithHashPrefix_ParsesCorrectly()
    {
        var c = LokiColor.FromHex("#0000FF");
        c.B8.Should().Be(255);
        c.R8.Should().Be(0);
        c.G8.Should().Be(0);
    }

    [Fact]
    public void FromHex_EightDigitHex_ParsesAlpha()
    {
        var c = LokiColor.FromHex("80FF0000");
        c.A8.Should().Be(0x80);
        c.R8.Should().Be(0xFF);
    }

    [Fact]
    public void FromHex_InvalidLength_ThrowsFormatException()
    {
        var act = () => LokiColor.FromHex("ZZZZZ");
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void ToArgb32_RoundTripsFromArgb32()
    {
        var original = LokiColor.FromArgb32(200, 100, 150, 50);
        var packed   = original.ToArgb32();
        var a = (byte)((packed >> 24) & 0xFF);
        var r = (byte)((packed >> 16) & 0xFF);
        var g = (byte)((packed >> 8)  & 0xFF);
        var b = (byte)( packed        & 0xFF);
        a.Should().Be(200);
        r.Should().Be(100);
        g.Should().Be(150);
        b.Should().Be(50);
    }

    [Fact]
    public void WithAlpha_ChangesOnlyAlpha()
    {
        var c = LokiColor.Red.WithAlpha(0.5f);
        c.R.Should().Be(1f);
        c.G.Should().Be(0f);
        c.B.Should().Be(0f);
        c.A.Should().BeApproximately(0.5f, 0.0001f);
    }

    [Fact]
    public void Lerp_AtZero_ReturnsSourceColor()
    {
        var result = LokiColor.Black.Lerp(LokiColor.White, 0f);
        result.Should().Be(LokiColor.Black);
    }

    [Fact]
    public void Lerp_AtOne_ReturnsTargetColor()
    {
        var result = LokiColor.Black.Lerp(LokiColor.White, 1f);
        result.Should().Be(LokiColor.White);
    }

    [Fact]
    public void Lerp_AtHalf_ReturnsMiddleGray()
    {
        var result = LokiColor.Black.Lerp(LokiColor.White, 0.5f);
        result.R.Should().BeApproximately(0.5f, 0.001f);
        result.G.Should().BeApproximately(0.5f, 0.001f);
        result.B.Should().BeApproximately(0.5f, 0.001f);
    }

    [Fact]
    public void ToString_Always_FormatsAsHexArgb()
    {
        LokiColor.Black.ToString().Should().Be("#FF000000");
    }
}
