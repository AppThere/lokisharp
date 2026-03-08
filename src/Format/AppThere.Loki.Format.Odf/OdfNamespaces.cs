// LAYER:   AppThere.Loki.Format.Odf — ODF Import
// KIND:    Implementation
// PURPOSE: Central registry of ODF XML namespace URIs used across all three
//          import passes. Provides XNamespace constants so callers compose
//          qualified names via the + operator: OdfNamespaces.Text + "p".
//          Does NOT contain any parsing logic.
// DEPENDS: (none)
// USED BY: StyleParser, OdfBodyParser, OdfImporter
// PHASE:   3
// ADR:     ADR-009

using System.Xml.Linq;

namespace AppThere.Loki.Format.Odf;

internal static class OdfNamespaces
{
    public static readonly XNamespace Office =
        "urn:oasis:names:tc:opendocument:xmlns:office:1.0";

    public static readonly XNamespace Style =
        "urn:oasis:names:tc:opendocument:xmlns:style:1.0";

    public static readonly XNamespace Text =
        "urn:oasis:names:tc:opendocument:xmlns:text:1.0";

    /// <summary>XSL-FO compatible namespace (prefix fo: in ODF XML).</summary>
    public static readonly XNamespace FoText =
        "urn:oasis:names:tc:opendocument:xmlns:xsl-fo-compatible:1.0";

    public static readonly XNamespace Svg =
        "urn:oasis:names:tc:opendocument:xmlns:svg-compatible:1.0";

    public static readonly XNamespace Draw =
        "urn:oasis:names:tc:opendocument:xmlns:drawing:1.0";
}
