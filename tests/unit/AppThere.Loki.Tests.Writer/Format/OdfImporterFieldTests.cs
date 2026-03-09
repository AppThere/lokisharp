using System.Collections.Immutable;
using System.Text;
using AppThere.Loki.Format.Odf;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.Skia.Fonts;
using AppThere.Loki.Writer.Layout;
using AppThere.Loki.Writer.Model.Inlines;
using AppThere.Loki.Writer.Model.Styles;
using Xunit;

namespace AppThere.Loki.Tests.Writer.Format;

public class OdfImporterFieldTests
{
    private class StubFontManager : IFontManager
    {
        public FontDescriptor MatchFont(string family, FontWeight weight, FontSlant slant) => FontDescriptor.Default;
        public IEnumerable<string> GetAvailableFamilies() => [];
        public IReadOnlyList<FontFamilyInfo> GetBundledFamilies() => [];
        public IReadOnlyList<FontFamilyInfo> GetSystemFamilies() => [];
        public bool TryGetTypeface(FontDescriptor descriptor, out ILokiTypeface? typeface) { typeface = null; return false; }
        public ILokiTypeface GetFallbackForScript(UnicodeScript script) => throw new NotImplementedException();
        public bool TryGetVariableAxes(string family, out IReadOnlyList<FontAxis>? axes) { axes = null; return false; }
        public void RegisterEmbedded(string familyName, Stream fontData) { }
        public Task<bool> TryDownloadFamilyAsync(string familyName, CancellationToken ct) => Task.FromResult(false);
    }
    private static string MakeFieldFodt(string fieldElement, string fieldText) =>
        $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <office:document xmlns:office="urn:oasis:names:tc:opendocument:xmlns:office:1.0"
                         xmlns:text="urn:oasis:names:tc:opendocument:xmlns:text:1.0"
                         xmlns:style="urn:oasis:names:tc:opendocument:xmlns:style:1.0"
                         office:version="1.3">
          <office:body>
            <office:text>
              <text:p text:style-name="Standard">
                <{fieldElement} xmlns:text="urn:oasis:names:tc:opendocument:xmlns:text:1.0">{fieldText}</{fieldElement}>
              </text:p>
            </office:text>
          </office:body>
        </office:document>
        """;

    [Fact]
    public async Task Import_FieldDocument_FieldNodesPresent()
    {
        var xml = MakeFieldFodt("text:title", "My Title");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        
        var importer = new OdfImporter();
        var doc = await importer.ImportAsync(stream, isFlat: true, new SkiaFontManager(NullLokiLogger.Instance), NullLokiLogger.Instance, CancellationToken.None);
        
        Assert.Single(doc.Body);
        var para = doc.Body[0] as AppThere.Loki.Writer.Model.ParagraphNode;
        Assert.NotNull(para);
        
        Assert.Single(para.Inlines);
        Assert.IsType<FieldNode>(para.Inlines[0]);
    }

    [Fact]
    public async Task Import_TitleField_HasCorrectKind()
    {
        var xml = MakeFieldFodt("text:title", "My Title");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        
        var importer = new OdfImporter();
        var doc = await importer.ImportAsync(stream, isFlat: true, new SkiaFontManager(NullLokiLogger.Instance), NullLokiLogger.Instance, CancellationToken.None);
        
        var para = doc.Body[0] as AppThere.Loki.Writer.Model.ParagraphNode;
        var field = para!.Inlines[0] as FieldNode;
        
        Assert.NotNull(field);
        Assert.Equal(FieldKind.Title, field.Kind);
    }

    [Fact]
    public async Task Import_FieldNode_StaticTextPreserved()
    {
        var xml = MakeFieldFodt("text:title", "My Title");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        
        var importer = new OdfImporter();
        var doc = await importer.ImportAsync(stream, isFlat: true, new SkiaFontManager(NullLokiLogger.Instance), NullLokiLogger.Instance, CancellationToken.None);
        
        var para = doc.Body[0] as AppThere.Loki.Writer.Model.ParagraphNode;
        var field = para!.Inlines[0] as FieldNode;
        
        Assert.NotNull(field);
        Assert.Equal("My Title", field.StaticText);
    }

    [Fact]
    public void Import_FieldNode_RenderedByMeasurer()
    {
        var fontManager = new SkiaFontManager(NullLokiLogger.Instance);
        var measurer = new InlineMeasurer(fontManager, NullLokiLogger.Instance);
        
        var fieldNode = new FieldNode(FieldKind.Title, "Testing Title", CharacterStyle.Default, null);
        var paraNode = new AppThere.Loki.Writer.Model.ParagraphNode(
            ImmutableList.Create<InlineNode>(fieldNode), 
            ParagraphStyle.Default, 
            null);
            
        var measured = measurer.Measure(paraNode, 1000f);
        
        // Ensure that BoxItems were generated from the StaticText (length > 0)
        Assert.Contains(measured.Items, item => item is BoxItem);
    }
}
