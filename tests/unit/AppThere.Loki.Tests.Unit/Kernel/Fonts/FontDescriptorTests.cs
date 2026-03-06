// LAYER:   AppThere.Loki.Tests.Unit — Tests
// KIND:    Tests
// PURPOSE: Unit tests for FontDescriptor and related font enums.
// DEPENDS: FontDescriptor, FontWeight, FontSlant, FontStretch
// USED BY: CI
// PHASE:   1

using AppThere.Loki.Kernel.Fonts;
using FluentAssertions;

namespace AppThere.Loki.Tests.Unit.Kernel.Fonts;

public sealed class FontDescriptorTests
{
    [Fact]
    public void Default_Always_HasInterFamilyAndTwelvePointSize()
    {
        FontDescriptor.Default.FamilyName.Should().Be("Inter");
        FontDescriptor.Default.SizeInPoints.Should().Be(12f);
        FontDescriptor.Default.Weight.Should().Be(FontWeight.Regular);
        FontDescriptor.Default.Slant.Should().Be(FontSlant.Upright);
        FontDescriptor.Default.Stretch.Should().Be(FontStretch.Normal);
    }

    [Fact]
    public void Constructor_AllParameters_AssignsEachField()
    {
        var axes = new Dictionary<string, float> { ["wght"] = 450f };
        var fd = new FontDescriptor(
            "Newsreader",
            FontWeight.Bold,
            FontSlant.Italic,
            FontStretch.Condensed,
            14f,
            axes);

        fd.FamilyName.Should().Be("Newsreader");
        fd.Weight.Should().Be(FontWeight.Bold);
        fd.Slant.Should().Be(FontSlant.Italic);
        fd.Stretch.Should().Be(FontStretch.Condensed);
        fd.SizeInPoints.Should().Be(14f);
        fd.VariableAxes.Should().ContainKey("wght");
    }

    [Fact]
    public void Default_VariableAxes_IsNull()
    {
        FontDescriptor.Default.VariableAxes.Should().BeNull();
    }

    [Fact]
    public void FontWeight_Regular_HasNumericValue400()
    {
        ((int)FontWeight.Regular).Should().Be(400);
    }

    [Fact]
    public void FontWeight_Bold_HasNumericValue700()
    {
        ((int)FontWeight.Bold).Should().Be(700);
    }

    [Fact]
    public void FontStretch_Normal_HasNumericValue100()
    {
        ((int)FontStretch.Normal).Should().Be(100);
    }

    [Fact]
    public void FontStretch_Condensed_HasNumericValue75()
    {
        ((int)FontStretch.Condensed).Should().Be(75);
    }

    [Fact]
    public void FontSlant_HasExpectedValues()
    {
        Enum.GetValues<FontSlant>().Should().Contain(FontSlant.Upright)
            .And.Contain(FontSlant.Italic)
            .And.Contain(FontSlant.Oblique);
    }

    [Fact]
    public void Equality_SameDescriptors_AreEqual()
    {
        var a = new FontDescriptor("Inter", FontWeight.Regular);
        var b = new FontDescriptor("Inter", FontWeight.Regular);
        a.Should().Be(b);
    }

    [Fact]
    public void Equality_DifferentWeight_AreNotEqual()
    {
        var a = new FontDescriptor("Inter", FontWeight.Regular);
        var b = new FontDescriptor("Inter", FontWeight.Bold);
        a.Should().NotBe(b);
    }
}
