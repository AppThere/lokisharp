// LAYER:   AppThere.Loki.Kernel — Kernel
// KIND:    Enum
// PURPOSE: CSS/OpenType font weight scale (100–900).
//          Maps to the OpenType wght axis for variable fonts.
//          Numeric values match the CSS font-weight specification.
// DEPENDS: (none)
// USED BY: FontDescriptor, IFontManager
// PHASE:   1
// ADR:     ADR-001

namespace AppThere.Loki.Kernel.Fonts;

public enum FontWeight
{
    Thin       = 100,
    ExtraLight = 200,
    Light      = 300,
    Regular    = 400,
    Medium     = 500,
    SemiBold   = 600,
    Bold       = 700,
    ExtraBold  = 800,
    Black      = 900,
}
