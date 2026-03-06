// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Record
// PURPOSE: Immutable description of all visual content for one document Part
//          (page, sheet, slide, or drawing). Input to TileRenderer and PdfRenderer.
//          Contains an ordered list of PaintBands covering the full Part height.
//          Constructed by the layout engine (Phase 3+); in Phase 1, built directly
//          in tests and the lokiprint test-render command.
//          SizeInPoints is the logical size of the Part (e.g. 595×842 for A4).
//          DpiBase is always 96.0f; scaling is applied per TileRequest.
// DEPENDS: PaintBand, SizeF
// USED BY: TileRenderer, PdfRenderer, JSON serialisation (ADR-003)
// PHASE:   1
// ADR:     ADR-003

using System.Collections.Immutable;
using AppThere.Loki.Kernel.Geometry;

namespace AppThere.Loki.Skia.Scene;

public sealed record PaintScene(
    int                       PartIndex,
    SizeF                     SizeInPoints,
    float                     DpiBase,         // always 96.0f
    ImmutableArray<PaintBand>  Bands)
{
    public static Builder CreateBuilder(int partIndex) => new(partIndex);

    public sealed class Builder
    {
        private readonly int _partIndex;
        private SizeF  _size     = new(595f, 842f);  // A4 default
        private float  _dpiBase  = 96f;
        private readonly List<PaintBand> _bands = new();

        internal Builder(int partIndex) => _partIndex = partIndex;

        public Builder WithSize(float widthPts, float heightPts)
            { _size = new(widthPts, heightPts); return this; }
        public Builder WithDpiBase(float dpi)
            { _dpiBase = dpi; return this; }
        public Builder AddBand(PaintBand band)
            { _bands.Add(band); return this; }

        public PaintScene Build() =>
            new(_partIndex, _size, _dpiBase, _bands.ToImmutableArray());
    }
}
