using System;
using SharpInspect.Core.Events;
using SharpInspect.Core.Models;

namespace SharpInspect.Core.Tests
{
    /// <summary>
    ///     테스트용 공통 유틸리티 클래스.
    /// </summary>
    public static class TestHelpers
    {
        /// <summary>
        ///     테스트용 ConsoleEntry를 생성합니다.
        /// </summary>
        public static ConsoleEntry CreateConsoleEntry(
            string message = "Test message",
            SharpInspectLogLevel level = SharpInspectLogLevel.Information)
        {
            return new ConsoleEntry(message, level);
        }

        /// <summary>
        ///     테스트용 NetworkEntry를 생성합니다.
        /// </summary>
        public static NetworkEntry CreateNetworkEntry(
            string url = "https://example.com/api/test",
            string method = "GET",
            int statusCode = 200)
        {
            return new NetworkEntry
            {
                Url = url,
                Method = method,
                StatusCode = statusCode,
                Host = "example.com",
                Path = "/api/test",
                TotalMs = 100.5
            };
        }

        /// <summary>
        ///     테스트용 PerformanceEntry를 생성합니다.
        /// </summary>
        public static PerformanceEntry CreatePerformanceEntry(
            double cpuUsage = 50.0,
            long totalMemory = 1024 * 1024 * 100)
        {
            return new PerformanceEntry
            {
                CpuUsagePercent = cpuUsage,
                TotalMemoryBytes = totalMemory,
                WorkingSetBytes = totalMemory * 2,
                Gen0Collections = 10,
                Gen1Collections = 5,
                Gen2Collections = 1
            };
        }

        /// <summary>
        ///     테스트용 ApplicationInfo를 생성합니다.
        /// </summary>
        public static ApplicationInfo CreateApplicationInfo(
            string assemblyName = "TestApp",
            string runtimeVersion = "8.0.0")
        {
            return new ApplicationInfo
            {
                AssemblyName = assemblyName,
                RuntimeVersion = runtimeVersion,
                ProcessId = 12345,
                MachineName = "TEST-MACHINE",
                ProcessorCount = 8
            };
        }
    }

    /// <summary>
    ///     EventBus 테스트를 위한 테스트용 이벤트 클래스.
    /// </summary>
    public class TestEvent : SharpInspectEventBase
    {
        public TestEvent(string message)
        {
            Message = message;
        }

        public string Message { get; }

        public override string EventType => "test:event";
    }

    /// <summary>
    ///     EventBus 테스트를 위한 또 다른 테스트용 이벤트 클래스.
    /// </summary>
    public class AnotherTestEvent : SharpInspectEventBase
    {
        public int Value { get; set; }

        public override string EventType => "test:another";
    }
}
