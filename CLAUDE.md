# CLAUDE.md - SharpInspect

## 프로젝트 개요

SharpInspect는 .NET 애플리케이션용 Chrome DevTools 스타일 진단 인스펙터입니다.
임베디드 웹 서버를 통해 HTTP 요청/응답, 콘솔/로그 출력, 성능 메트릭, 애플리케이션 정보를 실시간으로 모니터링합니다.

## 기술 스택

- **언어**: C# 14
- **대상 프레임워크**: .NET Framework 3.5/4.6.2, .NET Standard 2.0, .NET 6.0/8.0/9.0
- **빌드**: `dotnet build SharpInspect.sln`
- **샘플 실행**: `dotnet run --project samples/Sample.ConsoleApp`
- **Nullable**: 비활성화
- **암시적 using**: 비활성화
- **라이선스**: MIT

## 솔루션 구조

```
SharpInspect.sln
├── src/
│   ├── SharpInspect.Core/        # 핵심 모델, 스토리지, 인터셉터, 이벤트
│   ├── SharpInspect.Server/      # 임베디드 웹 서버 (REST API, WebSocket, 정적 파일)
│   └── SharpInspect/             # 공개 API 및 통합 진입점
├── samples/
│   ├── Sample.ConsoleApp/        # .NET 8 콘솔 앱 예제
│   └── Sample.WinForms/          # .NET Framework 4.6.2 WinForms 예제
└── tests/                        # 테스트 (미구현)
```

## 핵심 아키텍처

### 네임스페이스 및 역할

| 네임스페이스 | 역할 |
|---|---|
| `SharpInspect` | 정적 진입점 (`SharpInspectDevTools`), DI 확장, 세션 래퍼 |
| `SharpInspect.Core.Configuration` | `SharpInspectOptions` 설정 클래스, `DevelopmentDetectionMode` 열거형 |
| `SharpInspect.Core.Models` | `NetworkEntry`, `ConsoleEntry`, `PerformanceEntry`, `ApplicationInfo` 데이터 모델 |
| `SharpInspect.Core.Storage` | `InMemoryStore`, `RingBuffer` - 링 버퍼 기반 저장소 |
| `SharpInspect.Core.Events` | `EventBus` - 발행/구독 이벤트 시스템, `ApplicationInfoEvent` |
| `SharpInspect.Core.Interceptors` | HTTP 인터셉터 (`SharpInspectHandler`, `HttpWebRequestInterceptor`), `PerformanceInterceptor`, `ApplicationInterceptor` |
| `SharpInspect.Core.Logging` | `ConsoleHook`, `TraceHook`, `SharpInspectLogger` |
| `SharpInspect.Core.EnvironmentDetection` | `DevelopmentEnvironmentDetector` - 개발 환경 감지 |
| `SharpInspect.Server.WebServer` | `HttpListenerServer` - HTTP 리스너 기반 웹 서버 |
| `SharpInspect.Server.WebSocket` | `WebSocketManager` - WebSocket 연결 관리 |
| `SharpInspect.Server.Api` | API 응답 DTO (`ApiResponse`, `PagedResponse<T>`) |
| `SharpInspect.Server.Json` | `SimpleJson` - 외부 의존성 없는 JSON 직렬화 |
| `SharpInspect.Server.StaticFiles` | `EmbeddedResourceProvider` - 임베디드 UI 리소스 제공 |

### 설계 패턴

- **싱글턴**: `SharpInspectDevTools` 정적 클래스, `EventBus.Instance`
- **옵저버**: EventBus 발행/구독으로 실시간 업데이트
- **링 버퍼**: 고정 용량 순환 버퍼로 무한 메모리 증가 방지
- **핸들러/미들웨어**: `DelegatingHandler`로 HttpClient 인터셉션
- **팩토리**: `CreateHttpClient()`, `CreateHandler()` 메서드

### 조건부 컴파일 심볼

| 심볼 | 대상 |
|---|---|
| `NET35` / `LEGACY` | .NET Framework 3.5 |
| `NETFRAMEWORK` | .NET Framework 4.x |
| `NETSTANDARD2_0` | .NET Standard 2.0 |
| `MODERN_DOTNET` | .NET 6.0+ |

## REST API 엔드포인트

기본 주소: `http://localhost:9229`

### 상태 및 설정
- `GET /api/status` - 서버 상태 (업타임, 엔트리 수, WebSocket 클라이언트 수)

