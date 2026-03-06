// LAYER:   AppThere.Loki.Kernel — Kernel
// KIND:    Enum
// PURPOSE: Font slant style. Maps to OpenType ital and slnt axes.
//          Italic uses the font's designed italic glyphs (ital=1).
//          Oblique synthesises slant via a shear transform (slnt axis).
// DEPENDS: (none)
// USED BY: FontDescriptor, IFontManager
// PHASE:   1

namespace AppThere.Loki.Kernel.Fonts;

public enum FontSlant { Upright, Italic, Oblique }
