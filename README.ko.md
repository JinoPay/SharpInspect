# SharpInspect

**.NET 애플리케이션을 위한 Chrome DevTools 스타일 디버그 도구**

브라우저에서 F12를 누르면 Network 탭, Console 등을 볼 수 있듯이, .NET 앱에서도 네트워크 요청/응답, 로그 등을 실시간으로 확인할 수 있습니다.

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-4.6.2%20%7C%206.0%20%7C%208.0%20%7C%209.0-purple.svg)

[English](README.md) | **한국어**

## ✨ 특징

- **프레임워크 무관**: WinForms, WPF, 콘솔 앱, ASP.NET Core 등 어디서든 동작
- **한 줄 설정**: `SharpInspectDevTools.Initialize()` 한 줄로 시작
- **실시간 모니터링**: WebSocket을 통한 실시간 데이터 스트리밍
- **Chrome DevTools 스타일 UI**: 익숙한 인터페이스
- **비침투적**: 프로덕션 코드에 영향 없음

## 📦 지원 플랫폼

| 플랫폼 | 버전 |
|--------|------|
| .NET Framework | 4.6.2 이상 |
| .NET | 6.0, 8.0, 9.0 |
| .NET Standard | 2.0 |

## 🚀 빠른 시작

### 1. 설치

```bash
# NuGet (준비 중)
dotnet add package SharpInspect

# 또는 프로젝트 참조
```

### 2. 초기화

```csharp
using SharpInspect;

// 앱 시작 시 초기화
SharpInspectDevTools.Initialize();

// 옵션 지정
SharpInspectDevTools.Initialize(options =>
{
    options.Port = 9229;                    // 기본 포트
    options.EnableNetworkCapture = true;    // 네트워크 캡처
    options.EnableConsoleCapture = true;    // 콘솔 로그 캡처
});
```

### 3. 브라우저에서 확인

```
http://localhost:9229
```

## 📖 사용법

### 콘솔 앱 예제

```csharp
using SharpInspect;

class Program
{
    static async Task Main()
    {
        // SharpInspect 초기화
        SharpInspectDevTools.Initialize();

        // HttpClient 생성 (자동 캡처)
        using var client = SharpInspectDevTools.CreateHttpClient();

        // HTTP 요청 - DevTools에서 확인 가능
        var response = await client.GetStringAsync("https://api.example.com/data");

        // 콘솔 로그 - DevTools Console 탭에서 확인 가능
        Console.WriteLine("데이터 수신 완료");

        // 종료
        SharpInspectDevTools.Shutdown();
    }
}
```

### WinForms / WPF 예제

```csharp
// Program.cs 또는 App.xaml.cs
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        SharpInspectDevTools.Initialize();
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SharpInspectDevTools.Shutdown();
        base.OnExit(e);
    }
}
```

### using 문으로 자동 종료

```csharp
using (new SharpInspectSession())
{
    // 앱 로직
    // 블록 종료 시 자동으로 Shutdown 호출
}
```

## 🔧 설정 옵션

```csharp
SharpInspectDevTools.Initialize(options =>
{
    // 서버 설정
    options.Port = 9229;                        // DevTools 포트
    options.Host = "localhost";                 // 호스트 (보안상 localhost 권장)
    options.AutoOpenBrowser = false;            // 자동 브라우저 열기

    // 캡처 설정
    options.EnableNetworkCapture = true;        // 네트워크 캡처 활성화
    options.EnableConsoleCapture = true;        // 콘솔 로그 캡처 활성화

    // 네트워크 설정
    options.MaxNetworkEntries = 1000;           // 최대 저장 요청 수
    options.MaxBodySizeBytes = 1048576;         // 최대 바디 크기 (1MB)
    options.CaptureRequestBody = true;          // 요청 바디 캡처
    options.CaptureResponseBody = true;         // 응답 바디 캡처
    options.IgnoreUrlPatterns.Add("health");    // 무시할 URL 패턴

    // 콘솔 설정
    options.MaxConsoleEntries = 5000;           // 최대 로그 수
    options.MinLogLevel = SharpInspectLogLevel.Trace;  // 최소 로그 레벨

    // 보안
    options.MaskedHeaders.Add("Authorization"); // 마스킹할 헤더
    options.AccessToken = "my-secret-token";    // 접근 토큰 (선택)
});
```

## 🔌 RestSharp / 커스텀 HTTP 클라이언트 통합