### Network API
- `GET /api/network?offset=&limit=` - 네트워크 엔트리 페이징 조회
- `GET /api/network/{id}` - 특정 네트워크 엔트리 상세
- `POST /api/network/clear` - 네트워크 로그 초기화

### Console API
- `GET /api/console?offset=&limit=` - 콘솔 엔트리 페이징 조회
- `POST /api/console/clear` - 콘솔 로그 초기화

### Performance API
- `GET /api/performance?offset=&limit=` - 성능 엔트리 페이징 조회
- `POST /api/performance/clear` - 성능 로그 초기화

### Application API
- `GET /api/application` - 애플리케이션 정보 (앱 정보, 환경 변수, 로드된 어셈블리)

### WebSocket & UI
- `GET /ws` - WebSocket 실시간 이벤트 스트리밍
- `GET /` - DevTools UI (임베디드 정적 리소스)

## 코딩 컨벤션

- XML 문서 주석(`///`)을 모든 공개 API에 작성
- 모든 주석은 **한국어**로 작성
- 스레드 안전성 확보: `lock` 기반 동기화 사용
- 다중 프레임워크 지원: `#if` 조건부 컴파일로 프레임워크별 구현 분리
- 외부 의존성 최소화 (JSON 직렬화도 자체 구현)
- `IDisposable` 패턴으로 리소스 정리

## 주요 설정 (SharpInspectOptions)

### 서버 설정
| 속성 | 기본값 | 설명 |
|---|---|---|
| `Port` | 9229 | HTTP 리스너 포트 |
| `Host` | "localhost" | HTTP 리스너 호스트 |
| `AutoOpenBrowser` | false | 시작 시 브라우저 자동 열기 |
| `OpenInAppMode` | true | 브라우저를 앱 모드로 열기 (Chrome/Edge --app 플래그) |
| `AccessToken` | null | Bearer 토큰 인증 (선택) |

### 캡처 활성화
| 속성 | 기본값 | 설명 |
|---|---|---|
| `EnableNetworkCapture` | true | 네트워크 요청 캡처 |
| `EnableConsoleCapture` | true | 콘솔 출력 캡처 |
| `EnablePerformanceCapture` | true | 성능 메트릭 캡처 |
| `EnableApplicationCapture` | true | 애플리케이션 정보 캡처 |

### 저장소 설정
| 속성 | 기본값 | 설명 |
|---|---|---|
| `MaxNetworkEntries` | 1000 | 네트워크 엔트리 최대 보관 수 |
| `MaxConsoleEntries` | 5000 | 콘솔 엔트리 최대 보관 수 |
| `MaxPerformanceEntries` | 2000 | 성능 엔트리 최대 보관 수 |
| `MaxBodySizeBytes` | 1MB | 요청/응답 본문 최대 크기 |

### 캡처 상세 설정
| 속성 | 기본값 | 설명 |
|---|---|---|
| `CaptureRequestBody` | true | 요청 본문 캡처 |
| `CaptureResponseBody` | true | 응답 본문 캡처 |
| `PerformanceCaptureIntervalMs` | 1000 | 성능 메트릭 수집 간격(ms) |
| `ApplicationRefreshIntervalMs` | 30000 | 애플리케이션 정보 갱신 간격(ms) |
| `MinLogLevel` | Trace | 최소 로그 레벨 |
| `IgnoreUrlPatterns` | [] | 무시할 URL 패턴 |
| `MaskedHeaders` | ["Authorization", "Cookie", "Set-Cookie"] | 마스킹할 헤더 |

### 개발 환경 전용 설정
| 속성 | 기본값 | 설명 |
|---|---|---|
| `EnableInDevelopmentOnly` | true | 개발 환경에서만 활성화 |
| `DevelopmentDetectionMode` | Auto | 개발 환경 감지 모드 |
| `CustomDevelopmentCheck` | null | 커스텀 개발 환경 판별 함수 |

### DevelopmentDetectionMode 열거형
| 값 | 설명 |
|---|---|
| `Auto` | 환경 변수 우선, 디버거 연결 폴백 (기본값) |
| `EnvironmentVariableOnly` | DOTNET_ENVIRONMENT/ASPNETCORE_ENVIRONMENT만 확인 |
| `DebuggerOnly` | Debugger.IsAttached만 확인 |
| `Custom` | CustomDevelopmentCheck 함수 사용 |
