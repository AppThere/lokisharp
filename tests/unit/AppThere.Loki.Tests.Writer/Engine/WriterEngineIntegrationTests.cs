// LAYER:   AppThere.Loki.Tests.Writer — Tests
// KIND:    Tests
// PURPOSE: Integration tests for WriterEngine (full pipeline including ODF import).
//          Tests the engine lifecycle: InitialiseAsync with Stream.Null (empty doc),
//          InitialiseAsync with simple.fodt (real ODF), and basic rendering queries.
//          Font measurement is performed by a real SkiaFontManager.
//          Tests structural properties only — no pixel comparison.
// DEPENDS: WriterEngine, OdfImporter, LayoutEngine, LayoutCache,
//          SkiaFontManager, NullLokiLogger, simple.fodt corpus
// USED BY: CI test runner
// PHASE:   3
// ADR:     ADR-007, ADR-008, ADR-009

using AppThere.Loki.Format.Odf;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.LokiKit.Host;
using AppThere.Loki.Skia.Fonts;
using AppThere.Loki.Writer.Engine;

namespace AppThere.Loki.Tests.Writer.Engine;

public sealed class WriterEngineIntegrationTests
{
    // ── Fixture helpers ────────────────────────────────────────────────────────

    private static WriterEngine CreateSut()
    {
        var fontManager  = new SkiaFontManager(NullLokiLogger.Instance);
        var odfImporter  = new OdfImporter();
        var layoutEngine = new LayoutEngine(fontManager, NullLokiLogger.Instance);
        return new WriterEngine(fontManager, NullLokiLogger.Instance,
                                odfImporter, layoutEngine, LokiHostOptions.Default);
    }

    private static string CorpusPath(string file) =>
        Path.Combine(AppContext.BaseDirectory, "corpus", "writer", file);

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task InitialiseAsync_NullStream_ProducesEmptyDocument()
    {
        await using var engine = CreateSut();
        await engine.InitialiseAsync(Stream.Null, OpenOptions.Default, default);

        engine.PartCount.Should().BeGreaterThanOrEqualTo(1);
        engine.IsModified.Should().BeFalse();
    }

    [Fact]
    public async Task InitialiseAsync_NullStream_GetPaintSceneReturnsScene()
    {
        await using var engine = CreateSut();
        await engine.InitialiseAsync(Stream.Null, OpenOptions.Default, default);

        var scene = engine.GetPaintScene(0);
        scene.Should().NotBeNull();
    }

    [Fact]
    public async Task InitialiseAsync_NullStream_GetPartReturnsA4()
    {
        await using var engine = CreateSut();
        await engine.InitialiseAsync(Stream.Null, OpenOptions.Default, default);

        var part = engine.GetPart(0);
        part.SizeInPoints.Width.Should().BeApproximately(595.28f, 1f,
            "empty document uses A4 page style");
        part.SizeInPoints.Height.Should().BeApproximately(841.89f, 1f,
            "empty document uses A4 page style");
    }

    [Fact]
    public async Task InitialiseAsync_SimpleFodt_PartCountAtLeastOne()
    {
        var path = CorpusPath("simple.fodt");
        if (!File.Exists(path))
        {
            // Skip if corpus file not copied to output
            return;
        }

        await using var engine = CreateSut();
        await using var stream = File.OpenRead(path);
        await engine.InitialiseAsync(stream, OpenOptions.Default, default);

        engine.PartCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task InitialiseAsync_SimpleFodt_GetPartReturnsLetterPage()
    {
        // simple.fodt is US Letter: 8.5in × 11in = 612pt × 792pt
        var path = CorpusPath("simple.fodt");
        if (!File.Exists(path)) return;

        await using var engine = CreateSut();
        await using var stream = File.OpenRead(path);
        await engine.InitialiseAsync(stream, OpenOptions.Default, default);

        var part = engine.GetPart(0);
        part.SizeInPoints.Width.Should().BeApproximately(612f, 2f,
            "simple.fodt is US Letter 8.5in wide");
        part.SizeInPoints.Height.Should().BeApproximately(792f, 2f,
            "simple.fodt is US Letter 11in tall");
    }

    [Fact]
    public async Task InitialiseAsync_SimpleFodt_PaintSceneHasBands()
    {
        var path = CorpusPath("simple.fodt");
        if (!File.Exists(path)) return;

        await using var engine = CreateSut();
        await using var stream = File.OpenRead(path);
        await engine.InitialiseAsync(stream, OpenOptions.Default, default);

        var scene = engine.GetPaintScene(0);
        scene.Bands.Should().NotBeEmpty(
            "simple.fodt has text content that must produce paint bands");
    }

    [Fact]
    public async Task InitialiseNewAsync_ProducesEmptyDocument()
    {
        await using var engine = CreateSut();
        await engine.InitialiseNewAsync(DocumentKind.Writer, default);

        engine.PartCount.Should().BeGreaterThanOrEqualTo(1);
        engine.GetPart(0).DisplayName.Should().Contain("Page");
    }

    [Fact]
    public async Task CanExecute_AnyCommand_ReturnsFalse()
    {
        await using var engine = CreateSut();
        await engine.InitialiseAsync(Stream.Null, OpenOptions.Default, default);

        engine.CanExecute(Substitute.For<ILokiCommand>()).Should().BeFalse();
    }
}
