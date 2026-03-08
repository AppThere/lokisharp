// LAYER:   AppThere.Loki.Format.Odf — ODF Import
// KIND:    Implementation
// PURPOSE: Implements IOdfImporter — the three-pass ODF import pipeline.
//          Pass 1+2: StyleParser builds StyleRegistry from style sections.
//          Pass 3:   BodyParser walks office:text and resolves cascaded styles.
//          Supports both FODT (flat XML) and ODT (ZIP container) streams.
//          Extracts page dimensions from the first master page layout.
//          Throws LokiOpenException on structural failures; logs and continues
//          on content errors.
// DEPENDS: IOdfImporter, StyleParser, BodyParser, StyleResolver, OdfNamespaces,
//          OdfUnits, LokiDocument, PageStyle, LokiOpenException,
//          IFontManager, ILokiLogger
// USED BY: WriterEngine.InitialiseAsync
// PHASE:   3
// ADR:     ADR-009

using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.LokiKit.Errors;
using AppThere.Loki.Skia.Fonts;
using AppThere.Loki.Writer.Model;
using AppThere.Loki.Writer.Model.Styles;

namespace AppThere.Loki.Format.Odf;

internal sealed class OdfImporter : IOdfImporter
{
    public Task<LokiDocument> ImportAsync(
        Stream            source,
        bool              isFlat,
        IFontManager      fontManager,
        ILokiLogger       logger,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.Run(() => Import(source, isFlat, fontManager, logger), ct);
    }

    // ── Core import ───────────────────────────────────────────────────────────

    private static LokiDocument Import(
        Stream       source,
        bool         isFlat,
        IFontManager fontManager,
        ILokiLogger  logger)
    {
        try
        {
            var (contentDoc, stylesDoc) = LoadDocuments(source, isFlat);

            var styleParser = new StyleParser(logger);
            var registry    = styleParser.ParseStyleRegistry(contentDoc, stylesDoc);

            var resolver   = new StyleResolver(registry, fontManager, logger);
            var bodyParser = new BodyParser(resolver, logger);

            var officeText = contentDoc.Root
                ?.Element(OdfNamespaces.Office + "body")
                ?.Element(OdfNamespaces.Office + "text")
                ?? throw new LokiOpenException(
                    "ODF document missing office:body/office:text");

            var body      = bodyParser.ParseBody(officeText);
            var pageStyle = ParsePageStyle(contentDoc);
            var (title, language) = ParseMetadata(contentDoc);

            return new LokiDocument(body, registry, pageStyle, title, language);
        }
        catch (LokiOpenException)
        {
            throw;
        }
        catch (XmlException ex)
        {
            throw new LokiOpenException($"Invalid ODF document: {ex.Message}", ex);
        }
        catch (InvalidDataException ex)
        {
            throw new LokiOpenException($"Invalid ODF document: {ex.Message}", ex);
        }
    }

    // ── XML loading ───────────────────────────────────────────────────────────

    private static (XDocument Content, XDocument? Styles) LoadDocuments(
        Stream source, bool isFlat)
    {
        if (isFlat)
            return (XDocument.Load(source), null);

        using var zip = new ZipArchive(source, ZipArchiveMode.Read, leaveOpen: true);

        var contentEntry = zip.GetEntry("content.xml")
            ?? throw new LokiOpenException("ODT archive missing content.xml");

        using var contentStream = contentEntry.Open();
        var contentDoc = XDocument.Load(contentStream);

        XDocument? stylesDoc = null;
        var stylesEntry = zip.GetEntry("styles.xml");
        if (stylesEntry is not null)
        {
            using var stylesStream = stylesEntry.Open();
            stylesDoc = XDocument.Load(stylesStream);
        }

        return (contentDoc, stylesDoc);
    }

    // ── Page style ────────────────────────────────────────────────────────────

    private static PageStyle ParsePageStyle(XDocument doc)
    {
        var masterStyles = doc.Root?.Element(OdfNamespaces.Office + "master-styles");
        var masterPage   = masterStyles
            ?.Elements(OdfNamespaces.Style + "master-page")
            .FirstOrDefault(e =>
                (string?)e.Attribute(OdfNamespaces.Style + "name") == "Standard");

        var layoutName = (string?)masterPage
            ?.Attribute(OdfNamespaces.Style + "page-layout-name");

        if (layoutName is null) return PageStyle.A4;

        var pageLayout = doc.Root
            ?.Element(OdfNamespaces.Office + "automatic-styles")
            ?.Elements(OdfNamespaces.Style + "page-layout")
            .FirstOrDefault(e =>
                (string?)e.Attribute(OdfNamespaces.Style + "name") == layoutName);

        var props = pageLayout?.Element(OdfNamespaces.Style + "page-layout-properties");
        if (props is null) return PageStyle.A4;

        return new PageStyle(
            WidthPts:        Len(props, "page-width",    PageStyle.A4.WidthPts),
            HeightPts:       Len(props, "page-height",   PageStyle.A4.HeightPts),
            MarginTopPts:    Len(props, "margin-top",    PageStyle.A4.MarginTopPts),
            MarginBottomPts: Len(props, "margin-bottom", PageStyle.A4.MarginBottomPts),
            MarginStartPts:  Len(props, "margin-left",   PageStyle.A4.MarginStartPts),
            MarginEndPts:    Len(props, "margin-right",  PageStyle.A4.MarginEndPts));
    }

    private static float Len(XElement el, string attr, float fallback)
    {
        var v = (string?)el.Attribute(OdfNamespaces.FoText + attr);
        return v is not null ? OdfUnits.ToPoints(v, 12f) : fallback;
    }

    // ── Metadata ──────────────────────────────────────────────────────────────

    private static (string? Title, string? Language) ParseMetadata(XDocument doc)
    {
        XNamespace dc   = "http://purl.org/dc/elements/1.1/";
        var meta        = doc.Root?.Element(OdfNamespaces.Office + "meta");
        var title       = (string?)meta?.Element(dc + "title");
        var language    = (string?)meta?.Element(dc + "language");
        return (title, language);
    }
}
