# SharpInspect

**.NET 애플리케이션을 위한 Chrome DevTools 스타일 인스펙터**

HTTP 요청, 콘솔 로그, 성능 메트릭, 애플리케이션 정보를 실시간으로 모니터링합니다.

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-3.5%20%7C%204.6.2%20%7C%206.0%20%7C%208.0%20%7C%209.0-purple.svg)

[English](README.md) | **한국어**

## 특징

- **프레임워크 무관**: WinForms, WPF, 콘솔 앱, ASP.NET Core 등 어디서든 동작
- **한 줄 설정**: `SharpInspectDevTools.Initialize()` 한 줄로 시작
- **실시간 모니터링**: WebSocket을 통한 실시간 데이터 스트리밍
- **Chrome DevTools 스타일 UI**: 익숙한 인터페이스
- **외부 의존성 제로**: NuGet 패키지 없이 동작
- **개발 환경 전용**: 프로덕션에서는 자동으로 비활성화

## 지원 플랫폼

| 플랫폼 | 버전 |
|--------|------|
| .NET Framework | 3.5+, 4.6.2+ |
| .NET | 6.0, 8.0, 9.0 |
| .NET Standard | 2.0 |

## 빠른 시작

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
    options.Port = 9229;
    options.AutoOpenBrowser = true;
});
```

### 3. 브라우저에서 확인

```
http://localhost:9229
```

## 사용 예제

### 콘솔 앱

```csharp
using SharpInspect;

class Program
{
    static async Task Main()
    {
        SharpInspectDevTools.Initialize();

        // HttpClient 생성 (자동 캡처)
        using var client = SharpInspectDevTools.CreateHttpClient();

        // HTTP 요청 - DevTools Network 탭에서 확인
        var response = await client.GetStringAsync("https://api.example.com/data");

        // 콘솔 로그 - DevTools Console 탭에서 확인
        Console.WriteLine("데이터 수신 완료!");

        SharpInspectDevTools.Shutdown();
    }
}
```

### WinForms / WPF

```csharp
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

## 설정

```csharp
SharpInspectDevTools.Initialize(options =>
{
    // 서버 설정
    options.Port = 9229;
    options.Host = "localhost";
    options.AutoOpenBrowser = true;
    options.OpenInAppMode = true;  // 독립 창으로 열기 (Chrome/Edge)

    // 캡처 활성화
    options.EnableNetworkCapture = true;
    options.EnableConsoleCapture = true;
    options.EnablePerformanceCapture = true;
    options.EnableApplicationCapture = true;

    // 저장소 제한 (링 버퍼)
    options.MaxNetworkEntries = 1000;
    options.MaxConsoleEntries = 5000;
    options.MaxPerformanceEntries = 2000;
    options.MaxBodySizeBytes = 1048576;  // 1MB

    // 개발 환경 전용 모드 (기본값: 활성화)
    options.EnableInDevelopmentOnly = true;
    options.DevelopmentDetectionMode = DevelopmentDetectionMode.Auto;

    // 보안
    options.MaskedHeaders.Add("X-API-Key");
    options.AccessToken = "my-secret-token";
});
```

### 개발 환경 감지 모드

SharpInspect는 기본적으로 개발 환경에서만 동작합니다:

```csharp
// Auto (기본값): 환경 변수 우선 확인, 없으면 디버거 연결 상태 확인
options.DevelopmentDetectionMode = DevelopmentDetectionMode.Auto;

// 환경 변수만: DOTNET_ENVIRONMENT 또는 ASPNETCORE_ENVIRONMENT = "Development"
options.DevelopmentDetectionMode = DevelopmentDetectionMode.EnvironmentVariableOnly;

// 디버거만: Debugger.IsAttached
options.DevelopmentDetectionMode = DevelopmentDetectionMode.DebuggerOnly;

// 커스텀: 직접 로직 구현
options.DevelopmentDetectionMode = DevelopmentDetectionMode.Custom;
options.CustomDevelopmentCheck = () => MyConfig.IsDevMode;

// 모든 환경에서 강제 활성화
options.EnableInDevelopmentOnly = false;
```

