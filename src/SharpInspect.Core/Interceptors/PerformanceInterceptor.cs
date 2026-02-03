using System;
using System.Diagnostics;
using SharpInspect.Core.Configuration;
using SharpInspect.Core.Events;
using SharpInspect.Core.Models;
using SharpInspect.Core.Storage;
#if !NET35
using System.Threading;
#endif

namespace SharpInspect.Core.Interceptors
{
    /// <summary>
    ///     Collects runtime performance metrics on a background timer.
    /// </summary>
    public class PerformanceInterceptor : IDisposable
    {
        private readonly EventBus _eventBus;
        private readonly SharpInspectOptions _options;
        private readonly Process _currentProcess;
        private readonly ISharpInspectStore _store;
        private bool _disposed;

        // CPU calculation state
        private DateTime _lastCpuCheckTime;
        private TimeSpan _lastCpuTime;

#if NET35
        private System.Timers.Timer _timer;
#else
        private Timer _timer;
#endif

        /// <summary>
        ///     Creates a new PerformanceInterceptor and starts collection.
        /// </summary>
        public PerformanceInterceptor(
            ISharpInspectStore store,
            SharpInspectOptions options,
            EventBus eventBus = null)
        {
            if (store == null)
                throw new ArgumentNullException("store");
            if (options == null)
                throw new ArgumentNullException("options");

            _store = store;
            _options = options;
            _eventBus = eventBus ?? EventBus.Instance;

            _currentProcess = Process.GetCurrentProcess();
            _lastCpuCheckTime = DateTime.UtcNow;
            _lastCpuTime = _currentProcess.TotalProcessorTime;

            StartTimer();
        }

        private void StartTimer()
        {
            var interval = _options.PerformanceCaptureIntervalMs;
            if (interval <= 0) interval = 1000;

#if NET35
            _timer = new System.Timers.Timer(interval);
            _timer.Elapsed += (s, e) => CaptureMetrics();
            _timer.AutoReset = true;
            _timer.Start();
#else
            _timer = new Timer(
                _ => CaptureMetrics(),
                null,
                interval,
                interval);
#endif
        }

        private void CaptureMetrics()
        {
            if (!_options.EnablePerformanceCapture)
                return;

            try
            {
                _currentProcess.Refresh();

                var entry = new PerformanceEntry();

                // GC metrics (available on all frameworks)
                entry.Gen0Collections = GC.CollectionCount(0);
                entry.Gen1Collections = GC.CollectionCount(1);
                entry.Gen2Collections = GC.CollectionCount(2);
                entry.TotalMemoryBytes = GC.GetTotalMemory(false);

                // Memory metrics
                entry.WorkingSetBytes = _currentProcess.WorkingSet64;
                entry.PrivateMemoryBytes = _currentProcess.PrivateMemorySize64;

                // CPU usage calculation
                entry.CpuUsagePercent = CalculateCpuUsage();

                // Thread metrics
                entry.ThreadCount = _currentProcess.Threads.Count;

#if !NET35
                int workerThreads, completionPortThreads;
                ThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);
                entry.ThreadPoolWorkerThreads = workerThreads;
                entry.ThreadPoolCompletionPortThreads = completionPortThreads;
#endif

                // Modern .NET only metrics
#if MODERN_DOTNET
                try
                {
                    var gcInfo = GC.GetGCMemoryInfo();
                    entry.ManagedHeapSizeBytes = gcInfo.HeapSizeBytes;
                }
                catch
                {
                    // Unavailable on this runtime
                }

                try
                {
                    // GC.GetTotalPauseDuration() is .NET 7+ only, use reflection for net6 compat
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
                    // Unavailable on this runtime
                }
#endif

                _store.AddPerformanceEntry(entry);

#if NET35
                _eventBus.Publish(new PerformanceEntryEvent(entry));
#else
                _eventBus.PublishAsync(new PerformanceEntryEvent(entry));
#endif
            }
            catch
            {
                // Swallow exceptions to prevent timer crash
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

        /// <summary>
        ///     Disposes the interceptor and stops metric collection.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
#if NET35
                _timer?.Stop();
                _timer?.Dispose();
#else
                _timer?.Dispose();
#endif
            }
        }
    }
}
