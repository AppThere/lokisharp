using AppThere.Loki.Avalonia.Cache;
using Xunit;

namespace AppThere.Loki.Tests.Avalonia.Cache;

public class TileGridMathMultiPageTests
{
    [Fact]
    public void CanvasHeight_OnePageNoGap_EqualPageHeight()
    {
        float height = TileGridMath.CanvasHeightPts(1, 842f, 0f);
        Assert.Equal(842f, height);
    }

    [Fact]
    public void CanvasHeight_TwoPages_IncludesGap()
    {
        float height = TileGridMath.CanvasHeightPts(2, 842f, 16f);
        Assert.Equal(1716f, height);
    }

    [Fact]
    public void PageForCanvasY_FirstPage_ReturnsZero()
    {
        int page = TileGridMath.PageForCanvasY(100f, 842f, 16f, 2);
        Assert.Equal(0, page);
    }

    [Fact]
    public void PageForCanvasY_SecondPage_ReturnsOne()
    {
        int page = TileGridMath.PageForCanvasY(900f, 842f, 16f, 2);
        Assert.Equal(1, page);
    }

    [Fact]
    public void PageForCanvasY_InGap_ReturnsPrecedingPage()
    {
        int page = TileGridMath.PageForCanvasY(845f, 842f, 16f, 2);
        Assert.Equal(0, page);
    }

    [Fact]
    public void PageForCanvasY_Clamps_ToPageCount()
    {
        int page = TileGridMath.PageForCanvasY(9999f, 842f, 16f, 2);
        Assert.Equal(1, page);
    }

    [Fact]
    public void LocalYOnPage_PageZero_EqualCanvasY()
    {
        float localY = TileGridMath.LocalYOnPage(100f, 0, 842f, 16f);
        Assert.Equal(100f, localY);
    }

    [Fact]
    public void LocalYOnPage_PageOne_SubtractsOffset()
    {
        float localY = TileGridMath.LocalYOnPage(900f, 1, 842f, 16f);
        Assert.Equal(42f, localY);
    }

    [Fact]
    public void IsInPageGap_GapRegion_ReturnsTrue()
    {
        bool inGap = TileGridMath.IsInPageGap(843f, 857f, 0, 842f, 16f);
        Assert.True(inGap);
    }

    [Fact]
    public void IsInPageGap_PageContent_ReturnsFalse()
    {
        bool inGap = TileGridMath.IsInPageGap(100f, 200f, 0, 842f, 16f);
        Assert.False(inGap);
    }
}
