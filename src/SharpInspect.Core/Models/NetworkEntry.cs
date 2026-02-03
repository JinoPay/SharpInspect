using System;
using System.Collections.Generic;

namespace SharpInspect.Core.Models
{
    /// <summary>
    ///     캡처된 HTTP 네트워크 요청/응답을 나타냅니다.
    /// </summary>
    public class NetworkEntry
    {
        /// <summary>
        ///     고유 ID와 현재 타임스탬프로 새 NetworkEntry를 생성합니다.
        /// </summary>
        public NetworkEntry()
        {
            Id = Guid.NewGuid().ToString("N");
            Timestamp = DateTime.UtcNow;
            RequestHeaders = new Dictionary<string, string>();
            ResponseHeaders = new Dictionary<string, string>();
        }

        /// <summary>
        ///     요청이 오류로 끝났는지 여부.
        /// </summary>
        public bool IsError { get; set; }

        /// <summary>
        ///     요청이 시작된 타임스탬프.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        ///     요청 헤더.
        /// </summary>
        public Dictionary<string, string> RequestHeaders { get; set; }

        /// <summary>
        ///     응답 헤더.
        /// </summary>
        public Dictionary<string, string> ResponseHeaders { get; set; }

        /// <summary>
        ///     콘텐츠 다운로드 시간(밀리초).
        /// </summary>
        public double ContentDownloadMs { get; set; }

        /// <summary>
        ///     DNS 조회 시간(밀리초).
        /// </summary>
        public double DnsLookupMs { get; set; }

        /// <summary>
        ///     요청 전송에 소요된 시간(밀리초).
        /// </summary>
        public double RequestSentMs { get; set; }

        /// <summary>
        ///     TCP 연결 시간(밀리초).
        /// </summary>
        public double TcpConnectMs { get; set; }

        /// <summary>
        ///     TLS 핸드셰이크 시간(밀리초).
        /// </summary>
        public double TlsHandshakeMs { get; set; }

        /// <summary>
        ///     총 요청/응답 시간(밀리초).
        /// </summary>
        public double TotalMs { get; set; }

        /// <summary>
        ///     첫 번째 바이트 대기 시간(TTFB, 밀리초).
        /// </summary>
        public double WaitingMs { get; set; }

        /// <summary>
        ///     응답의 HTTP 상태 코드.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        ///     요청 콘텐츠 길이(바이트).
        /// </summary>
        public long RequestContentLength { get; set; }

        /// <summary>
        ///     응답 콘텐츠 길이(바이트).
        /// </summary>
        public long ResponseContentLength { get; set; }

        /// <summary>
        ///     요청 실패 시 오류 메시지.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        ///     URL의 호스트 부분.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        ///     이 엔트리의 고유 식별자.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     이 요청을 시작한 코드의 스택 트레이스.
        /// </summary>
        public string Initiator { get; set; }

        /// <summary>
        ///     HTTP 메서드 (GET, POST, PUT, DELETE 등).
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        ///     URL의 경로 부분.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        ///     사용된 프로토콜 (HTTP/1.1, HTTP/2 등).
        /// </summary>
        public string Protocol { get; set; }

        /// <summary>
        ///     URL의 쿼리 문자열 부분.
        /// </summary>
        public string QueryString { get; set; }

        /// <summary>
        ///     요청 본문 내용.
        /// </summary>
        public string RequestBody { get; set; }

        /// <summary>
        ///     요청 콘텐츠 타입.
        /// </summary>
        public string RequestContentType { get; set; }

        /// <summary>
        ///     응답 본문 내용.
        /// </summary>
        public string ResponseBody { get; set; }

        /// <summary>
        ///     응답 콘텐츠 타입.
        /// </summary>
        public string ResponseContentType { get; set; }

        /// <summary>
        ///     HTTP 상태 텍스트.
        /// </summary>
        public string StatusText { get; set; }

        /// <summary>
        ///     전체 요청 URL.
        /// </summary>
        public string Url { get; set; }
    }
}
