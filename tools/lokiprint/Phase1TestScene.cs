// LAYER:   AppThere.Loki.Tools.LokiPrint — Host
// KIND:    Implementation
// PURPOSE: Builds the Phase 1 exit-criterion PaintScene. Thin redirect to
//          AppThere.Loki.Skia.Testing.Phase1TestScene which now lives in the
//          Skia layer so that StubEngine (LokiKit layer) can also reference it
//          without an upward dependency.
//          Kept here as a type alias so any code in lokiprint that still
//          references the old name continues to compile without changes.
// DEPENDS: AppThere.Loki.Skia.Testing.Phase1TestScene
// USED BY: (legacy; new code uses AppThere.Loki.Skia.Testing.Phase1TestScene directly)
// PHASE:   2

using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.Skia.Fonts;
using AppThere.Loki.Skia.Images;
using AppThere.Loki.Skia.Scene;

namespace AppThere.Loki.Tools.LokiPrint;

/// <summary>
/// Redirect shim — delegates to AppThere.Loki.Skia.Testing.Phase1TestScene.
/// </summary>
internal static class Phase1TestScene
{
    public static (PaintScene scene, IImageStore imageStore)
        Build(IFontManager fontManager, ILokiLogger logger,
              IImageStore? externalStore = null)
        => AppThere.Loki.Skia.Testing.Phase1TestScene.Build(fontManager, logger, externalStore);
}
