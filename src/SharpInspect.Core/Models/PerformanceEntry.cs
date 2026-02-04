using System;

namespace SharpInspect.Core.Models;

/// <summary>
///     캡처된 성능 메트릭 스냅샷을 나타냅니다.
/// </summary>
public class PerformanceEntry
{
    /// <summary>
    ///     고유 ID와 현재 타임스탬프로 새 PerformanceEntry를 생성합니다.
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
    ///     메트릭 스냅샷이 촬영된 타임스탬프.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    ///     프로세스 CPU 사용률 (0-100).
    /// </summary>
    public double CpuUsagePercent { get; set; }

    /// <summary>
    ///     GC 일시정지 시간 비율 (0-100). .NET 7+ 전용; 사용 불가 시 -1.
    /// </summary>
    public double GcPauseTimePercent { get; set; }

    /// <summary>
    ///     프로세스 시작 이후 0세대 가비지 수집 횟수.
    /// </summary>
    public int Gen0Collections { get; set; }

    /// <summary>
    ///     프로세스 시작 이후 1세대 가비지 수집 횟수.
    /// </summary>
    public int Gen1Collections { get; set; }

    /// <summary>
    ///     프로세스 시작 이후 2세대 가비지 수집 횟수.
    /// </summary>
    public int Gen2Collections { get; set; }

    /// <summary>
    ///     프로세스의 총 스레드 수.
    /// </summary>
    public int ThreadCount { get; set; }

    /// <summary>
    ///     스레드 풀의 사용 가능한 완료 포트 스레드 수. 사용 불가 시 -1.
    /// </summary>
    public int ThreadPoolCompletionPortThreads { get; set; }

    /// <summary>
    ///     스레드 풀의 사용 가능한 작업자 스레드 수. 사용 불가 시 -1.
    /// </summary>
    public int ThreadPoolWorkerThreads { get; set; }

    /// <summary>
    ///     관리 힙 크기(바이트). .NET 6+ 전용; 사용 불가 시 -1.
    /// </summary>
    public long ManagedHeapSizeBytes { get; set; }

    /// <summary>
    ///     프로세스 전용 메모리(바이트).
    /// </summary>
    public long PrivateMemoryBytes { get; set; }

    /// <summary>
    ///     GC가 보고한 총 관리 메모리(바이트).
    /// </summary>
    public long TotalMemoryBytes { get; set; }

    /// <summary>
    ///     프로세스 작업 세트(물리 메모리, 바이트).
    /// </summary>
    public long WorkingSetBytes { get; set; }

    /// <summary>
    ///     이 엔트리의 고유 식별자.
    /// </summary>
    public string Id { get; set; }
}