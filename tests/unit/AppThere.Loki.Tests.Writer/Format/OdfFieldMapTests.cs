using AppThere.Loki.Format.Odf;
using AppThere.Loki.Writer.Model.Inlines;
using Xunit;

namespace AppThere.Loki.Tests.Writer.Format;

public class OdfFieldMapTests
{
    [Fact]
    public void Resolve_Title_ReturnsTitleKind()
    {
        Assert.Equal(FieldKind.Title, OdfFieldMap.Resolve("title"));
    }

    [Fact]
    public void Resolve_PageNumber_ReturnsPageNumberKind()
    {
        Assert.Equal(FieldKind.PageNumber, OdfFieldMap.Resolve("page-number"));
    }

    [Fact]
    public void Resolve_UnknownElement_ReturnsUnknown()
    {
        Assert.Equal(FieldKind.Unknown, OdfFieldMap.Resolve("some-unknown-field"));
    }

    [Fact]
    public void Resolve_AllMappedElements_NoUnknown()
    {
        var keys = new[]
        {
            "title", "description", "subject", "initial-creator", "author-name", 
            "page-number", "page-count", "date", "time", "file-name", "chapter"
        };
        
        foreach (var key in keys)
        {
            var kind = OdfFieldMap.Resolve(key);
            Assert.NotEqual(FieldKind.Unknown, kind);
        }
    }
}
