// LAYER:   AppThere.Loki.Tests.Unit — Tests
// KIND:    Tests
// PURPOSE: Diagnostic — check SkiaSharp font loading in this environment
// DEPENDS: SkiaFontManager, SKTypeface
// USED BY: Developer diagnostics (not CI)
// PHASE:   1

using System.Reflection;
using SkiaSharp;
using AppThere.Loki.Skia.Fonts;
using AppThere.Loki.Tests.Unit.Skia;
using FluentAssertions;

namespace AppThere.Loki.Tests.Unit.Skia.Fonts;

public sealed class DiagnosticSkiaTests
{
    static DiagnosticSkiaTests() => SkiaTestInitializer.EnsureSkiaSharpLoaded();


    [Fact]
    public void Diagnostic_ResourceNames_AreFound()
    {
        var asm = typeof(SkiaFontManager).Assembly;
        var names = asm.GetManifestResourceNames();
        Assert.True(names.Any(n => n.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)),
            $"No .ttf resources found. Resources: [{string.Join(", ", names)}]");
    }

    [Fact]
    public void Diagnostic_XzStub_CanBePreloaded()
    {
        const string xzStub = "/data/data/com.termux/files/usr/lib/libxzstub.so";
        if (!File.Exists(xzStub)) { Assert.True(true, "No xzstub — skipping"); return; }

        // Try loading the stub directly via NativeLibrary
        nint handle = IntPtr.Zero;
        Exception? ex = null;
        try { handle = System.Runtime.InteropServices.NativeLibrary.Load(xzStub); }
        catch (Exception e) { ex = e; }

        Assert.True(handle != IntPtr.Zero,
            $"Could not load xzstub: {ex?.Message ?? "returned zero"}");
    }

    [Fact]
    public void Diagnostic_NativeLibraryLoad_Succeeds()
    {
        const string bionicPath = "/data/data/com.termux/files/usr/lib/libSkiaSharp.so";
        if (!File.Exists(bionicPath))
        {
            Assert.True(true, "Bionic path not present — skipping on non-Android host");
            return;
        }

        // Attempt to load the bionic library directly.
        nint handle = IntPtr.Zero;
        Exception? loadEx = null;
        try { handle = System.Runtime.InteropServices.NativeLibrary.Load(bionicPath); }
        catch (Exception ex) { loadEx = ex; }

        Assert.True(handle != IntPtr.Zero,
            $"NativeLibrary.Load(bionicPath) failed: {loadEx?.Message ?? "returned zero"}");
    }

    [Fact]
    public void Diagnostic_FromStream_LoadsInter()
    {
        var asm = typeof(SkiaFontManager).Assembly;
        var name = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.Contains("Inter-VariableFont", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(name);

        using var stream = asm.GetManifestResourceStream(name);
        Assert.NotNull(stream);

        var tf = SKTypeface.FromStream(stream);
        Assert.NotNull(tf);
        Assert.Contains("Inter", tf.FamilyName);
    }
}
