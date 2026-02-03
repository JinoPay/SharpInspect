using System.Collections.Generic;
using SharpInspect.Core.Models;

namespace SharpInspect.Core.Configuration
{
    /// <summary>
    ///     SharpInspect 설정 옵션.
    /// </summary>
    public class SharpInspectOptions
    {
        /// <summary>
        ///     기본값으로 새 SharpInspectOptions를 생성합니다.
        /// </summary>
        public SharpInspectOptions()
        {
            Port = 9229;
            Host = "localhost";
            AutoOpenBrowser = false;
            EnableHotkey = true;
            Hotkey = "F12";
            EnableNetworkCapture = true;
            EnableConsoleCapture = true;
            EnablePerformanceCapture = true;
            EnableApplicationCapture = true;
            ApplicationRefreshIntervalMs = 30000;
            MaxNetworkEntries = 1000;
            MaxBodySizeBytes = 1048576; // 1MB
            IgnoreUrlPatterns = new List<string>();
            CaptureRequestBody = true;
            CaptureResponseBody = true;
            MaxConsoleEntries = 5000;
            MaxPerformanceEntries = 2000;
            PerformanceCaptureIntervalMs = 1000;
            MinLogLevel = SharpInspectLogLevel.Trace;
            EnableInDevelopmentOnly = true;
            AccessToken = null;
            MaskedHeaders = new List<string> { "Authorization", "Cookie", "Set-Cookie" };
        }

        /// <summary>
        ///     SharpInspect 시작 시 브라우저를 자동으로 열지 여부를 가져오거나 설정합니다.
        ///     기본값: false
        /// </summary>
        public bool AutoOpenBrowser { get; set; }

        /// <summary>
        ///     요청 본문 캡처 여부를 가져오거나 설정합니다.
        ///     기본값: true
        /// </summary>
        public bool CaptureRequestBody { get; set; }

        /// <summary>
        ///     응답 본문 캡처 여부를 가져오거나 설정합니다.
        ///     기본값: true
        /// </summary>
        public bool CaptureResponseBody { get; set; }

        /// <summary>
        ///     콘솔 출력 캡처 여부를 가져오거나 설정합니다.
        ///     기본값: true
        /// </summary>
        public bool EnableConsoleCapture { get; set; }

        /// <summary>
        ///     글로벌 핫키 기능 활성화 여부를 가져오거나 설정합니다.
        ///     기본값: true
        /// </summary>
        public bool EnableHotkey { get; set; }

        /// <summary>
        ///     개발 환경에서만 활성화 여부를 가져오거나 설정합니다.
        ///     기본값: true
        /// </summary>
        public bool EnableInDevelopmentOnly { get; set; }

        /// <summary>
        ///     네트워크 요청 캡처 여부를 가져오거나 설정합니다.
        ///     기본값: true
        /// </summary>
        public bool EnableNetworkCapture { get; set; }

        /// <summary>
        ///     애플리케이션 정보 캡처 여부를 가져오거나 설정합니다.
        ///     기본값: true
        /// </summary>
        public bool EnableApplicationCapture { get; set; }

        /// <summary>
        ///     성능 메트릭 캡처 여부를 가져오거나 설정합니다.
        ///     기본값: true
        /// </summary>
        public bool EnablePerformanceCapture { get; set; }

        /// <summary>
        ///     캡처할 최대 본문 크기(바이트)를 가져오거나 설정합니다.
        ///     기본값: 1MB (1048576)
        /// </summary>
        public int MaxBodySizeBytes { get; set; }

        /// <summary>
        ///     저장할 최대 콘솔 엔트리 수를 가져오거나 설정합니다.
        ///     기본값: 5000
        /// </summary>
        public int MaxConsoleEntries { get; set; }

        /// <summary>
        ///     저장할 최대 네트워크 엔트리 수를 가져오거나 설정합니다.
        ///     기본값: 1000
        /// </summary>
        public int MaxNetworkEntries { get; set; }

        /// <summary>
        ///     저장할 최대 성능 엔트리 수를 가져오거나 설정합니다.
        ///     기본값: 2000
        /// </summary>
        public int MaxPerformanceEntries { get; set; }

        /// <summary>
        ///     애플리케이션 정보 갱신 간격(밀리초)을 가져오거나 설정합니다.
        ///     주로 로드된 어셈블리 목록 갱신에 사용됩니다.
        ///     기본값: 30000 (30초)
        /// </summary>
        public int ApplicationRefreshIntervalMs { get; set; }

        /// <summary>
        ///     성능 메트릭 캡처 간격(밀리초)을 가져오거나 설정합니다.
        ///     기본값: 1000 (1초)
        /// </summary>
        public int PerformanceCaptureIntervalMs { get; set; }

        /// <summary>
        ///     임베디드 웹 서버 포트를 가져오거나 설정합니다.
        ///     기본값: 9229
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        ///     네트워크 캡처에서 무시할 URL 패턴을 가져오거나 설정합니다.
        /// </summary>
        public List<string> IgnoreUrlPatterns { get; set; }

        /// <summary>
        ///     캡처에서 마스킹할 헤더 이름을 가져오거나 설정합니다.
        ///     기본값: Authorization, Cookie, Set-Cookie
        /// </summary>
        public List<string> MaskedHeaders { get; set; }

        /// <summary>
        ///     캡처할 최소 로그 레벨을 가져오거나 설정합니다.
        ///     기본값: Trace
        /// </summary>
        public SharpInspectLogLevel MinLogLevel { get; set; }

        /// <summary>
        ///     기본 인증용 선택적 액세스 토큰을 가져오거나 설정합니다.
        ///     기본값: null (인증 없음)
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        ///     임베디드 웹 서버 호스트를 가져오거나 설정합니다.
        ///     기본값: localhost
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        ///     DevTools를 여는 핫키를 가져오거나 설정합니다.
        ///     기본값: F12
        /// </summary>
        public string Hotkey { get; set; }

        /// <summary>
        ///     현재 옵션의 복사본을 생성합니다.
        /// </summary>
        public SharpInspectOptions Clone()
        {
            var clone = new SharpInspectOptions
            {
                Port = Port,
                Host = Host,
                AutoOpenBrowser = AutoOpenBrowser,
                EnableHotkey = EnableHotkey,
                Hotkey = Hotkey,
                EnableNetworkCapture = EnableNetworkCapture,
                EnableConsoleCapture = EnableConsoleCapture,
                EnablePerformanceCapture = EnablePerformanceCapture,
                EnableApplicationCapture = EnableApplicationCapture,
                ApplicationRefreshIntervalMs = ApplicationRefreshIntervalMs,
                MaxNetworkEntries = MaxNetworkEntries,
                MaxBodySizeBytes = MaxBodySizeBytes,
                CaptureRequestBody = CaptureRequestBody,
                CaptureResponseBody = CaptureResponseBody,
                MaxConsoleEntries = MaxConsoleEntries,
                MaxPerformanceEntries = MaxPerformanceEntries,
                PerformanceCaptureIntervalMs = PerformanceCaptureIntervalMs,
                MinLogLevel = MinLogLevel,
                EnableInDevelopmentOnly = EnableInDevelopmentOnly,
                AccessToken = AccessToken
            };

            clone.IgnoreUrlPatterns = new List<string>(IgnoreUrlPatterns);
            clone.MaskedHeaders = new List<string>(MaskedHeaders);

            return clone;
        }

        /// <summary>
        ///     DevTools UI의 전체 URL을 가져옵니다.
        /// </summary>
        public string GetDevToolsUrl()
        {
            return string.Format("http://{0}:{1}", Host, Port);
        }
    }
}
