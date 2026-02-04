using System;
using System.Collections.Generic;
using SharpInspect.Core.Models;

namespace SharpInspect.Core.Configuration;

/// <summary>
///     개발 환경 감지 모드.
/// </summary>
public enum DevelopmentDetectionMode
{
    /// <summary>
    ///     환경 변수 우선, 디버거 연결 폴백 (기본값).
    ///     DOTNET_ENVIRONMENT 또는 ASPNETCORE_ENVIRONMENT 환경 변수를 먼저 확인하고,
    ///     설정되지 않은 경우 Debugger.IsAttached 상태로 판단합니다.
    /// </summary>
    Auto,

    /// <summary>
    ///     환경 변수만 확인.
    ///     DOTNET_ENVIRONMENT 또는 ASPNETCORE_ENVIRONMENT가 "Development"인 경우에만 개발 환경으로 판단합니다.
    ///     환경 변수가 설정되지 않은 경우 프로덕션으로 간주합니다.
    /// </summary>
    EnvironmentVariableOnly,

    /// <summary>
    ///     디버거 연결 상태만 확인.
    ///     Debugger.IsAttached가 true인 경우에만 개발 환경으로 판단합니다.
    /// </summary>
    DebuggerOnly,

    /// <summary>
    ///     커스텀 판별 함수 사용.
    ///     CustomDevelopmentCheck 함수를 통해 개발 환경 여부를 판단합니다.
    /// </summary>
    Custom
}

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
        OpenInAppMode = true;
        EnableNetworkCapture = true;
        EnableConsoleCapture = true;
        EnablePerformanceCapture = true;
        EnableApplicationCapture = true;
        ApplicationRefreshIntervalMs = 30000;
        MaxNetworkEntries = 1000;
        MaxBodySizeBytes = 1048576; // 1MB
        IgnoreUrlPatterns = [];
        CaptureRequestBody = true;
        CaptureResponseBody = true;
        MaxConsoleEntries = 5000;
        MaxPerformanceEntries = 2000;
        PerformanceCaptureIntervalMs = 1000;
        MinLogLevel = SharpInspectLogLevel.Trace;
        EnableInDevelopmentOnly = true;
        DevelopmentDetectionMode = DevelopmentDetectionMode.Auto;
        CustomDevelopmentCheck = null;
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
    ///     애플리케이션 정보 캡처 여부를 가져오거나 설정합니다.
    ///     기본값: true
    /// </summary>
    public bool EnableApplicationCapture { get; set; }

    /// <summary>
    ///     콘솔 출력 캡처 여부를 가져오거나 설정합니다.
    ///     기본값: true
    /// </summary>
    public bool EnableConsoleCapture { get; set; }

    /// <summary>
    ///     개발 환경에서만 활성화 여부를 가져오거나 설정합니다.
    ///     true인 경우, 개발 환경이 아니면 SharpInspect가 초기화되지 않습니다.
    ///     기본값: true
    /// </summary>
    public bool EnableInDevelopmentOnly { get; set; }

    /// <summary>
    ///     네트워크 요청 캡처 여부를 가져오거나 설정합니다.
    ///     기본값: true
    /// </summary>
    public bool EnableNetworkCapture { get; set; }

    /// <summary>
    ///     성능 메트릭 캡처 여부를 가져오거나 설정합니다.
    ///     기본값: true
    /// </summary>
    public bool EnablePerformanceCapture { get; set; }

    /// <summary>
    ///     브라우저를 앱 모드(주소창/탭 없는 독립 창)로 열지 여부를 가져오거나 설정합니다.
    ///     Chrome/Edge의 --app 플래그를 사용합니다.
    ///     앱 모드 실패 시 기본 브라우저로 폴백합니다.
    ///     기본값: true
    /// </summary>
    public bool OpenInAppMode { get; set; }

    /// <summary>
    ///     개발 환경 감지 모드를 가져오거나 설정합니다.
    ///     EnableInDevelopmentOnly가 true일 때 사용됩니다.
    ///     기본값: Auto (환경 변수 우선, 디버거 연결 폴백)
    /// </summary>
    public DevelopmentDetectionMode DevelopmentDetectionMode { get; set; }

    /// <summary>
    ///     커스텀 개발 환경 판별 함수를 가져오거나 설정합니다.
    ///     DevelopmentDetectionMode가 Custom일 때 사용됩니다.
    ///     함수가 true를 반환하면 개발 환경으로 판단합니다.
    ///     기본값: null
    /// </summary>
    public Func<bool> CustomDevelopmentCheck { get; set; }

    /// <summary>
    ///     애플리케이션 정보 갱신 간격(밀리초)을 가져오거나 설정합니다.
    ///     주로 로드된 어셈블리 목록 갱신에 사용됩니다.
    ///     기본값: 30000 (30초)
    /// </summary>
    public int ApplicationRefreshIntervalMs { get; set; }

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
    ///     현재 옵션의 복사본을 생성합니다.
    /// </summary>
    public SharpInspectOptions Clone()
    {
        var clone = new SharpInspectOptions
        {
            Port = Port,
            Host = Host,
            AutoOpenBrowser = AutoOpenBrowser,
            OpenInAppMode = OpenInAppMode,
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
            DevelopmentDetectionMode = DevelopmentDetectionMode,
            CustomDevelopmentCheck = CustomDevelopmentCheck,
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
        return $"http://{Host}:{Port}";
    }
}