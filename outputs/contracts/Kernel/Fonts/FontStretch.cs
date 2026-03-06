// LAYER:   AppThere.Loki.Kernel — Kernel
// KIND:    Enum
// PURPOSE: Font stretch/width. Maps to OpenType wdth axis.
//          Numeric values are percentage of normal width (100 = normal).
// DEPENDS: (none)
// USED BY: FontDescriptor, IFontManager
// PHASE:   1

namespace AppThere.Loki.Kernel.Fonts;

public enum FontStretch
{
    UltraCondensed = 50,
    ExtraCondensed = 62,
    Condensed      = 75,
    SemiCondensed  = 87,
    Normal         = 100,
    SemiExpanded   = 112,
    Expanded       = 125,
    ExtraExpanded  = 150,
    UltraExpanded  = 200,
}
