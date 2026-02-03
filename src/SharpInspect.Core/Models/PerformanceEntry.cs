using System;

namespace SharpInspect.Core.Models
{
    /// <summary>
    ///     Represents a captured performance metrics snapshot.
    /// </summary>
    public class PerformanceEntry
    {
        /// <summary>
        ///     Creates a new PerformanceEntry with a unique ID and current timestamp.
        /// </summary>
        public PerformanceEntry()
        {
            Id = Guid.NewGuid().ToString("N");
            Timestamp = DateTime.UtcNow;
            GcPauseTimePercent = -1;
            ManagedHeapSizeBytes = -1;
            ThreadPoolWorkerThreads = -1;
            ThreadPoolCompletionPortThreads = -1;
        }

        /// <summary>
        ///     Timestamp when the metrics snapshot was taken.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        ///     Process CPU usage percentage (0-100).
        /// </summary>
        public double CpuUsagePercent { get; set; }

        /// <summary>
        ///     GC pause time as a percentage (0-100). Available on .NET 7+ only; -1 if unavailable.
        /// </summary>
        public double GcPauseTimePercent { get; set; }

        /// <summary>
        ///     Number of Gen0 garbage collections since process start.
        /// </summary>
        public int Gen0Collections { get; set; }

        /// <summary>
        ///     Number of Gen1 garbage collections since process start.
        /// </summary>
        public int Gen1Collections { get; set; }

        /// <summary>
        ///     Number of Gen2 garbage collections since process start.
        /// </summary>
        public int Gen2Collections { get; set; }

        /// <summary>
        ///     Total number of threads in the process.
        /// </summary>
        public int ThreadCount { get; set; }

        /// <summary>
        ///     Number of available completion port threads in the thread pool. -1 if unavailable.
        /// </summary>
        public int ThreadPoolCompletionPortThreads { get; set; }

        /// <summary>
        ///     Number of available worker threads in the thread pool. -1 if unavailable.
        /// </summary>
        public int ThreadPoolWorkerThreads { get; set; }

        /// <summary>
        ///     Managed heap size in bytes. Available on .NET 6+ only; -1 if unavailable.
        /// </summary>
        public long ManagedHeapSizeBytes { get; set; }

        /// <summary>
        ///     Process private memory in bytes.
        /// </summary>
        public long PrivateMemoryBytes { get; set; }

        /// <summary>
        ///     Total managed memory reported by GC in bytes.
        /// </summary>
        public long TotalMemoryBytes { get; set; }

        /// <summary>
        ///     Process working set (physical memory) in bytes.
        /// </summary>
        public long WorkingSetBytes { get; set; }

        /// <summary>
        ///     Unique identifier for this entry.
        /// </summary>
        public string Id { get; set; }
    }
}
