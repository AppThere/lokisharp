// LAYER:   AppThere.Loki.LokiKit — Hourglass Waist
// KIND:    Value types
// PURPOSE: Options passed to ILokiHost.OpenAsync, and the DocumentKind enum
//          identifying which engine handles a document.
// DEPENDS: —
// USED BY: ILokiHost, ILokiDocument
// PHASE:   2

namespace AppThere.Loki.LokiKit.Host;

/// <summary>Options governing how ILokiHost.OpenAsync behaves.</summary>
public sealed record OpenOptions
{
    /// <summary>
    /// Hint to the format detector. If null, format is inferred from stream content.
    /// </summary>
    public DocumentFormat? FormatHint { get; init; }

    /// <summary>
    /// If true, the document is opened read-only. Save and ExecuteAsync
    /// (for mutating commands) will throw InvalidOperationException.
    /// </summary>
    public bool ReadOnly { get; init; }

    public static readonly OpenOptions Default = new();
}

/// <summary>Format of the source stream.</summary>
public enum DocumentFormat
{
    /// <summary>Auto-detect from stream content (default).</summary>
    Auto,
    Odt,
    Docx,
    Ods,
    Xlsx,
    Odp,
    Pptx,
    Odg,
    Svg,
    Markdown,
    Csv,
}

/// <summary>Which engine handles a document.</summary>
public enum DocumentKind
{
    Writer,   // Text documents — ODT, DOCX, MD
    Calc,     // Spreadsheets — ODS, XLSX, CSV
    Draw,     // Vector drawings — ODG, SVG
    Impress,  // Slide decks — ODP, PPTX
}

/// <summary>Format to write when saving.</summary>
public enum SaveFormat
{
    /// <summary>Save in the document's native format.</summary>
    Native,
    Odt,
    Docx,
    Ods,
    Xlsx,
    Odp,
    Pptx,
    PdfA2b,
    Epub3,
}
