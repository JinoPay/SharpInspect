using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SharpInspect.Core.Configuration;
using SharpInspect.Core.EnvironmentDetection;
using SharpInspect.Core.Events;
using SharpInspect.Core.Interceptors;
using SharpInspect.Core.Logging;
using SharpInspect.Core.Storage;
#if !NET35
using System.Net.Http;
using SharpInspect.Server.WebServer;
#endif

namespace SharpInspect
{
    /// <summary>
    ///     SharpInspect DevTools의 메인 진입점.
    /// </summary>
    public static class SharpInspectDevTools
    {
        private static bool _initialized;
        private static bool _disabled;
        private static ConsoleHook _consoleHook;
        private static InMemoryStore _store;

#if !NET35
        private static ISharpInspectServer _server;
#endif
        private static readonly object _lock = new();
        private static ApplicationInterceptor _applicationInterceptor;
        private static PerformanceInterceptor _performanceInterceptor;
        private static TraceHook _traceHook;

        /// <summary>
        ///     SharpInspect가 초기화되었는지 여부를 가져옵니다.
        /// </summary>
        public static bool IsInitialized
        {
            get
            {
                lock (_lock)
                {
                    return _initialized;
                }
            }
        }

        /// <summary>
        ///     SharpInspect가 개발 환경 외에서 비활성화되었는지 여부를 가져옵니다.
        ///     EnableInDevelopmentOnly가 true이고 프로덕션 환경에서 실행 중일 때 true입니다.
        /// </summary>
        public static bool IsDisabled
        {
            get
            {
                lock (_lock)
                {
                    return _disabled;
                }
            }
        }

        /// <summary>
        ///     이벤트 버스 인스턴스를 가져옵니다.
        /// </summary>
        public static EventBus EventBus { get; private set; }

        /// <summary>
        ///     스토리지 인스턴스를 가져옵니다.
        /// </summary>
        public static ISharpInspectStore Store => _store;

        /// <summary>
        ///     현재 설정 옵션을 가져옵니다.
        /// </summary>
        public static SharpInspectOptions Options { get; private set; }

        /// <summary>
        ///     DevTools URL을 가져옵니다.
        /// </summary>
        public static string DevToolsUrl => Options?.GetDevToolsUrl();

        /// <summary>
        ///     기본 옵션으로 SharpInspect를 초기화합니다.
        /// </summary>
        public static void Initialize()
        {
            Initialize(new SharpInspectOptions());
        }

        /// <summary>
        ///     설정 액션을 사용하여 SharpInspect를 초기화합니다.
        /// </summary>
        public static void Initialize(Action<SharpInspectOptions> configure)
        {
            var options = new SharpInspectOptions();
            configure?.Invoke(options);
            Initialize(options);
        }

        /// <summary>
        ///     지정된 옵션으로 SharpInspect를 초기화합니다.
        /// </summary>
        public static void Initialize(SharpInspectOptions options)
        {
            lock (_lock)
            {
                if (_initialized)
                    throw new InvalidOperationException(
                        "SharpInspect has already been initialized. Call Shutdown() first.");

                Options = options ?? new SharpInspectOptions();

                // 개발 환경 체크
                if (!DevelopmentEnvironmentDetector.IsDevelopment(Options))
                {
                    // 프로덕션 환경 - 초기화하지 않고 조용히 종료
                    _disabled = true;
                    _initialized = true;
                    return;
                }

                _disabled = false;
                EventBus = new EventBus();
                _store = new InMemoryStore(Options.MaxNetworkEntries, Options.MaxConsoleEntries, Options.MaxPerformanceEntries);

                // .NET Framework용 HTTP 인터셉터 초기화
                HttpWebRequestInterceptor.Initialize(_store, Options, EventBus);

                // 콘솔 후킹 초기화
                if (Options.EnableConsoleCapture)
                {
                    _consoleHook = new ConsoleHook(_store, Options, EventBus);
                    _traceHook = new TraceHook(_store, Options, EventBus);
                }

                // 성능 캡처 초기화
                if (Options.EnablePerformanceCapture)
                {
                    _performanceInterceptor = new PerformanceInterceptor(_store, Options, EventBus);
                }

                // 애플리케이션 정보 캡처 초기화
                if (Options.EnableApplicationCapture)
                {
                    _applicationInterceptor = new ApplicationInterceptor(_store, Options, EventBus);
                }

#if !NET35
                // 웹 서버 시작
                _server = new HttpListenerServer(_store, Options, EventBus);
                _server.Start();

                if (Options.AutoOpenBrowser) OpenBrowser(Options.GetDevToolsUrl());
#endif

                _initialized = true;

                Console.WriteLine("SharpInspect DevTools initialized at " + Options.GetDevToolsUrl());
            }
        }

        /// <summary>
        ///     기본 브라우저에서 DevTools를 엽니다.
        /// </summary>
        public static void OpenDevTools()
        {
            if (!_initialized || Options == null)
                return;

            OpenBrowser(Options.GetDevToolsUrl());
        }

        /// <summary>
        ///     SharpInspect를 종료하고 리소스를 해제합니다.
        /// </summary>
        public static void Shutdown()
        {
            lock (_lock)
            {
                if (!_initialized)
                    return;

#if !NET35
                _server?.Dispose();
                _server = null;
#endif

                _consoleHook?.Dispose();
                _consoleHook = null;

                _traceHook?.Dispose();
                _traceHook = null;

                _performanceInterceptor?.Dispose();
                _performanceInterceptor = null;

                _applicationInterceptor?.Dispose();
                _applicationInterceptor = null;

                _store?.ClearAll();
                EventBus?.ClearAll();

                _initialized = false;
                _disabled = false;
            }
        }

