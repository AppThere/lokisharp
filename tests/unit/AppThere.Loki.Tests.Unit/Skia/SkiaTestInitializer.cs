// LAYER:   AppThere.Loki.Tests.Unit — Tests
// KIND:    Implementation
// PURPOSE: Ensures libSkiaSharp can be loaded in the test environment.
//          On Android PRoot (Termux/Android-NDK dotnet) systems, the standard
//          linux-arm64 glibc build of libSkiaSharp cannot be loaded. This helper:
//            1. Pre-loads a bionic stub that exports missing XZ symbols so that
//               the system libunwindstack.so resolves correctly.
//            2. Registers a DllImportResolver that redirects "libSkiaSharp"
//               to a bionic-compiled copy at the Termux usr/lib path.
//          On a standard Linux CI host the glibc path is used normally and this
//          class is a no-op. On Windows and macOS it is unconditionally a no-op.
// DEPENDS: (none — pure .NET + P/Invoke)
// USED BY: SkiaFontManagerTests, DiagnosticSkiaTests, LokiTextShaperTests, SkiaImageTests
// PHASE:   1

using System.Runtime.InteropServices;
using SkiaSharp;

namespace AppThere.Loki.Tests.Unit.Skia;

internal static class SkiaTestInitializer
{
    private static readonly object _lock = new();
    private static bool _done;

    // dlopen flags: RTLD_NOW (0x2) | RTLD_GLOBAL (0x100)
    // Declared only on Linux to prevent DllImport resolution failures on
    // Windows and macOS test hosts, which eagerly inspect P/Invoke stubs
    // during test discovery even when the method is never called.
#if NET && !BROWSER
    [DllImport("libdl", EntryPoint = "dlopen")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Interoperability", "SYSLIB1054",
        Justification = "Termux-only shim; guarded by RuntimeInformation check at call site.")]
    private static extern IntPtr DlOpen(string filename, int flags);
#endif

    private const int RtldNow    = 0x00002;
    private const int RtldGlobal = 0x00100;

    /// <summary>
    /// Call once before any SkiaSharp type is used in a test assembly.
    /// On Android/PRoot hosts this pre-loads a bionic stub for missing XZ
    /// symbols and redirects the SkiaSharp P/Invoke to the bionic binary.
    /// On Windows, macOS, and standard Linux CI this is a no-op.
    /// </summary>
    internal static void EnsureSkiaSharpLoaded()
    {
        lock (_lock)
        {
            if (_done) return;
            _done = true;

            // Guard 1: This shim is only relevant on Linux (which includes Termux/Android
            // PRoot). Windows and macOS use self-contained native asset packages and must
            // never reach the resolver-registration code below.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return;

            const string bionicSkia = "/data/data/com.termux/files/usr/lib/libSkiaSharp.so";
            const string xzStub     = "/data/data/com.termux/files/usr/lib/libxzstub.so";

            // Guard 2: If the Termux bionic binary is absent we are on a standard
            // Linux CI host — let the SkiaSharp.NativeAssets.Linux package handle loading.
            if (!File.Exists(bionicSkia)) return;

            try
            {
#if NET && !BROWSER
                // Preload the xz stub with RTLD_GLOBAL so its symbols (Xzs_Construct etc.)
                // are available when libunwindstack.so is loaded as a transitive dependency.
                if (File.Exists(xzStub))
                    DlOpen(xzStub, RtldNow | RtldGlobal);
#endif

                NativeLibrary.SetDllImportResolver(
                    typeof(SKTypeface).Assembly,
                    (libraryName, _, _) =>
                    {
                        if (!libraryName.StartsWith("libSkiaSharp",
                                StringComparison.OrdinalIgnoreCase))
                            return IntPtr.Zero;

                        return NativeLibrary.Load(bionicSkia);
                    });

                // Redirect HarfBuzzSharp P/Invoke to the Android system HarfBuzz
                // (bionic ABI — the glibc NuGet asset cannot load in this environment).
                const string bionicHarfBuzz = "/system/lib64/libharfbuzz_ng.so";
                NativeLibrary.SetDllImportResolver(
                    typeof(HarfBuzzSharp.Buffer).Assembly,
                    (libraryName, _, _) =>
                    {
                        if (!libraryName.StartsWith("libHarfBuzzSharp",
                                StringComparison.OrdinalIgnoreCase))
                            return IntPtr.Zero;

                        return NativeLibrary.Load(bionicHarfBuzz);
                    });
            }
            catch
            {
                // Resolver already registered or load failed — safe to ignore here.
            }
        }
    }
}
