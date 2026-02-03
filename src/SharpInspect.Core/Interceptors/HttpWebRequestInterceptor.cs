using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using SharpInspect.Core.Configuration;
using SharpInspect.Core.Events;
using SharpInspect.Core.Models;
using SharpInspect.Core.Storage;

namespace SharpInspect.Core.Interceptors
{
    /// <summary>
    ///     HttpWebRequest 작업을 인터셉트하기 위한 유틸리티 클래스.
    ///     .NET Framework 3.5+ 호환.
    /// </summary>
    public static class HttpWebRequestInterceptor
    {
        private static bool _initialized;
        private static EventBus _eventBus;
        private static ISharpInspectStore _store;
        private static readonly object _initLock = new();
        private static SharpInspectOptions _options;

        /// <summary>
        ///     요청/응답을 캡처하는 HttpWebRequest 래퍼를 생성합니다.
        /// </summary>
        public static HttpWebResponse GetResponseWithCapture(HttpWebRequest request)
        {
            if (!_initialized || !_options.EnableNetworkCapture) return (HttpWebResponse)request.GetResponse();

            if (ShouldIgnoreUrl(request.RequestUri)) return (HttpWebResponse)request.GetResponse();

            var entry = new NetworkEntry();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // 요청 캡처
                CaptureRequest(entry, request);

                // 응답 가져오기
                var response = (HttpWebResponse)request.GetResponse();

                stopwatch.Stop();

                // 응답 캡처
                CaptureResponse(entry, response, stopwatch.Elapsed);

                // 저장 및 발행
                StoreAndPublish(entry);

                return response;
            }
            catch (WebException ex)
            {
                stopwatch.Stop();

                if (ex.Response != null)
                {
                    CaptureResponse(entry, (HttpWebResponse)ex.Response, stopwatch.Elapsed);
                    entry.IsError = true;
                    entry.ErrorMessage = ex.Message;
                }
                else
                {
                    CaptureError(entry, ex, stopwatch.Elapsed);
                }

                StoreAndPublish(entry);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                CaptureError(entry, ex, stopwatch.Elapsed);
                StoreAndPublish(entry);
                throw;
            }
        }

        /// <summary>
        ///     HttpWebRequest의 동기 요청/응답 캡처를 래핑합니다.
        ///     사용법: GetResponse()를 수동 호출하기 전에 사용합니다.
        /// </summary>
        public static NetworkEntry CaptureRequestStart(HttpWebRequest request)
        {
            if (!_initialized || !_options.EnableNetworkCapture) return null;

            if (ShouldIgnoreUrl(request.RequestUri)) return null;

            var entry = new NetworkEntry();
            CaptureRequest(entry, request);
            return entry;
        }

        /// <summary>
        ///     응답 수신 후 캡처를 완료합니다.
        /// </summary>
        public static void CaptureRequestEnd(NetworkEntry entry, HttpWebResponse response, TimeSpan elapsed)
        {
            if (entry == null || !_initialized)
                return;

            CaptureResponse(entry, response, elapsed);
            StoreAndPublish(entry);
        }

        /// <summary>
        ///     오류 발생 후 캡처를 완료합니다.
        /// </summary>
        public static void CaptureRequestError(NetworkEntry entry, Exception ex, TimeSpan elapsed)
        {
            if (entry == null || !_initialized)
                return;

            CaptureError(entry, ex, elapsed);
            StoreAndPublish(entry);
        }

        /// <summary>
        ///     지정된 의존성으로 인터셉터를 초기화합니다.
        /// </summary>
        public static void Initialize(
            ISharpInspectStore store,
            SharpInspectOptions options,
            EventBus eventBus = null)
        {
            lock (_initLock)
            {
                _store = store;
                _options = options;
                _eventBus = eventBus ?? EventBus.Instance;
                _initialized = true;
            }
        }

        private static bool ShouldIgnoreUrl(Uri uri)
        {
            if (uri == null || _options == null || _options.IgnoreUrlPatterns == null)
                return false;

            var url = uri.ToString();
            foreach (var pattern in _options.IgnoreUrlPatterns)
                if (url.Contains(pattern))
                    return true;
            return false;
        }

