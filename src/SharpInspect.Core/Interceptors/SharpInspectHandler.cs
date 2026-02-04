using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharpInspect.Core.Configuration;
using SharpInspect.Core.Models;
using SharpInspect.Core.Storage;

namespace SharpInspect.Core.Interceptors
{
    /// <summary>
    ///     HTTP 요청/응답을 인터셉트하고 캡처하는 HttpClient DelegatingHandler.
    ///     .NET 4.5+ 및 .NET Standard 2.0+에서 사용 가능.
    /// </summary>
    public class SharpInspectHandler : DelegatingHandler
    {
        private readonly ISharpInspectStore _store;
        private readonly SharpInspectOptions _options;

        /// <summary>
        ///     지정된 의존성으로 새 SharpInspectHandler를 생성합니다.
        /// </summary>
        public SharpInspectHandler(
            ISharpInspectStore store,
            SharpInspectOptions options)
            : this(store, options, new HttpClientHandler())
        {
        }

        /// <summary>
        ///     지정된 의존성과 내부 핸들러로 새 SharpInspectHandler를 생성합니다.
        /// </summary>
        public SharpInspectHandler(
            ISharpInspectStore store,
            SharpInspectOptions options,
            HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        ///     [Obsolete] 하위 호환성을 위한 생성자. EventBus 파라미터는 무시됩니다.
        /// </summary>
        [Obsolete("EventBus는 더 이상 직접 전달할 필요가 없습니다. Store에서 자동으로 이벤트를 발행합니다.")]
        public SharpInspectHandler(
            ISharpInspectStore store,
            SharpInspectOptions options,
            Events.EventBus eventBus)
            : this(store, options, new HttpClientHandler())
        {
        }

        /// <summary>
        ///     [Obsolete] 하위 호환성을 위한 생성자. EventBus 파라미터는 무시됩니다.
        /// </summary>
        [Obsolete("EventBus는 더 이상 직접 전달할 필요가 없습니다. Store에서 자동으로 이벤트를 발행합니다.")]
        public SharpInspectHandler(
            ISharpInspectStore store,
            SharpInspectOptions options,
            Events.EventBus eventBus,
            HttpMessageHandler innerHandler)
            : this(store, options, innerHandler)
        {
        }

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (!_options.EnableNetworkCapture || ShouldIgnoreUrl(request.RequestUri))
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            var entry = new NetworkEntry();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // 요청 상세 정보 캡처
                await CaptureRequest(entry, request).ConfigureAwait(false);

                // 요청 전송
                var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

                stopwatch.Stop();

                // 응답 상세 정보 캡처
                await CaptureResponse(entry, response, stopwatch.Elapsed).ConfigureAwait(false);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                CaptureError(entry, ex, stopwatch.Elapsed);
                throw;
            }
            finally
            {
                // 엔트리 저장 (Store에서 자동으로 이벤트 발행)
                _store.AddNetworkEntry(entry);
            }
        }

        private bool ShouldIgnoreUrl(Uri uri)
        {
            if (uri == null || _options.IgnoreUrlPatterns == null)
                return false;

            var url = uri.ToString();
            foreach (var pattern in _options.IgnoreUrlPatterns)
                if (url.Contains(pattern))
                    return true;
            return false;
        }

        private bool ShouldMaskHeader(string headerName)
        {
            if (_options.MaskedHeaders == null)
                return false;

            foreach (var masked in _options.MaskedHeaders)
                if (string.Equals(headerName, masked, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private string GetInitiator()
        {
            try
            {
                var stackTrace = new StackTrace(true);
                var frames = stackTrace.GetFrames();
                if (frames == null)
                    return null;

                var sb = new StringBuilder();
                var skip = true;

                foreach (var frame in frames)
                {
                    var method = frame.GetMethod();
                    if (method == null)
                        continue;

                    var declaringType = method.DeclaringType;
                    if (declaringType == null)
                        continue;

                    var typeName = declaringType.FullName ?? "";

                    // SharpInspect 내부 프레임 건너뛰기
                    if (typeName.StartsWith("SharpInspect.") || typeName.StartsWith("System.Net.Http")) continue;

                    skip = false;

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

        private async Task CaptureRequest(NetworkEntry entry, HttpRequestMessage request)
        {
            entry.Method = request.Method.Method;
            entry.Url = request.RequestUri.ToString();
            entry.Host = request.RequestUri.Host;
            entry.Path = request.RequestUri.AbsolutePath;
            entry.QueryString = request.RequestUri.Query;

            // 헤더 캡처
            foreach (var header in request.Headers)
            {
                var value = string.Join(", ", header.Value);
                if (ShouldMaskHeader(header.Key)) value = "***masked***";
                entry.RequestHeaders[header.Key] = value;
            }

            // 콘텐츠 헤더 및 본문 캡처
            if (request.Content != null)
            {
                foreach (var header in request.Content.Headers)
                {
                    var value = string.Join(", ", header.Value);
                    entry.RequestHeaders[header.Key] = value;
                }

                entry.RequestContentType = request.Content.Headers.ContentType?.ToString();
                entry.RequestContentLength = request.Content.Headers.ContentLength ?? 0;

                if (_options.CaptureRequestBody && entry.RequestContentLength <= _options.MaxBodySizeBytes)
                    entry.RequestBody = await ReadContentAsString(request.Content).ConfigureAwait(false);
            }

            // 요청 출처 캡처 (스택 트레이스)
            entry.Initiator = GetInitiator();
        }

        private async Task CaptureResponse(NetworkEntry entry, HttpResponseMessage response, TimeSpan elapsed)
        {
            entry.StatusCode = (int)response.StatusCode;
            entry.StatusText = response.ReasonPhrase;
            entry.TotalMs = elapsed.TotalMilliseconds;

#if MODERN_DOTNET
            entry.Protocol = response.Version.ToString();
#else
            entry.Protocol = "HTTP/" + response.Version.ToString();
#endif

            // 헤더 캡처
            foreach (var header in response.Headers)
            {
                var value = string.Join(", ", header.Value);
                if (ShouldMaskHeader(header.Key)) value = "***masked***";
                entry.ResponseHeaders[header.Key] = value;
            }

            // 콘텐츠 헤더 및 본문 캡처
            if (response.Content != null)
            {
                foreach (var header in response.Content.Headers)
                {
                    var value = string.Join(", ", header.Value);
                    entry.ResponseHeaders[header.Key] = value;
                }

                entry.ResponseContentType = response.Content.Headers.ContentType?.ToString();
                entry.ResponseContentLength = response.Content.Headers.ContentLength ?? 0;

                if (_options.CaptureResponseBody && entry.ResponseContentLength <= _options.MaxBodySizeBytes)
                    entry.ResponseBody = await ReadContentAsString(response.Content).ConfigureAwait(false);
            }
        }

        private async Task<string> ReadContentAsString(HttpContent content)
        {
            try
            {
                // 다중 읽기를 위해 콘텐츠 버퍼링
                await content.LoadIntoBufferAsync().ConfigureAwait(false);
                return await content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch
            {
                return "[Unable to read content]";
            }
        }

        private void CaptureError(NetworkEntry entry, Exception ex, TimeSpan elapsed)
        {
            entry.IsError = true;
            entry.ErrorMessage = ex.Message;
            entry.TotalMs = elapsed.TotalMilliseconds;
        }
    }
}
