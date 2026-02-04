using System;
using System.Diagnostics;
using SharpInspect.Core.Configuration;
using SharpInspect.Core.Events;
using SharpInspect.Core.Models;
using SharpInspect.Core.Storage;
using System.Threading;

namespace SharpInspect.Core.Interceptors;

/// <summary>
///     백그라운드 타이머로 런타임 성능 메트릭을 수집합니다.
/// </summary>
public class PerformanceInterceptor : IDisposable
{
    private readonly SharpInspectOptions _options;
    private readonly Process _currentProcess;
    private readonly ISharpInspectStore _store;
    private bool _disposed;

    // CPU 계산 상태
    private DateTime _lastCpuCheckTime;
    private TimeSpan _lastCpuTime;

    // 네트워크 통계 계산 상태
    private int _lastNetworkCount;
    private DateTime _lastNetworkCheckTime;

    private Timer _timer;

    /// <summary>
    ///     새 PerformanceInterceptor를 생성하고 수집을 시작합니다.
    /// </summary>
    public PerformanceInterceptor(
        ISharpInspectStore store,
        SharpInspectOptions options)
    {
        if (store == null)
            throw new ArgumentNullException(nameof(store));
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        _store = store;
        _options = options;

        _currentProcess = Process.GetCurrentProcess();
        _lastCpuCheckTime = DateTime.UtcNow;
        _lastCpuTime = _currentProcess.TotalProcessorTime;
        _lastNetworkCheckTime = DateTime.UtcNow;
        _lastNetworkCount = 0;

        StartTimer();
    }

    /// <summary>
    ///     [Obsolete] 하위 호환성을 위한 생성자. EventBus 파라미터는 무시됩니다.
    /// </summary>
    [Obsolete("EventBus는 더 이상 직접 전달할 필요가 없습니다. Store에서 자동으로 이벤트를 발행합니다.")]
    public PerformanceInterceptor(
        ISharpInspectStore store,
        SharpInspectOptions options,
        EventBus eventBus)
        : this(store, options)
    {
    }

    private void StartTimer()
    {
        var interval = _options.PerformanceCaptureIntervalMs;
        if (interval <= 0) interval = 1000;

        _timer = new Timer(
            _ => CaptureMetrics(),
            null,
            interval,
            interval);
    }

    private void CaptureMetrics()
    {
        if (!_options.EnablePerformanceCapture)
            return;

        try
        {
            _currentProcess.Refresh();

            var entry = new PerformanceEntry
            {
                // GC 메트릭 (모든 프레임워크에서 사용 가능)
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2),
                TotalMemoryBytes = GC.GetTotalMemory(false),
                // 메모리 메트릭
                WorkingSetBytes = _currentProcess.WorkingSet64,
                PrivateMemoryBytes = _currentProcess.PrivateMemorySize64,
                // CPU 사용률 계산
                CpuUsagePercent = CalculateCpuUsage(),
                // 스레드 메트릭
                ThreadCount = _currentProcess.Threads.Count
            };

            // 네트워크 요청 통계 계산
            CalculateNetworkStats(entry);

            // 최신 .NET 전용 메트릭
#if MODERN_DOTNET
                try
                {
                    var gcInfo = GC.GetGCMemoryInfo();
                    entry.ManagedHeapSizeBytes = gcInfo.HeapSizeBytes;
                }
                catch
                {
                    // 이 런타임에서 사용 불가
                }

                try
                {
                    // GC.GetTotalPauseDuration()은 .NET 7+ 전용, net6 호환을 위해 리플렉션 사용
                    var method = typeof(GC).GetMethod("GetTotalPauseDuration",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (method != null)
                    {
                        var totalPause = (TimeSpan)method.Invoke(null, null);
                        var uptime = DateTime.UtcNow - _currentProcess.StartTime.ToUniversalTime();
                        if (uptime.TotalMilliseconds > 0)
                            entry.GcPauseTimePercent = Math.Round(
                                totalPause.TotalMilliseconds / uptime.TotalMilliseconds * 100.0, 4);
                    }
                }
                catch
                {
                    // 이 런타임에서 사용 불가
                }
#endif

            // Store에서 자동으로 이벤트 발행
            _store.AddPerformanceEntry(entry);
        }
        catch
        {
            // 타이머 크래시 방지를 위해 예외 무시
        }
    }

    private double CalculateCpuUsage()
    {
        try
        {
            var currentTime = DateTime.UtcNow;
            var currentCpuTime = _currentProcess.TotalProcessorTime;

            var elapsedTime = (currentTime - _lastCpuCheckTime).TotalMilliseconds;
            var elapsedCpu = (currentCpuTime - _lastCpuTime).TotalMilliseconds;

            _lastCpuCheckTime = currentTime;
            _lastCpuTime = currentCpuTime;

            if (elapsedTime <= 0)
                return 0;

            var cpuUsage = elapsedCpu / (Environment.ProcessorCount * elapsedTime) * 100.0;
            return Math.Round(Math.Min(cpuUsage, 100.0), 2);
        }
        catch
        {
            return 0;
        }
    }

    private void CalculateNetworkStats(PerformanceEntry entry)
    {
        try
        {
            var now = DateTime.UtcNow;
            var currentCount = _store.NetworkEntryCount;
            var elapsedSeconds = (now - _lastNetworkCheckTime).TotalSeconds;

            // 초당 요청 수 계산
            if (elapsedSeconds > 0)
            {
                var newRequests = currentCount - _lastNetworkCount;
                entry.RequestsPerSecond = Math.Round(newRequests / elapsedSeconds, 1);
            }

            _lastNetworkCount = currentCount;
            _lastNetworkCheckTime = now;

            // Uptime 계산
            var uptime = now - _currentProcess.StartTime.ToUniversalTime();
            entry.UptimeSeconds = (long)uptime.TotalSeconds;

            // 최근 요청들의 평균 응답 시간 및 에러율 계산
            var recentEntries = _store.GetNetworkEntries(0, 100);
            if (recentEntries != null && recentEntries.Length > 0)
            {
                double totalTime = 0;
                int errorCount = 0;
                int validCount = 0;

                foreach (var e in recentEntries)
                {
                    if (e.TotalMs > 0)
                    {
                        totalTime += e.TotalMs;
                        validCount++;
                    }

                    if (e.IsError || e.StatusCode >= 400)
                        errorCount++;
                }

                entry.AvgResponseTimeMs = validCount > 0 ? Math.Round(totalTime / validCount, 1) : -1;
                entry.ErrorRatePercent = Math.Round((double)errorCount / recentEntries.Length * 100, 1);
            }
        }
        catch
        {
            // 네트워크 통계 계산 실패 시 기본값 유지
        }
    }

    /// <summary>
    ///     인터셉터를 해제하고 메트릭 수집을 중지합니다.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _timer?.Dispose();
        }
    }
}