        private static bool ShouldMaskHeader(string headerName)
        {
            if (_options == null || _options.MaskedHeaders == null)
                return false;

            foreach (var masked in _options.MaskedHeaders)
                if (string.Equals(headerName, masked, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private static string GetInitiator()
        {
            try
            {
                var stackTrace = new StackTrace(true);
                var frames = stackTrace.GetFrames();
                if (frames == null)
                    return null;

                var sb = new StringBuilder();

                foreach (var frame in frames)
                {
                    var method = frame.GetMethod();
                    if (method == null)
                        continue;

                    var declaringType = method.DeclaringType;
                    if (declaringType == null)
                        continue;

                    var typeName = declaringType.FullName ?? "";

                    // SharpInspect 및 System.Net 내부 프레임 건너뛰기
                    if (typeName.StartsWith("SharpInspect.") ||
                        typeName.StartsWith("System.Net"))
                        continue;

                    var fileName = frame.GetFileName();
                    var lineNumber = frame.GetFileLineNumber();

                    if (!string.IsNullOrEmpty(fileName))
                        sb.AppendFormat("at {0}.{1}() in {2}:line {3}\n",
                            typeName, method.Name, fileName, lineNumber);
                    else
                        sb.AppendFormat("at {0}.{1}()\n", typeName, method.Name);

                    // 처음 몇 개 프레임만 캡처
                    if (sb.Length > 500)
                        break;
                }

                return sb.Length > 0 ? sb.ToString().TrimEnd() : null;
            }
            catch
            {
                return null;
            }
        }

        private static string ReadResponseBody(HttpWebResponse response)
        {
            try
            {
                using (var stream = response.GetResponseStream())
                {
                    if (stream == null)
                        return null;

                    // 호출자가 다시 읽을 수 있도록 스트림을 버퍼링해야 함
                    // 이는 제한사항 - 큰 응답의 경우 캡처하지 않는 것을 고려
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch
            {
                return "[Unable to read response body]";
            }
        }

        private static void CaptureError(NetworkEntry entry, Exception ex, TimeSpan elapsed)
        {
            entry.IsError = true;
            entry.ErrorMessage = ex.Message;
            entry.TotalMs = elapsed.TotalMilliseconds;
        }

        private static void CaptureRequest(NetworkEntry entry, HttpWebRequest request)
        {
            entry.Method = request.Method;
            entry.Url = request.RequestUri.ToString();
            entry.Host = request.RequestUri.Host;
            entry.Path = request.RequestUri.AbsolutePath;
            entry.QueryString = request.RequestUri.Query;

            // 헤더 캡처
            if (request.Headers != null)
                foreach (var key in request.Headers.AllKeys)
                {
                    var value = request.Headers[key];
                    if (ShouldMaskHeader(key)) value = "***masked***";
                    entry.RequestHeaders[key] = value;
                }

            entry.RequestContentType = request.ContentType;
            entry.RequestContentLength = request.ContentLength;
            entry.Initiator = GetInitiator();
        }

        private static void CaptureResponse(NetworkEntry entry, HttpWebResponse response, TimeSpan elapsed)
        {
            entry.StatusCode = (int)response.StatusCode;
            entry.StatusText = response.StatusDescription;
            entry.TotalMs = elapsed.TotalMilliseconds;
            entry.Protocol = "HTTP/" + response.ProtocolVersion;

            // 헤더 캡처
            if (response.Headers != null)
                foreach (var key in response.Headers.AllKeys)
                {
                    var value = response.Headers[key];
                    if (ShouldMaskHeader(key)) value = "***masked***";
                    entry.ResponseHeaders[key] = value;
                }

            entry.ResponseContentType = response.ContentType;
            entry.ResponseContentLength = response.ContentLength;

            // 캡처가 활성화되어 있고 크기 제한 이내인 경우 응답 본문 캡처
            if (_options.CaptureResponseBody &&
                response.ContentLength <= _options.MaxBodySizeBytes &&
                response.ContentLength != -1)
                entry.ResponseBody = ReadResponseBody(response);
        }

        private static void StoreAndPublish(NetworkEntry entry)
        {
            if (_store != null) _store.AddNetworkEntry(entry);

            if (_eventBus != null)
            {
#if NET35
                // .NET 3.5용 동기 발행
                _eventBus.Publish(new NetworkEntryEvent(entry));
#else
                _eventBus.PublishAsync(new NetworkEntryEvent(entry));
#endif
            }
        }
    }
}
