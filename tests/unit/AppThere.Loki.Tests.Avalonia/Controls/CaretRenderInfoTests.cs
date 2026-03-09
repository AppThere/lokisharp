using AppThere.Loki.Avalonia.Controls;
using AppThere.Loki.Kernel.Color;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using System.Collections.Generic;
using Xunit;
using NSubstitute;

namespace AppThere.Loki.Tests.Avalonia.Controls;

public class CaretRenderInfoTests
{
    [Fact]
    public void CaretRenderInfo_LocalCaret_IsLocalTrue()
    {
        var info = new CaretRenderInfo(new Rect(0, 0, 2, 20), true, LokiColor.Red, true);
        Assert.True(info.IsLocal);
    }

    [Fact]
    public void CaretRenderInfo_RemoteCaret_IsLocalFalse()
    {
        var info = new CaretRenderInfo(new Rect(0, 0, 2, 20), false, LokiColor.Red, true);
        Assert.False(info.IsLocal);
    }
}
