// LAYER:   AppThere.Loki.Tests.Unit — Tests
// KIND:    Tests
// PURPOSE: Unit tests for LokiException hierarchy and its typed subclasses.
// DEPENDS: LokiException, StorageException, FontLoadException, FontResolutionException
// USED BY: CI
// PHASE:   1

using AppThere.Loki.Kernel.Errors;
using FluentAssertions;

namespace AppThere.Loki.Tests.Unit.Kernel.Errors;

public sealed class LokiExceptionTests
{
    [Fact]
    public void StorageException_WithPath_StoresPath()
    {
        var ex = new StorageException("not found", "/some/path");
        ex.Path.Should().Be("/some/path");
        ex.Message.Should().Be("not found");
    }

    [Fact]
    public void StorageException_WithInner_WrapsInnerException()
    {
        var inner = new IOException("disk error");
        var ex    = new StorageException("storage failed", "/x", inner);
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void StorageException_IsLokiException()
    {
        var ex = new StorageException("msg");
        ex.Should().BeAssignableTo<LokiException>();
    }

    [Fact]
    public void FontLoadException_StoresFamilyName()
    {
        var ex = new FontLoadException("Inter", "file missing");
        ex.FamilyName.Should().Be("Inter");
        ex.Message.Should().Be("file missing");
    }

    [Fact]
    public void FontLoadException_IsLokiException()
    {
        var ex = new FontLoadException("Inter", "msg");
        ex.Should().BeAssignableTo<LokiException>();
    }

    [Fact]
    public void FontResolutionException_StoresFamilyName()
    {
        var ex = new FontResolutionException("Noto", "no match");
        ex.FamilyName.Should().Be("Noto");
        ex.Message.Should().Be("no match");
    }

    [Fact]
    public void FontResolutionException_IsLokiException()
    {
        var ex = new FontResolutionException("Noto", "msg");
        ex.Should().BeAssignableTo<LokiException>();
    }

    [Fact]
    public void StorageException_NullPath_IsAllowed()
    {
        var ex = new StorageException("msg", null);
        ex.Path.Should().BeNull();
    }
}