기존 HTTP 클라이언트(RestSharp 등)를 사용하는 경우, 인터셉터를 구현하여 통합할 수 있습니다:

```csharp
public class SharpInspectInterceptor : IRequestInterceptor
{
    public int Order => 50;

    public async Task<RestResponse> InterceptAsync(
        IRestClient client,
        RestRequest request,
        Func<Task<RestResponse>> next)
    {
        var entry = new NetworkEntry
        {
            Method = request.Method.ToString(),
            Url = client.BuildUri(request).ToString(),
            Timestamp = DateTime.UtcNow
        };

        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        entry.StatusCode = (int)response.StatusCode;
        entry.TotalMs = sw.ElapsedMilliseconds;
        entry.ResponseBody = response.Content;

        // SharpInspect에 기록
        SharpInspectDevTools.Store?.AddNetworkEntry(entry);

        return response;
    }
}
```

## 🖥️ DevTools UI 기능

### Network 탭
- 요청/응답 목록 (시간순)
- 상태 코드별 색상 구분 (2xx 초록, 4xx 주황, 5xx 빨강)
- 요청/응답 헤더 상세 보기
- 요청/응답 바디 (JSON 포맷팅)
- 타이밍 정보 (DNS, TCP, TLS, TTFB 등)
- 필터링 및 검색
- Clear 버튼

### Console 탭
- 로그 레벨별 색상 구분
- 실시간 스트리밍
- 필터링 및 검색
- 예외 스택 트레이스 표시

## 📁 프로젝트 구조

```
SharpInspect/
├── src/
│   ├── SharpInspect.Core/       # 핵심 로직 (모델, 스토리지, 이벤트)
│   ├── SharpInspect.Server/     # 내장 웹서버 (REST API, WebSocket)
│   └── SharpInspect/            # 통합 패키지 (진입점, DI 확장)
└── samples/
    ├── Sample.ConsoleApp/       # 콘솔 앱 예제 (.NET 8)
    └── Sample.WinForms/         # WinForms 예제 (.NET Framework 4.6.2)
```

## 🛠️ 빌드

```bash
# 전체 빌드
dotnet build SharpInspect.sln

# 샘플 실행
dotnet run --project samples/Sample.ConsoleApp
```

## 📋 API 엔드포인트

DevTools 서버가 제공하는 REST API:

| 엔드포인트 | 메서드 | 설명 |
|-----------|--------|------|
| `/api/status` | GET | 서버 상태 |
| `/api/network` | GET | 네트워크 엔트리 목록 |
| `/api/network/{id}` | GET | 특정 엔트리 상세 |
| `/api/network/clear` | POST | 네트워크 기록 초기화 |
| `/api/console` | GET | 콘솔 로그 목록 |
| `/api/console/clear` | POST | 콘솔 로그 초기화 |
| `/ws` | WebSocket | 실시간 이벤트 스트리밍 |

## 🔒 보안 고려사항

- 기본적으로 `localhost`에서만 접근 가능
- 프로덕션 환경에서는 비활성화 권장
- 민감한 헤더(Authorization, Cookie 등) 자동 마스킹
- 선택적 토큰 인증 지원

## 📝 로드맵

- [x] Network 탭 (HTTP 캡처)
- [x] Console 탭 (로그 캡처)
- [x] 실시간 WebSocket 스트리밍
- [x] Chrome DevTools 스타일 UI
- [ ] Performance 탭 (GC, 메모리, CPU)
- [ ] Application 탭 (앱 정보, 환경변수)
- [ ] F12 글로벌 핫키
- [ ] HAR 형식 내보내기
- [ ] 커스텀 패널 플러그인 시스템

## 🤝 기여

이슈와 PR을 환영합니다!

## 📄 라이선스

MIT License

---

## 💡 왜 SharpInspect인가?

### 기존 도구들의 한계

| 도구 | 문제점 |
|------|--------|
| Fiddler | 외부 프록시 도구, 앱과 별도 실행 필요 |
| Charles | 유료, 설정 복잡 |
| Visual Studio 디버거 | 중단점 필요, 실시간 모니터링 어려움 |
| 로그 파일 | 실시간 확인 불가, 포맷팅 어려움 |

### SharpInspect의 장점

- ✅ 앱 내장형 - 별도 도구 불필요
- ✅ 실시간 - WebSocket으로 즉시 확인
- ✅ 익숙한 UI - Chrome DevTools 스타일
- ✅ 간편한 설정 - 한 줄 초기화
- ✅ 오픈소스 - 무료, 커스터마이징 가능
