// LAYER:   AppThere.Loki.Tests.Unit — Tests
// KIND:    Tests
// PURPOSE: Unit tests for Result<T,E> discriminated union.
// DEPENDS: Result
// USED BY: CI
// PHASE:   1

using AppThere.Loki.Kernel.Errors;
using FluentAssertions;

namespace AppThere.Loki.Tests.Unit.Kernel.Errors;

public sealed class ResultTests
{
    [Fact]
    public void Ok_Value_IsOkReturnsTrue()
    {
        var r = Result<int, string>.Ok(42);
        r.IsOk.Should().BeTrue();
        r.IsError.Should().BeFalse();
    }

    [Fact]
    public void Ok_Value_ReturnsStoredValue()
    {
        var r = Result<int, string>.Ok(42);
        r.Value.Should().Be(42);
    }

    [Fact]
    public void Ok_AccessingError_ThrowsInvalidOperationException()
    {
        var r = Result<int, string>.Ok(42);
        var act = () => r.Error;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Fail_Error_IsErrorReturnsTrue()
    {
        var r = Result<int, string>.Fail("oops");
        r.IsError.Should().BeTrue();
        r.IsOk.Should().BeFalse();
    }

    [Fact]
    public void Fail_Error_ReturnsStoredError()
    {
        var r = Result<int, string>.Fail("oops");
        r.Error.Should().Be("oops");
    }

    [Fact]
    public void Fail_AccessingValue_ThrowsInvalidOperationException()
    {
        var r = Result<int, string>.Fail("oops");
        var act = () => r.Value;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Map_OkResult_TransformsValue()
    {
        var r      = Result<int, string>.Ok(5);
        var mapped = r.Map(x => x * 2);
        mapped.IsOk.Should().BeTrue();
        mapped.Value.Should().Be(10);
    }

    [Fact]
    public void Map_ErrorResult_PropagatesErrorUnchanged()
    {
        var r      = Result<int, string>.Fail("err");
        var mapped = r.Map(x => x * 2);
        mapped.IsError.Should().BeTrue();
        mapped.Error.Should().Be("err");
    }

    [Fact]
    public void MapError_ErrorResult_TransformsError()
    {
        var r      = Result<int, string>.Fail("err");
        var mapped = r.MapError(e => e.ToUpper());
        mapped.IsError.Should().BeTrue();
        mapped.Error.Should().Be("ERR");
    }

    [Fact]
    public void MapError_OkResult_PropagatesValueUnchanged()
    {
        var r      = Result<int, string>.Ok(99);
        var mapped = r.MapError(e => e.ToUpper());
        mapped.IsOk.Should().BeTrue();
        mapped.Value.Should().Be(99);
    }

    [Fact]
    public void ValueOrDefault_OkResult_ReturnsValue()
    {
        var r = Result<int, string>.Ok(7);
        r.ValueOrDefault(0).Should().Be(7);
    }

    [Fact]
    public void ValueOrDefault_ErrorResult_ReturnsFallback()
    {
        var r = Result<int, string>.Fail("x");
        r.ValueOrDefault(42).Should().Be(42);
    }

    [Fact]
    public void ToString_OkResult_IncludesValue()
    {
        Result<int, string>.Ok(5).ToString().Should().Be("Ok(5)");
    }

    [Fact]
    public void ToString_ErrorResult_IncludesError()
    {
        Result<int, string>.Fail("bad").ToString().Should().Be("Fail(bad)");
    }
}
