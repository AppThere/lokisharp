// LAYER:   AppThere.Loki.Format.Odf — ODF Import
// KIND:    Implementation
// PURPOSE: Maps ODF XML text:* elements to Loki FieldKinds.
// DEPENDS: FieldKind
// USED BY: OdfDocumentParser
// PHASE:   5
// ADR:     ADR-016

using AppThere.Loki.Writer.Model.Inlines;

namespace AppThere.Loki.Format.Odf;

public static class OdfFieldMap
{
    private static readonly Dictionary<string, FieldKind> _map = new(StringComparer.OrdinalIgnoreCase)
    {
        { "title",           FieldKind.Title },
        { "description",     FieldKind.Description },
        { "subject",         FieldKind.Subject },
        { "initial-creator", FieldKind.Author },
        { "author-name",     FieldKind.Author },
        { "page-number",     FieldKind.PageNumber },
        { "page-count",      FieldKind.PageCount },
        { "date",            FieldKind.Date },
        { "time",            FieldKind.Time },
        { "file-name",       FieldKind.FileName },
        { "chapter",         FieldKind.Chapter },
    };

    public static FieldKind Resolve(string localName)
    {
        if (_map.TryGetValue(localName, out var kind))
            return kind;
        return FieldKind.Unknown;
    }
}
