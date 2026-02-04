using System;

namespace SharpInspect.Server.Har
{
    /// <summary>
    ///     HAR 1.2 형식의 루트 객체.
    /// </summary>
    public class HarRoot
    {
        /// <summary>
        ///     HAR 로그 객체.
        /// </summary>
        public HarLog Log { get; set; }
    }

    /// <summary>
    ///     HAR 로그 객체.
    /// </summary>
    public class HarLog
    {
        /// <summary>
        ///     HAR 형식 버전 (1.2).
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        ///     HAR을 생성한 도구 정보.
        /// </summary>
        public HarCreator Creator { get; set; }

        /// <summary>
        ///     HTTP 요청/응답 엔트리 목록.
        /// </summary>
        public HarEntry[] Entries { get; set; }
    }

    /// <summary>
    ///     HAR 생성자 정보.
    /// </summary>
    public class HarCreator
    {
        /// <summary>
        ///     생성자 이름.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     생성자 버전.
        /// </summary>
        public string Version { get; set; }
    }

    /// <summary>
    ///     HAR 엔트리 (단일 HTTP 트랜잭션).
    /// </summary>
    public class HarEntry
    {
        /// <summary>
        ///     요청이 시작된 시간 (ISO 8601 형식).
        /// </summary>
        public string StartedDateTime { get; set; }

        /// <summary>
        ///     총 요청/응답 시간(밀리초).
        /// </summary>
        public int Time { get; set; }

        /// <summary>
        ///     요청 정보.
        /// </summary>
        public HarRequest Request { get; set; }

        /// <summary>
        ///     응답 정보.
        /// </summary>
        public HarResponse Response { get; set; }

        /// <summary>
        ///     캐시 정보.
        /// </summary>
        public HarCache Cache { get; set; }

        /// <summary>
        ///     타이밍 정보.
        /// </summary>
        public HarTimings Timings { get; set; }

        /// <summary>
        ///     주석 (오류 메시지 등).
        /// </summary>
        public string Comment { get; set; }
    }

    /// <summary>
    ///     HAR 요청 정보.
    /// </summary>
    public class HarRequest
    {
        /// <summary>
        ///     HTTP 메서드.
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        ///     요청 URL.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        ///     HTTP 버전.
        /// </summary>
        public string HttpVersion { get; set; }

        /// <summary>
        ///     쿠키 목록.
        /// </summary>
        public HarCookie[] Cookies { get; set; }

        /// <summary>
        ///     헤더 목록.
        /// </summary>
        public HarNameValuePair[] Headers { get; set; }

        /// <summary>
        ///     쿼리스트링 파라미터 목록.
        /// </summary>
        public HarNameValuePair[] QueryString { get; set; }

        /// <summary>
        ///     POST 데이터.
        /// </summary>
        public HarPostData PostData { get; set; }

        /// <summary>
        ///     헤더 크기(바이트). 알 수 없으면 -1.
        /// </summary>
        public int HeadersSize { get; set; }

        /// <summary>
        ///     본문 크기(바이트). 알 수 없으면 -1.
        /// </summary>
        public int BodySize { get; set; }
    }

    /// <summary>
    ///     HAR 응답 정보.
    /// </summary>
    public class HarResponse
    {
        /// <summary>
        ///     HTTP 상태 코드.
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        ///     HTTP 상태 텍스트.
        /// </summary>
        public string StatusText { get; set; }

        /// <summary>
        ///     HTTP 버전.
        /// </summary>
        public string HttpVersion { get; set; }

        /// <summary>
        ///     쿠키 목록.
        /// </summary>
        public HarCookie[] Cookies { get; set; }

        /// <summary>
        ///     헤더 목록.
        /// </summary>
        public HarNameValuePair[] Headers { get; set; }

        /// <summary>
        ///     응답 콘텐츠.
        /// </summary>
        public HarContent Content { get; set; }

        /// <summary>
        ///     리다이렉트 URL.
        /// </summary>
        public string RedirectURL { get; set; }

        /// <summary>
        ///     헤더 크기(바이트). 알 수 없으면 -1.
        /// </summary>
        public int HeadersSize { get; set; }

        /// <summary>
        ///     본문 크기(바이트). 알 수 없으면 -1.
        /// </summary>
        public int BodySize { get; set; }
    }

    /// <summary>
    ///     HAR 이름-값 쌍 (헤더, 쿼리스트링용).
    /// </summary>
    public class HarNameValuePair
    {
        /// <summary>
        ///     이름.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     값.
        /// </summary>
        public string Value { get; set; }
    }

    /// <summary>
    ///     HAR 쿠키 정보.
    /// </summary>
    public class HarCookie
    {
        /// <summary>
        ///     쿠키 이름.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     쿠키 값.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        ///     쿠키 경로.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        ///     쿠키 도메인.
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        ///     만료 시간 (ISO 8601 형식).
        /// </summary>
        public string Expires { get; set; }

        /// <summary>
        ///     HttpOnly 플래그.
        /// </summary>
        public bool HttpOnly { get; set; }

        /// <summary>
        ///     Secure 플래그.
        /// </summary>
        public bool Secure { get; set; }
    }

    /// <summary>
    ///     HAR POST 데이터.
    /// </summary>
    public class HarPostData
    {
        /// <summary>
        ///     MIME 타입.
        /// </summary>
        public string MimeType { get; set; }

        /// <summary>
        ///     본문 텍스트.
        /// </summary>
        public string Text { get; set; }
    }

    /// <summary>
    ///     HAR 응답 콘텐츠.
    /// </summary>
    public class HarContent
    {
        /// <summary>
        ///     콘텐츠 크기(바이트).
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        ///     MIME 타입.
        /// </summary>
        public string MimeType { get; set; }

        /// <summary>
        ///     콘텐츠 텍스트.
        /// </summary>
        public string Text { get; set; }
    }

    /// <summary>
    ///     HAR 캐시 정보. SharpInspect에서는 캐시를 추적하지 않으므로 빈 객체.
    /// </summary>
    public class HarCache
    {
    }

    /// <summary>
    ///     HAR 타이밍 정보. 모든 값은 밀리초 단위.
    ///     -1은 해당 타이밍이 적용되지 않음을 의미.
    /// </summary>
    public class HarTimings
    {
        /// <summary>
        ///     연결 대기 시간. SharpInspect에서 추적하지 않음.
        /// </summary>
        public int Blocked { get; set; }

        /// <summary>
        ///     DNS 조회 시간.
        /// </summary>
        public int Dns { get; set; }

        /// <summary>
        ///     TCP 연결 시간.
        /// </summary>
        public int Connect { get; set; }

        /// <summary>
        ///     요청 전송 시간.
        /// </summary>
        public int Send { get; set; }

        /// <summary>
        ///     첫 번째 바이트 대기 시간 (TTFB).
        /// </summary>
        public int Wait { get; set; }

        /// <summary>
        ///     콘텐츠 다운로드 시간.
        /// </summary>
        public int Receive { get; set; }

        /// <summary>
        ///     TLS 핸드셰이크 시간.
        /// </summary>
        public int Ssl { get; set; }
    }
}
