// LAYER:   AppThere.Loki.Writer — Engine
// KIND:    Implementation
// PURPOSE: ILokiPart implementation for Writer documents. Represents one page
//          of the document. SizeInPoints matches the PageStyle including margins.
//          DisplayName is "Page N" (1-based). Index is 0-based.
// DEPENDS: ILokiPart, PageStyle, SizeF
// USED BY: WriterEngine.GetPart
// PHASE:   3
// ADR:     ADR-007

using AppThere.Loki.Kernel.Geometry;
using AppThere.Loki.LokiKit.Document;
using AppThere.Loki.Writer.Model;

namespace AppThere.Loki.Writer.Engine;

internal sealed class WriterPart : ILokiPart
{
    public int    Index        { get; }
    public SizeF  SizeInPoints { get; }
    public string DisplayName  { get; }

    public WriterPart(int partIndex, PageStyle pageStyle)
    {
        Index        = partIndex;
        SizeInPoints = new SizeF(pageStyle.WidthPts, pageStyle.HeightPts);
        DisplayName  = $"Page {partIndex + 1}";
    }
}
