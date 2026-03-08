// LAYER:   AppThere.Loki.Tests.Writer — Tests
// KIND:    Tests
// PURPOSE: Unit tests for OdfUnits.ToPoints — verifies all supported unit
//          conversions, inherited-font-size scaling, and graceful failure cases.
//          Does NOT test style cascade logic (covered by StyleResolverTests).
// DEPENDS: OdfUnits
// USED BY: CI unit test run
// PHASE:   3
// ADR:     ADR-009

using AppThere.Loki.Writer.Model;
using FluentAssertions;
using Xunit;

namespace AppThere.Loki.Tests.Writer.Model;

public sealed class OdfUnitsTests
{
    [Fact]
    public void ToPoints_Pt_ReturnsValue()
    {
        OdfUnits.ToPoints("12pt", 12f).Should().BeApproximately(12f, 0.001f);
    }

    [Fact]
    public void ToPoints_Cm_Converts()
    {
        OdfUnits.ToPoints("1cm", 12f).Should().BeApproximately(28.3465f, 0.001f);
    }

    [Fact]
    public void ToPoints_Mm_Converts()
    {
        OdfUnits.ToPoints("10mm", 12f).Should().BeApproximately(28.3465f, 0.001f);
    }

    [Fact]
    public void ToPoints_In_Converts()
    {
        OdfUnits.ToPoints("1in", 12f).Should().BeApproximately(72f, 0.001f);
    }

    [Fact]
    public void ToPoints_Pc_Converts()
    {
        OdfUnits.ToPoints("1pc", 12f).Should().BeApproximately(12f, 0.001f);
    }

    [Fact]
    public void ToPoints_Em_UsesInherited()
    {
        OdfUnits.ToPoints("2em", 10f).Should().BeApproximately(20f, 0.001f);
    }

    [Fact]
    public void ToPoints_Percent_UsesInherited()
    {
        OdfUnits.ToPoints("150%", 12f).Should().BeApproximately(18f, 0.001f);
    }

    [Fact]
    public void ToPoints_UnknownUnit_ReturnsZero()
    {
        OdfUnits.ToPoints("3xx", 12f).Should().Be(0f);
    }

    [Fact]
    public void ToPoints_EmptyString_ReturnsZero()
    {
        OdfUnits.ToPoints("", 12f).Should().Be(0f);
    }

    [Fact]
    public void ToPoints_Whitespace_Stripped()
    {
        OdfUnits.ToPoints(" 12pt ", 12f).Should().BeApproximately(12f, 0.001f);
    }
}
