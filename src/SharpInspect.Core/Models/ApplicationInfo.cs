using System;
using System.Collections.Generic;

namespace SharpInspect.Core.Models
{
    /// <summary>
    ///     애플리케이션의 현재 상태 정보를 나타내는 스냅샷 모델.
    /// </summary>
    public class ApplicationInfo
    {
        /// <summary>
        ///     고유 ID와 현재 타임스탬프로 새 ApplicationInfo를 생성합니다.
        /// </summary>
        public ApplicationInfo()
        {
            Id = Guid.NewGuid().ToString("N");
            Timestamp = DateTime.UtcNow;
            EnvironmentVariables = new Dictionary<string, string>();
            LoadedAssemblies = new List<AssemblyInfo>();
            CommandLineArgs = new List<string>();
        }

        /// <summary>
        ///     이 스냅샷의 고유 식별자.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     스냅샷이 캡처된 타임스탬프.
        /// </summary>
        public DateTime Timestamp { get; set; }

        // ── 앱 정보 (시작 시 캡처) ──

        /// <summary>
        ///     엔트리 어셈블리 이름.
        /// </summary>
        public string AssemblyName { get; set; }

        /// <summary>
        ///     엔트리 어셈블리 버전.
        /// </summary>
        public string AssemblyVersion { get; set; }

        /// <summary>
        ///     엔트리 어셈블리 파일 위치.
        /// </summary>
        public string AssemblyLocation { get; set; }

        /// <summary>
        ///     CLR/런타임 버전 문자열.
        /// </summary>
        public string RuntimeVersion { get; set; }

        /// <summary>
        ///     프레임워크 설명 (예: ".NET 8.0.1").
        /// </summary>
        public string FrameworkDescription { get; set; }

        /// <summary>
        ///     운영 체제 설명.
        /// </summary>
        public string OsDescription { get; set; }

        /// <summary>
        ///     프로세스 아키텍처 (x86, x64, Arm64 등).
        /// </summary>
        public string ProcessArchitecture { get; set; }

        /// <summary>
        ///     현재 프로세스 ID.
        /// </summary>
        public int ProcessId { get; set; }

        /// <summary>
        ///     머신 이름.
        /// </summary>
        public string MachineName { get; set; }

        /// <summary>
        ///     현재 사용자 이름.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        ///     프로세스 시작 시간 (UTC).
        /// </summary>
        public DateTime ProcessStartTime { get; set; }

        /// <summary>
        ///     현재 작업 디렉토리.
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        ///     커맨드 라인 인수 목록.
        /// </summary>
        public List<string> CommandLineArgs { get; set; }

        /// <summary>
        ///     논리 프로세서 수.
        /// </summary>
        public int ProcessorCount { get; set; }

        /// <summary>
        ///     서버 GC 모드 여부.
        /// </summary>
        public bool IsServerGC { get; set; }

        // ── 환경 변수 ──

        /// <summary>
        ///     환경 변수 키-값 쌍. 민감한 값은 마스킹됩니다.
        /// </summary>
        public Dictionary<string, string> EnvironmentVariables { get; set; }

        // ── 로드된 어셈블리 ──

        /// <summary>
        ///     현재 로드된 어셈블리 목록.
        /// </summary>
        public List<AssemblyInfo> LoadedAssemblies { get; set; }

        /// <summary>
        ///     로드된 어셈블리 수.
        /// </summary>
        public int LoadedAssemblyCount { get; set; }
    }

    /// <summary>
    ///     로드된 어셈블리의 정보를 나타냅니다.
    /// </summary>
    public class AssemblyInfo
    {
        /// <summary>
        ///     어셈블리 전체 이름.
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        ///     어셈블리 간단한 이름.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     어셈블리 버전.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        ///     어셈블리 파일 위치.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        ///     동적 어셈블리 여부.
        /// </summary>
        public bool IsDynamic { get; set; }

        /// <summary>
        ///     GAC(Global Assembly Cache)에서 로드되었는지 여부.
        ///     .NET Framework에서만 유효합니다.
        /// </summary>
        public bool IsGAC { get; set; }
    }
}