## DevTools UI 기능

### Network 탭
- 요청/응답 목록 및 타이밍 정보
- 상태 코드별 색상 구분 (2xx 초록, 4xx 주황, 5xx 빨강)
- 헤더 및 바디 상세 보기 (JSON 포맷팅)
- 타이밍 분석 (DNS, TCP, TLS, TTFB)
- 필터링 및 검색
- Clear 버튼
- **Export HAR**: 네트워크 로그를 HAR(HTTP Archive) 형식으로 내보내기

### Console 탭
- 로그 레벨별 색상 구분
- 실시간 스트리밍
- 예외 스택 트레이스 표시
- 필터링 및 검색

### Performance 탭
- CPU 사용량 모니터링
- 메모리 메트릭 (Working Set, GC 힙)
- GC 수집 횟수
- 스레드 수 추적

### Application 탭
- 앱 정보 (이름, 버전, 런타임, PID)
- 환경 변수
- 로드된 어셈블리 목록

## REST API

| 엔드포인트 | 메서드 | 설명 |
|-----------|--------|------|
| `/api/status` | GET | 서버 상태 |
| `/api/network` | GET | 네트워크 엔트리 (페이징) |
| `/api/network/{id}` | GET | 특정 네트워크 엔트리 |
| `/api/network/clear` | POST | 네트워크 로그 초기화 |
| `/api/console` | GET | 콘솔 엔트리 (페이징) |
| `/api/console/clear` | POST | 콘솔 로그 초기화 |
| `/api/performance` | GET | 성능 엔트리 (페이징) |
| `/api/performance/clear` | POST | 성능 로그 초기화 |
| `/api/application` | GET | 애플리케이션 정보 |
| `/api/network/export/har` | GET | 네트워크 로그를 HAR로 내보내기 |
| `/ws` | WebSocket | 실시간 이벤트 스트림 |

## 프로젝트 구조

```
SharpInspect/
├── src/
│   ├── SharpInspect.Core/       # 핵심 모델, 스토리지, 이벤트, 인터셉터
│   ├── SharpInspect.Server/     # 임베디드 웹 서버 (REST API, WebSocket)
│   └── SharpInspect/            # 공개 API, DI 확장
└── samples/
    ├── Sample.ConsoleApp/       # .NET 8 콘솔 예제
    └── Sample.WinForms/         # .NET Framework 4.6.2 WinForms 예제
```

## 빌드

```bash
# 전체 빌드
dotnet build SharpInspect.sln

# 샘플 실행
dotnet run --project samples/Sample.ConsoleApp
```

## 보안 고려사항

- 기본적으로 `localhost`에서만 접근 가능
- 프로덕션에서 자동 비활성화 (EnableInDevelopmentOnly = true)
- 민감한 헤더 자동 마스킹 (Authorization, Cookie)
- 선택적 토큰 인증 지원

## 로드맵

### 완료
- [x] Network 탭 (HTTP 캡처 및 타이밍)
- [x] Console 탭 (로그 캡처)
- [x] Performance 탭 (CPU, 메모리, GC 메트릭)
- [x] Application 탭 (앱 정보, 환경 변수, 어셈블리)
- [x] 실시간 WebSocket 스트리밍
- [x] Chrome DevTools 스타일 UI
- [x] 개발 환경 전용 모드 (다양한 감지 전략)
- [x] 멀티 프레임워크 지원 (.NET Framework 3.5 ~ .NET 9.0)
- [x] 다크 모드 UI
- [x] HAR 내보내기

### 예정
- [ ] 커스텀 패널 플러그인 시스템
- [ ] 요청 재전송 (Replay)
- [ ] 성능 타임라인 뷰
- [ ] NuGet 패키지 배포

## 기여

이슈와 PR을 환영합니다!

## 라이선스

MIT License
