// LAYER:   AppThere.Loki.Avalonia — Host
// KIND:    Enum + record (responsive layout system)
// PURPOSE: Five-breakpoint responsive layout system. Mobile-first:
//          Phone is the base layout; larger breakpoints are additive.
//          Phase 4 implements Phone (full) and Normal (full).
//          Compact, Wide, UltraWide are stubbed — fall through to
//          nearest implemented breakpoint.
// DEPENDS: —
// USED BY: LokiMainWindow, LokiDocumentPage, responsive AXAML triggers
// PHASE:   4

namespace AppThere.Loki.Avalonia.Host;

/// <summary>
/// Responsive layout breakpoints. Values are the minimum window width
/// in Avalonia device-independent pixels at which the breakpoint activates.
/// </summary>
public enum LayoutBreakpoint
{
    /// <summary>Width &lt; 600 DIPs. Single panel, no persistent toolbar, touch-first.</summary>
    Phone    = 0,
    /// <summary>600 ≤ width &lt; 900. Single panel, compact toolbar. (Phase 5)</summary>
    Compact  = 600,
    /// <summary>900 ≤ width &lt; 1200. Single panel, full toolbar, sidebar collapsed.</summary>
    Normal   = 900,
    /// <summary>1200 ≤ width &lt; 1800. Two panels, sidebar expanded. (Phase 5)</summary>
    Wide     = 1200,
    /// <summary>Width ≥ 1800. Three panels. (Phase 5)</summary>
    UltraWide = 1800,
}

/// <summary>
/// Resolves the active breakpoint from a window width.
/// </summary>
public static class LayoutBreakpointResolver
{
    public static LayoutBreakpoint Resolve(double windowWidthDips) => windowWidthDips switch
    {
        >= 1800 => LayoutBreakpoint.UltraWide,
        >= 1200 => LayoutBreakpoint.Wide,
        >= 900  => LayoutBreakpoint.Normal,
        >= 600  => LayoutBreakpoint.Compact,
        _       => LayoutBreakpoint.Phone,
    };

    /// <summary>
    /// Returns the nearest implemented breakpoint for the given breakpoint.
    /// Phase 4: Phone and Normal are implemented. Others fall through.
    /// </summary>
    public static LayoutBreakpoint NearestImplemented(LayoutBreakpoint bp) => bp switch
    {
        LayoutBreakpoint.Phone     => LayoutBreakpoint.Phone,
        LayoutBreakpoint.Compact   => LayoutBreakpoint.Phone,   // Phase 5
        LayoutBreakpoint.Normal    => LayoutBreakpoint.Normal,
        LayoutBreakpoint.Wide      => LayoutBreakpoint.Normal,  // Phase 5
        LayoutBreakpoint.UltraWide => LayoutBreakpoint.Normal,  // Phase 5
        _                          => LayoutBreakpoint.Phone,
    };
}
