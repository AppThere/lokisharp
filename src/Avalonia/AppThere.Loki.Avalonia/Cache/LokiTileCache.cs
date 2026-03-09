// LAYER:   AppThere.Loki.Avalonia — Tile Cache
// KIND:    Implementation
// PURPOSE: Viewport-aware tile cache. Schedules renders on the thread pool,
//          deduplicates in-flight requests, classifies tiles into Hot/Warm/
//          Cool/Cold zones, and evicts Cold tiles within a memory budget.
//          TileReady is always raised on the Avalonia UI thread.
//          Does NOT perform layout or document access — delegates rendering
//          to ILokiView.RenderTileAsync.
// DEPENDS: ILokiTileCache, ILokiView, TileCacheOptions, TileGridMath,
//          CachedTile, TileKey, TileZone, ViewportGeometry, ILokiLogger
// USED BY: LokiHostBuilderExtensions.UseAvaloniaSurfaces, LokiTileControl
// PHASE:   4
// ADR:     ADR-011

using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using AppThere.Loki.Avalonia.Controls;
using AppThere.Loki.Kernel.Logging;
using AppThere.Loki.LokiKit.Events;
using AppThere.Loki.LokiKit.View;
using AppThere.Loki.LokiKit.Host;
using AppThere.Loki.Skia.Rendering;
using SkiaSharp;
using SkiaTileKey = AppThere.Loki.Skia.Rendering.TileKey;

namespace AppThere.Loki.Avalonia.Cache;

public sealed class LokiTileCache : ILokiTileCache
{
    private readonly ILokiView         _view;
    private readonly TileCacheOptions  _options;
    private readonly LokiHostOptions   _hostOptions;
    private readonly ILokiLogger       _logger;
    private readonly Action<Action>    _uiDispatch;
    private readonly object            _lock = new();

    private readonly Dictionary<TileKey, CachedTile>                               _completed = new();
    private readonly Dictionary<TileKey, (Task Task, CancellationTokenSource Cts)> _inFlight  = new();

    private ViewportGeometry?  _viewport;
    private int                _pageCount;
    private long               _totalBytes;
    private readonly CancellationTokenSource _maintenanceCts = new();
    private readonly Task                    _maintenanceTask;

    public event EventHandler<TileKey>? TileReady;
    public int  CachedTileCount   { get { lock (_lock) return _completed.Count; } }
    public long CachedMemoryBytes { get { lock (_lock) return _totalBytes; } }
    public IReadOnlyCollection<TileKey> CachedKeys { get { lock (_lock) return _completed.Keys.ToList(); } }

    public LokiTileCache(ILokiView view, TileCacheOptions options, ILokiLogger logger, LokiHostOptions? hostOptions = null)
        : this(view, options, logger, uiDispatch: null, hostOptions) { }

    internal LokiTileCache(
        ILokiView view,
        TileCacheOptions options,
        ILokiLogger logger,
        Action<Action>? uiDispatch,
        LokiHostOptions? hostOptions = null)
    {
        _view        = view;
        _options     = options;
        _logger      = logger;
        _uiDispatch  = uiDispatch ?? (action => Dispatcher.UIThread.Post(action));
        _hostOptions = hostOptions ?? LokiHostOptions.Default;
        _maintenanceTask = RunMaintenanceAsync(_maintenanceCts.Token);
    }

    // ── ILokiTileCache ────────────────────────────────────────────────────────

    public void UpdateViewport(ViewportGeometry vp, int pageCount)
    {
        float docW = _view.GetPartSize(0).Width;
        float docH = _view.GetPartSize(0).Height;
        float gapPts = _hostOptions.PageGapPts;
        
        var warmKeys = new List<TileKey>();
        for (int p = 0; p < pageCount; p++)
        {
            float pageTopPts = p * (docH + gapPts);
            float pageBottomPts = pageTopPts + docH;

            float vpTop = vp.ScrollOffsetYPts;
            float vpBottom = vpTop + vp.ViewportHeightPts;
            float tilePts = vp.TileSizePx / vp.Zoom;
            float vpTilesY = Math.Max(1f, vp.ViewportHeightPts / tilePts);
            float expansion = _options.KeepRadiusMultiplier * vpTilesY * tilePts;

            if (pageBottomPts < vpTop - expansion || pageTopPts > vpBottom + expansion)
                continue;

            var pageVp = vp with { 
                PartIndex = p, 
                ScrollOffsetYPts = Math.Max(0f, vp.ScrollOffsetYPts - pageTopPts) 
            };
            warmKeys.AddRange(EnumerateWarmTiles(pageVp, docW, docH));
        }

        List<TileKey>? toSchedule = null;
        lock (_lock)
        {
            _viewport = vp;
            _pageCount = pageCount;
            foreach (var entry in _completed.Values)
                entry.Zone = GetZoneUnderLock(entry.Key, vp, docH);

            EvictColdTilesUnderLock(vp, docH);

            foreach (var key in warmKeys)
            {
                if (!_completed.ContainsKey(key) && !_inFlight.ContainsKey(key))
                    (toSchedule ??= new()).Add(key);
            }
        }
        if (toSchedule is null) return;
        foreach (var k in toSchedule) ScheduleRender(k);
    }

