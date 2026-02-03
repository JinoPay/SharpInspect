using System;
using System.Collections.Generic;

namespace SharpInspect.Core.Models
{
    /// <summary>
    /// Represents a captured HTTP network request/response.
    /// </summary>
    public class NetworkEntry
    {
        /// <summary>
        /// Unique identifier for this entry.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Timestamp when the request was initiated.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// HTTP method (GET, POST, PUT, DELETE, etc.).
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Full request URL.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Host portion of the URL.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Path portion of the URL.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Query string portion of the URL.
        /// </summary>
        public string QueryString { get; set; }

        /// <summary>
        /// HTTP status code of the response.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// HTTP status text (e.g., "OK", "Not Found").
        /// </summary>
        public string StatusText { get; set; }

        /// <summary>
        /// Request headers.
        /// </summary>
        public Dictionary<string, string> RequestHeaders { get; set; }

        /// <summary>
        /// Response headers.
        /// </summary>
        public Dictionary<string, string> ResponseHeaders { get; set; }

        /// <summary>
        /// Request body content.
        /// </summary>
        public string RequestBody { get; set; }

        /// <summary>
        /// Request content type.
        /// </summary>
        public string RequestContentType { get; set; }

        /// <summary>
        /// Request content length in bytes.
        /// </summary>
        public long RequestContentLength { get; set; }

        /// <summary>
        /// Response body content.
        /// </summary>
        public string ResponseBody { get; set; }

        /// <summary>
        /// Response content type.
        /// </summary>
        public string ResponseContentType { get; set; }

        /// <summary>
        /// Response content length in bytes.
        /// </summary>
        public long ResponseContentLength { get; set; }

        /// <summary>
        /// DNS lookup time in milliseconds.
        /// </summary>
        public double DnsLookupMs { get; set; }

        /// <summary>
        /// TCP connection time in milliseconds.
        /// </summary>
        public double TcpConnectMs { get; set; }

        /// <summary>
        /// TLS handshake time in milliseconds.
        /// </summary>
        public double TlsHandshakeMs { get; set; }

        /// <summary>
        /// Time spent sending the request in milliseconds.
        /// </summary>
        public double RequestSentMs { get; set; }

        /// <summary>
        /// Time waiting for first byte (TTFB) in milliseconds.
        /// </summary>
        public double WaitingMs { get; set; }

        /// <summary>
        /// Content download time in milliseconds.
        /// </summary>
        public double ContentDownloadMs { get; set; }

        /// <summary>
        /// Total request/response time in milliseconds.
        /// </summary>
        public double TotalMs { get; set; }

        /// <summary>
        /// Stack trace of the code that initiated this request.
        /// </summary>
        public string Initiator { get; set; }

        /// <summary>
        /// Protocol used (HTTP/1.1, HTTP/2, etc.).
        /// </summary>
        public string Protocol { get; set; }

        /// <summary>
        /// Indicates if the request resulted in an error.
        /// </summary>
        public bool IsError { get; set; }

        /// <summary>
        /// Error message if the request failed.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Creates a new NetworkEntry with a unique ID and current timestamp.
        /// </summary>
        public NetworkEntry()
        {
            Id = Guid.NewGuid().ToString("N");
            Timestamp = DateTime.UtcNow;
            RequestHeaders = new Dictionary<string, string>();
            ResponseHeaders = new Dictionary<string, string>();
        }
    }
}
