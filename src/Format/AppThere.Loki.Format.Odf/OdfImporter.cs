// LAYER:   AppThere.Loki.Format.Odf — ODF Import
// KIND:    Implementation
// PURPOSE: Implements IOdfImporter. Reads an ODT/FODT stream into a
//          LokiDocument by delegating to OdfDocumentParser.
//          FODT (isFlat=true): loads the stream as a single XDocument.
//          ODT  (isFlat=false): opens as a ZIP archive, reads content.xml
//          (and optionally styles.xml) then delegates to OdfDocumentParser.
//          Throws LokiOpenException on structural errors (e.g. not XML/ZIP).
// DEPENDS: OdfDocumentParser, IOdfImporter, LokiDocument, IFontManager, ILokiLogger
// USED BY: WriterEngine.InitialiseAsync
// PHASE:   3
// ADR:     ADR-009

using System.IO.Compression;
using System.Xml.Linq;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.Skia.Fonts;
using AppThere.Loki.Writer.Engine;
using AppThere.Loki.Writer.Model;

namespace AppThere.Loki.Format.Odf;

/// <summary>
/// Production implementation of IOdfImporter (defined in AppThere.Loki.Writer.Engine).
/// Registered in DI by LokiHostBuilderExtensions.UseWriterEngine().
/// </summary>
public sealed class OdfImporter : AppThere.Loki.Writer.Engine.IOdfImporter
{
    public async Task<LokiDocument> ImportAsync(
        Stream            source,
        bool              isFlat,
        IFontManager      fontManager,
        ILokiLogger       logger,
        CancellationToken ct = default)
    {
        try
        {
            if (isFlat)
                return await ParseFlatAsync(source, logger, ct).ConfigureAwait(false);
            else
                return await ParseZipAsync(source, logger, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"ODF import failed: {ex.Message}", ex);
        }
    }

    // ── Flat ODF (FODT) ───────────────────────────────────────────────────────

    private static Task<LokiDocument> ParseFlatAsync(
        Stream source, ILokiLogger logger, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        XDocument doc;
        try
        {
            doc = XDocument.Load(source);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("FODT file is not valid XML.", ex);
        }

        var result = OdfDocumentParser.Parse(doc, logger);
        return Task.FromResult(result);
    }

    // ── ZIP ODF (ODT) ─────────────────────────────────────────────────────────

    private static async Task<LokiDocument> ParseZipAsync(
        Stream source, ILokiLogger logger, CancellationToken ct)
    {
        // Buffer the stream so ZipArchive can read it
        var ms = new MemoryStream();
        await source.CopyToAsync(ms, ct).ConfigureAwait(false);
        ms.Seek(0, SeekOrigin.Begin);

        using var zip = new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: false);

        // Merge styles.xml (if present) into content.xml for a single-doc parse
        var contentEntry = zip.GetEntry("content.xml")
            ?? throw new InvalidOperationException("ODT ZIP does not contain content.xml.");

        XDocument merged;
        using (var contentStream = contentEntry.Open())
        {
            try
            {
                merged = XDocument.Load(contentStream);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("ODT content.xml is not valid XML.", ex);
            }
        }

        // Copy named styles from styles.xml into the merged document
        var stylesEntry = zip.GetEntry("styles.xml");
        if (stylesEntry is not null)
        {
            using var stylesStream = stylesEntry.Open();
            try
            {
                var stylesDoc = XDocument.Load(stylesStream);
                MergeNamedStyles(merged, stylesDoc);
            }
            catch { /* Non-fatal: skip if styles.xml is unreadable */ }
        }

        return OdfDocumentParser.Parse(merged, logger);
    }

    private static void MergeNamedStyles(XDocument target, XDocument styles)
    {
        // Inject the office:styles element from styles.xml into the content doc
        XNamespace ns = "urn:oasis:names:tc:opendocument:xmlns:office:1.0";
        var targetRoot  = target.Root;
        var stylesNode  = styles.Root?.Element(ns + "styles");
        if (targetRoot is null || stylesNode is null) return;

        // Replace or add office:styles in target
        var existing = targetRoot.Element(ns + "styles");
        if (existing is not null)
            existing.ReplaceWith(new XElement(stylesNode));
        else
            targetRoot.AddFirst(new XElement(stylesNode));
    }
}
