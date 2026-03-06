// LAYER:   AppThere.Loki.Tests.Unit — Tests
// KIND:    Tests
// PURPOSE: Unit tests for NullLokiLogger no-op implementation.
// DEPENDS: NullLokiLogger, ILokiLogger
// USED BY: CI
// PHASE:   1

using AppThere.Loki.Kernel.Logging;
using FluentAssertions;

namespace AppThere.Loki.Tests.Unit.Kernel.Logging;

public sealed class NullLokiLoggerTests
{
    [Fact]
    public void Instance_Always_ReturnsSingletonInstance()
    {
        NullLokiLogger.Instance.Should().BeSameAs(NullLokiLogger.Instance);
    }

    [Fact]
    public void Instance_IsILokiLogger()
    {
        NullLokiLogger.Instance.Should().BeAssignableTo<ILokiLogger>();
    }

    [Fact]
    public void IsDebugEnabled_Always_ReturnsFalse()
    {
        NullLokiLogger.Instance.IsDebugEnabled.Should().BeFalse();
    }

    [Fact]
    public void Debug_Always_DoesNotThrow()
    {
        var act = () => NullLokiLogger.Instance.Debug("msg {0}", "arg");
        act.Should().NotThrow();
    }

    [Fact]
    public void Info_Always_DoesNotThrow()
    {
        var act = () => NullLokiLogger.Instance.Info("info message");
        act.Should().NotThrow();
    }

    [Fact]
    public void Warn_Always_DoesNotThrow()
    {
        var act = () => NullLokiLogger.Instance.Warn("warn message");
        act.Should().NotThrow();
    }

    [Fact]
    public void Error_WithException_DoesNotThrow()
    {
        var act = () => NullLokiLogger.Instance.Error("error", new Exception("test"));
        act.Should().NotThrow();
    }

    [Fact]
    public void Error_WithoutException_DoesNotThrow()
    {
        var act = () => NullLokiLogger.Instance.Error("error without ex");
        act.Should().NotThrow();
    }
}
