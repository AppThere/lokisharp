// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Record
// PURPOSE: Describes a font family available through IFontManager.
//          Read-only metadata — does not hold typeface objects.
// DEPENDS: FontAxis
// USED BY: IFontManager
// PHASE:   1
// ADR:     ADR-001

namespace AppThere.Loki.Skia.Fonts;

public sealed record FontFamilyInfo(
    string                  Name,
    bool                    IsVariable,
    bool                    IsBundled,
    IReadOnlyList<FontAxis> Axes);

public sealed record FontAxis(
    string Tag,     // OpenType 4-char axis tag, e.g. "wght", "opsz"
    string Name,    // Human-readable name, e.g. "Weight", "Optical Size"
    float  Min,
    float  Max,
    float  Default);