    public WriteableBitmap? TryGetTile(TileKey key)
    {
        lock (_lock)
        {
            if (_completed.TryGetValue(key, out var entry))
            {
                entry.LastAccessed = DateTime.UtcNow;
                return entry.Bitmap;
            }
            if (!_inFlight.ContainsKey(key))
                ScheduleRenderUnderLock(key);
        }
        return null;
    }

    public void Invalidate(TileInvalidatedEventArgs args)
    {
        bool invalidateAll = args.InvalidatedKeys.Count == 0;
        List<(TileKey Key, TileZone Zone)>? toReschedule = null;
        lock (_lock)
        {
            IEnumerable<TileKey> targets = invalidateAll
                ? new List<TileKey>(_completed.Keys)
                : args.InvalidatedKeys.Select(MapSkiaKey);

            float docH = _view.GetPartSize(0).Height;

            foreach (var key in targets)
            {
                TileZone zone = _viewport is null ? TileZone.Cold
                    : GetZoneUnderLock(key, _viewport, docH);
                    
                if (_completed.Remove(key, out var entry))
                {
                    _totalBytes -= entry.ByteCost;
                    entry.Bitmap.Dispose();
                    toReschedule ??= new();
                    toReschedule.Add((key, zone));
                }
                if (_inFlight.TryGetValue(key, out var flight))
                {
                    flight.Cts.Cancel();
                    _inFlight.Remove(key);
                }
            }
        }
        if (toReschedule is null) return;
        foreach (var (key, zone) in toReschedule)
        {
            if (zone == TileZone.Hot)
            {
                ScheduleRender(key);
            }
            else if (zone == TileZone.Warm)
            {
                var captured = key;
                _ = Task.Run(async () =>
                {
                    await Task.Delay(_options.InvalidationDebouncedMs).ConfigureAwait(false);
                    ScheduleRender(captured);
                });
            }
        }
    }

