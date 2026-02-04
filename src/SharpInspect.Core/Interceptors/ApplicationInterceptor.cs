using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime;
using SharpInspect.Core.Configuration;
using SharpInspect.Core.Models;
using SharpInspect.Core.Storage;
using System.Threading;

namespace SharpInspect.Core.Interceptors;

/// <summary>
///     애플리케이션 메타데이터를 수집하는 인터셉터.
///     시작 시 정적 정보를 캡처하고, 주기적으로 어셈블리 목록을 갱신합니다.
/// </summary>
public class ApplicationInterceptor : IDisposable
{
    private readonly ISharpInspectStore _store;
    private readonly SharpInspectOptions _options;
    private bool _disposed;

    private Timer _timer;

    /// <summary>
    ///     새 ApplicationInterceptor를 생성하고 초기 스냅샷을 캡처합니다.
    /// </summary>
    public ApplicationInterceptor(
        ISharpInspectStore store,
        SharpInspectOptions options)
    {
        if (store == null)
            throw new ArgumentNullException(nameof(store));
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        _store = store;
        _options = options;

        // 초기 스냅샷 캡처
        CaptureSnapshot();

        // 어셈블리 목록 갱신 타이머 시작
        StartTimer();
    }

    /// <summary>
    ///     [Obsolete] 하위 호환성을 위한 생성자. EventBus 파라미터는 무시됩니다.
    /// </summary>
    [Obsolete("EventBus는 더 이상 직접 전달할 필요가 없습니다. Store에서 자동으로 이벤트를 발행합니다.")]
    public ApplicationInterceptor(
        ISharpInspectStore store,
        SharpInspectOptions options,
        Events.EventBus eventBus)
        : this(store, options)
    {
    }

    private void CaptureSnapshot()
    {
        try
        {
            var info = new ApplicationInfo();

            // 엔트리 어셈블리 정보
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                info.AssemblyName = entryAssembly.GetName().Name;
                info.AssemblyVersion = entryAssembly.GetName().Version != null
                    ? entryAssembly.GetName().Version.ToString()
                    : null;
                try
                {
                    info.AssemblyLocation = entryAssembly.Location;
                }
                catch
                {
                    // 동적 어셈블리 또는 단일 파일 배포
                }
            }

            // 런타임 버전
            info.RuntimeVersion = Environment.Version.ToString();

#if MODERN_DOTNET
                info.FrameworkDescription = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
                info.OsDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
                info.ProcessArchitecture =
 System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString();
#elif NETFRAMEWORK
            info.FrameworkDescription = ".NET Framework " + Environment.Version;
            info.OsDescription = Environment.OSVersion.ToString();
            info.ProcessArchitecture = Environment.Is64BitProcess ? "X64" : "X86";
#else
                info.FrameworkDescription = ".NET Standard 2.0 (runtime: " + Environment.Version + ")";
                info.OsDescription = Environment.OSVersion.ToString();
                info.ProcessArchitecture = Environment.Is64BitProcess ? "X64" : "X86";
#endif

            // 프로세스 정보
            var process = Process.GetCurrentProcess();
            info.ProcessId = process.Id;
            try
            {
                info.ProcessStartTime = process.StartTime.ToUniversalTime();
            }
            catch
            {
                info.ProcessStartTime = DateTime.UtcNow;
            }

            info.MachineName = Environment.MachineName;

            info.UserName = Environment.UserName;

            info.WorkingDirectory = Environment.CurrentDirectory;
            info.ProcessorCount = Environment.ProcessorCount;
            info.IsServerGC = GCSettings.IsServerGC;

            // 커맨드 라인 인수
            try
            {
                var args = Environment.GetCommandLineArgs();
                info.CommandLineArgs = new List<string>(args);
            }
            catch
            {
                // 권한 부족 시 무시
            }

            // 환경 변수
            info.EnvironmentVariables = CaptureEnvironmentVariables();

            // 로드된 어셈블리
            info.LoadedAssemblies = CaptureLoadedAssemblies();
            info.LoadedAssemblyCount = info.LoadedAssemblies.Count;

            // Store에서 자동으로 이벤트 발행
            _store.SetApplicationInfo(info);
        }
        catch
        {
            // 캡처 실패 시 타이머 크래시 방지를 위해 무시
        }
    }

    private Dictionary<string, string> CaptureEnvironmentVariables()
    {
        var result = new Dictionary<string, string>();
        try
        {
            var envVars = Environment.GetEnvironmentVariables();
            foreach (DictionaryEntry entry in envVars)
            {
                var key = entry.Key != null ? entry.Key.ToString() : "";
                var value = entry.Value != null ? entry.Value.ToString() : "";

                // 민감한 값 마스킹
                if (IsSensitiveKey(key))
                    value = "********";

                result[key] = value;
            }
        }
        catch
        {
            // 권한 부족 시 무시
        }

        return result;
    }

    private static bool IsSensitiveKey(string key)
    {
        var lower = key.ToLowerInvariant();
        return lower.Contains("password")
               || lower.Contains("secret")
               || lower.Contains("token")
               || lower.Contains("key")
               || lower.Contains("connectionstring");
    }

    private List<AssemblyInfo> CaptureLoadedAssemblies()
    {
        var result = new List<AssemblyInfo>();
        try
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                var asmInfo = new AssemblyInfo
                {
                    FullName = asm.FullName,
                    Name = asm.GetName().Name,
                    Version = asm.GetName().Version != null
                        ? asm.GetName().Version.ToString()
                        : null
                };
                asmInfo.IsDynamic = asm.IsDynamic;

                if (!asm.IsDynamic)
                {
                    try
                    {
                        asmInfo.Location = asm.Location;
                    }
                    catch
                    {
                        // 위치를 가져올 수 없는 어셈블리
                    }

#if NETFRAMEWORK
                    asmInfo.IsGAC = asm.GlobalAssemblyCache;
#endif
                }

                result.Add(asmInfo);
            }
        }
        catch
        {
            // 어셈블리 목록 가져오기 실패
        }

        return result;
    }

    private void StartTimer()
    {
        var interval = _options.ApplicationRefreshIntervalMs;
        if (interval <= 0) interval = 30000;

        _timer = new Timer(
            _ => CaptureSnapshot(),
            null,
            interval,
            interval);
    }

    /// <summary>
    ///     인터셉터를 해제하고 갱신 타이머를 중지합니다.
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