        private static void OpenBrowser(string url)
        {
            try
            {
                // 앱 모드가 활성화된 경우 Chrome/Edge 앱 모드로 시도
                if (Options.OpenInAppMode && TryOpenInAppMode(url))
                    return;

                // 기본 브라우저로 열기
                OpenInDefaultBrowser(url);
            }
            catch
            {
                // 브라우저 열기 실패 무시
            }
        }

        /// <summary>
        ///     Chrome 또는 Edge의 앱 모드로 URL을 엽니다.
        /// </summary>
        private static bool TryOpenInAppMode(string url)
        {
            // Chrome/Edge 실행 파일 경로 목록 (Windows 기준)
            var browserPaths = new[]
            {
                // Chrome 경로들
                Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\Google\Chrome\Application\chrome.exe"),
                Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Google\Chrome\Application\chrome.exe"),
                Environment.ExpandEnvironmentVariables(@"%LocalAppData%\Google\Chrome\Application\chrome.exe"),
                // Edge 경로들
                Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\Microsoft\Edge\Application\msedge.exe"),
                Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Microsoft\Edge\Application\msedge.exe")
            };

            foreach (var browserPath in browserPaths)
            {
                if (System.IO.File.Exists(browserPath))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = browserPath,
                            Arguments = $"--app={url}",
                            UseShellExecute = false
                        });
                        return true;
                    }
                    catch
                    {
                        // 이 경로 실패, 다음 경로 시도
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///     기본 브라우저로 URL을 엽니다.
        /// </summary>
        private static void OpenInDefaultBrowser(string url)
        {
#if NETFRAMEWORK
            Process.Start(url);
#else
            // 크로스 플랫폼 브라우저 열기
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                Process.Start("xdg-open", url);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Process.Start("open", url);
#endif
        }

#if !NET35
        /// <summary>
        ///     SharpInspect 인터셉션이 활성화된 새 HttpClient를 생성합니다.
        ///     비활성화 상태에서는 인터셉션 없는 일반 HttpClient를 반환합니다.
        /// </summary>
        public static HttpClient CreateHttpClient()
        {
            // 비활성화 상태에서는 인터셉션 없는 일반 HttpClient 반환
            if (_disabled)
                return new HttpClient();

            if (!_initialized)
                throw new InvalidOperationException("SharpInspect must be initialized before creating an HttpClient.");

            var handler = new SharpInspectHandler(_store, Options, EventBus);
            return new HttpClient(handler);
        }

        /// <summary>
        ///     HttpClient에서 사용할 수 있는 SharpInspectHandler를 생성합니다.
        ///     비활성화 상태에서는 일반 HttpClientHandler를 반환합니다.
        /// </summary>
        public static DelegatingHandler CreateHandler()
        {
            // 비활성화 상태에서는 패스스루 핸들러 반환
            if (_disabled)
                return new PassThroughHandler();

            if (!_initialized)
                throw new InvalidOperationException("SharpInspect must be initialized before creating a handler.");

            return new SharpInspectHandler(_store, Options, EventBus);
        }

        /// <summary>
        ///     커스텀 내부 핸들러를 사용하는 SharpInspectHandler를 생성합니다.
        ///     비활성화 상태에서는 내부 핸들러를 래핑한 패스스루 핸들러를 반환합니다.
        /// </summary>
        public static DelegatingHandler CreateHandler(HttpMessageHandler innerHandler)
        {
            // 비활성화 상태에서는 패스스루 핸들러 반환
            if (_disabled)
                return new PassThroughHandler(innerHandler);

            if (!_initialized)
                throw new InvalidOperationException("SharpInspect must be initialized before creating a handler.");

            return new SharpInspectHandler(_store, Options, EventBus, innerHandler);
        }

        /// <summary>
        ///     비활성화 상태에서 사용되는 패스스루 핸들러.
        ///     요청을 인터셉션 없이 그대로 전달합니다.
        /// </summary>
        private class PassThroughHandler : DelegatingHandler
        {
            public PassThroughHandler() : base(new HttpClientHandler())
            {
            }

            public PassThroughHandler(HttpMessageHandler innerHandler) : base(innerHandler)
            {
            }
        }
#endif
    }

    /// <summary>
    ///     Dispose 시 SharpInspect를 종료하는 일회용 래퍼 클래스.
    /// </summary>
    public class SharpInspectSession : IDisposable
    {
        private bool _disposed;

        /// <summary>
        ///     새 SharpInspect 세션을 생성하고 초기화합니다.
        /// </summary>
        public SharpInspectSession()
        {
            SharpInspectDevTools.Initialize();
        }

        /// <summary>
        ///     옵션을 지정하여 새 SharpInspect 세션을 생성하고 초기화합니다.
        /// </summary>
        public SharpInspectSession(SharpInspectOptions options)
        {
            SharpInspectDevTools.Initialize(options);
        }

        /// <summary>
        ///     설정 액션을 사용하여 새 SharpInspect 세션을 생성하고 초기화합니다.
        /// </summary>
        public SharpInspectSession(Action<SharpInspectOptions> configure)
        {
            SharpInspectDevTools.Initialize(configure);
        }

        /// <summary>
        ///     세션을 해제하고 SharpInspect를 종료합니다.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                SharpInspectDevTools.Shutdown();
            }
        }
    }
}