    public void InvalidateAll()
    {
        List<TileKey>? hotKeys = null;
        lock (_lock)
        {
            foreach (var entry in _completed.Values) entry.Bitmap.Dispose();
            _completed.Clear();
            _totalBytes = 0;
            foreach (var (_, f) in _inFlight.Values) f.Cancel();
            _inFlight.Clear();
            if (_viewport is not null && _pageCount > 0)
            {
                var size = _view.GetPartSize(0);
                hotKeys = new List<TileKey>();
                for (int p = 0; p < _pageCount; p++)
                {
                    float pageTop = p * (size.Height + _hostOptions.PageGapPts);
                    var pageVp = _viewport with { 
                        PartIndex = p, 
                        ScrollOffsetYPts = Math.Max(0f, _viewport.ScrollOffsetYPts - pageTop) 
                    };
                    hotKeys.AddRange(TileGridMath.TilesForViewport(pageVp, size.Width, size.Height));
                }
            }
        }
        if (hotKeys is not null)
            foreach (var key in hotKeys) ScheduleRender(key);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private TileZone GetZoneUnderLock(TileKey key, ViewportGeometry vp, float docH)
    {
        float pageTopPts = key.PartIndex * (docH + _hostOptions.PageGapPts);
        var pageVp = vp with { ScrollOffsetYPts = Math.Max(0f, vp.ScrollOffsetYPts - pageTopPts) };
        return TileGridMath.ZoneForTile(key, pageVp, _options.KeepRadiusMultiplier, _options.RetainRadiusMultiplier);
    }

    private void ScheduleRender(TileKey key)
    {
        lock (_lock) { ScheduleRenderUnderLock(key); }
    }

    private void ScheduleRenderUnderLock(TileKey key)
    {
        if (_inFlight.ContainsKey(key)) return;
        var cts  = new CancellationTokenSource();
        var task = Task.Run(() => RenderTileAsync(key, cts.Token));
        _inFlight[key] = (task, cts);
    }

    private async Task RenderTileAsync(TileKey key, CancellationToken ct)
    {
        var request = new TileRequest(key.PartIndex, key.Zoom, key.TileX, key.TileY,
                                      _options.TileSizePx);
        SKBitmap? bitmap = null;
        try
        {
            bitmap = await _view.RenderTileAsync(request, ct).ConfigureAwait(false);
            if (ct.IsCancellationRequested) return;

            var wb   = ConvertToWriteableBitmap(bitmap);
            long cost = (long)bitmap.Width * bitmap.Height * 4;

            lock (_lock)
            {
                _inFlight.Remove(key);
                EvictUntilBudgetUnderLock(cost);
                var entry = new CachedTile(key, wb, cost);
                if (_viewport is not null)
                    entry.Zone = GetZoneUnderLock(key, _viewport, _view.GetPartSize(0).Height);
                _completed[key] = entry;
                _totalBytes += cost;
            }

            _uiDispatch(() => TileReady?.Invoke(this, key));
        }
        catch (OperationCanceledException)
        {
            lock (_lock) { _inFlight.Remove(key); }
        }
        catch (Exception ex)
        {
            _logger.Error("TileCache: render failed for {0}", ex, key);
            lock (_lock) { _inFlight.Remove(key); }
        }
        finally
        {
            bitmap?.Dispose();
        }
    }

    private static WriteableBitmap ConvertToWriteableBitmap(SKBitmap bitmap)
    {
        var wb = new WriteableBitmap(
            new PixelSize(bitmap.Width, bitmap.Height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);
        using var fb = wb.Lock();
        Marshal.Copy(bitmap.Bytes, 0, fb.Address, bitmap.Bytes.Length);
        return wb;
    }

    private void EvictColdTilesUnderLock(ViewportGeometry vp, float docH)
    {
        var cold = _completed.Values.Where(e => e.Zone == TileZone.Cold).ToList();
        foreach (var entry in cold)
        {
            _completed.Remove(entry.Key);
            _totalBytes -= entry.ByteCost;
            entry.Bitmap.Dispose();
        }
        var coldFlight = _inFlight.Keys
            .Where(k => GetZoneUnderLock(k, vp, docH) == TileZone.Cold)
            .ToList();
        foreach (var k in coldFlight)
        {
            _inFlight[k].Cts.Cancel();
            _inFlight.Remove(k);
        }
    }

    private void EvictUntilBudgetUnderLock(long incoming)
    {
        if (_totalBytes + incoming <= _options.MemoryCapBytes) return;
        var lru = _completed.Values
            .Where(e => e.Zone is TileZone.Cool or TileZone.Cold)
            .OrderBy(e => e.LastAccessed)
            .ToList();
        foreach (var entry in lru)
        {
            if (_totalBytes + incoming <= _options.MemoryCapBytes) break;
            _completed.Remove(entry.Key);
            _totalBytes -= entry.ByteCost;
            entry.Bitmap.Dispose();
        }
    }

    private IEnumerable<TileKey> EnumerateWarmTiles(
        ViewportGeometry vp, float docW, float docH)
    {
        float tilePts   = vp.TileSizePx / vp.Zoom;
        float vpTiles   = Math.Max(1f, Math.Max(
            vp.ViewportWidthPts / tilePts, vp.ViewportHeightPts / tilePts));
        float expansion = _options.KeepRadiusMultiplier * vpTiles * tilePts;
        var expandedVp = vp with
        {
            ViewportWidthPts  = vp.ViewportWidthPts  + 2f * expansion,
            ViewportHeightPts = vp.ViewportHeightPts + 2f * expansion,
            ScrollOffsetXPts  = Math.Max(0f, vp.ScrollOffsetXPts - expansion),
            ScrollOffsetYPts  = Math.Max(0f, vp.ScrollOffsetYPts - expansion),
        };
        return TileGridMath.TilesForViewport(expandedVp, docW, docH).ToList();
    }

    private async Task RunMaintenanceAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(
            TimeSpan.FromMilliseconds(_options.MaintenanceIntervalMs));
        while (!ct.IsCancellationRequested)
        {
            try { await timer.WaitForNextTickAsync(ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }

            lock (_lock)
            {
                if (_viewport is null || _pageCount == 0) continue;
                float docH = _view.GetPartSize(0).Height;
                foreach (var e in _completed.Values)
                    e.Zone = GetZoneUnderLock(e.Key, _viewport, docH);
                EvictColdTilesUnderLock(_viewport, docH);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _maintenanceCts.CancelAsync().ConfigureAwait(false);
        try { await _maintenanceTask.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false); }
        catch (Exception) { }

        Task[] pending;
        lock (_lock)
        {
            foreach (var (_, f) in _inFlight.Values) f.Cancel();
            pending = _inFlight.Values.Select(f => f.Task).ToArray();
        }
        try { await Task.WhenAll(pending).WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false); }
        catch (Exception) { }

        lock (_lock)
        {
            foreach (var entry in _completed.Values) entry.Bitmap.Dispose();
            _completed.Clear();
            _inFlight.Clear();
            _totalBytes = 0;
        }
        _maintenanceCts.Dispose();
    }

    private static TileKey MapSkiaKey(SkiaTileKey k) =>
        new(k.PartIndex, k.TileCol, k.TileRow, k.ZoomLevel);
}
