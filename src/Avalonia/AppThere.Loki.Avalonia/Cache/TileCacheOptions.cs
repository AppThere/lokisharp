// LAYER:   AppThere.Loki.Avalonia — Tile Cache
// KIND:    Record (configuration)
// PURPOSE: Runtime-configurable options for the viewport-aware tile cache.
//          Supplied to UseAvaloniaSurfaces() on LokiHostBuilder.
//          Platform-appropriate defaults provided as static properties.
//          All properties are init-only — use with-expressions to override.
// DEPENDS: —
// USED BY: LokiHostBuilderExtensions.UseAvaloniaSurfaces, LokiTileCache
// PHASE:   4
// ADR:     ADR-011

namespace AppThere.Loki.Avalonia.Cache;

public sealed record TileCacheOptions
{
    /// <summary>Tile edge length in pixels. Tiles are always square.</summary>
    public int   TileSizePx              { get; init; } = 512;

    /// <summary>
    /// Tiles within this multiple of the viewport height/width are kept
    /// in cache and pre-rendered. E.g. 2.0 means keep tiles within 2×
    /// the visible area in every direction.
    /// </summary>
    public float KeepRadiusMultiplier    { get; init; } = 2.0f;

    /// <summary>
    /// Tiles within this multiple are retained if memory permits.
    /// Evicted before warm tiles on memory pressure.
    /// Must be >= KeepRadiusMultiplier.
    /// </summary>
    public float RetainRadiusMultiplier  { get; init; } = 4.0f;

    /// <summary>Maximum total tile memory in bytes.</summary>
    public long  MemoryCapBytes          { get; init; } = 256L * 1024 * 1024;

    /// <summary>
    /// Debounce interval in milliseconds for TileInvalidated warm-zone
    /// re-render scheduling. Hot zone tiles are always re-rendered immediately.
    /// </summary>
    public int   InvalidationDebouncedMs { get; init; } = 100;

    /// <summary>Interval between background maintenance sweeps in milliseconds.</summary>
    public int   MaintenanceIntervalMs   { get; init; } = 500;

    /// <summary>Desktop defaults: 256MB cap, 2×/4× radius multipliers.</summary>
    public static readonly TileCacheOptions Desktop = new();

    /// <summary>Mobile defaults: 64MB cap, 1.5×/3× radius multipliers.</summary>
    public static readonly TileCacheOptions Mobile = new()
    {
        TileSizePx             = 512,
        KeepRadiusMultiplier   = 1.5f,
        RetainRadiusMultiplier = 3.0f,
        MemoryCapBytes         = 64L * 1024 * 1024,
    };
}
