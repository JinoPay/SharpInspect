#if !NET35
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharpInspect.Core.Configuration;
using SharpInspect.Core.Events;
using SharpInspect.Core.Models;
using SharpInspect.Core.Storage;

namespace SharpInspect.Core.Interceptors
{
    /// <summary>
    ///     HttpClient DelegatingHandler that intercepts and captures HTTP requests/responses.
    ///     Available for .NET 4.5+ and .NET Standard 2.0+.
    /// </summary>
    public class SharpInspectHandler : DelegatingHandler
    {
        private readonly EventBus _eventBus;
        private readonly ISharpInspectStore _store;
        private readonly SharpInspectOptions _options;

        /// <summary>
        ///     Creates a new SharpInspectHandler with the specified dependencies.
        /// </summary>
        public SharpInspectHandler(
            ISharpInspectStore store,
            SharpInspectOptions options,
            EventBus eventBus)
            : this(store, options, eventBus, new HttpClientHandler())
        {
        }

        /// <summary>
        ///     Creates a new SharpInspectHandler with the specified dependencies and inner handler.
        /// </summary>
        public SharpInspectHandler(
            ISharpInspectStore store,
            SharpInspectOptions options,
            EventBus eventBus,
            HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _eventBus = eventBus ?? EventBus.Instance;
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
                // Capture request details
                await CaptureRequest(entry, request).ConfigureAwait(false);

                // Send the request
                var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

                stopwatch.Stop();

                // Capture response details
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
                // Store and publish the entry
                _store.AddNetworkEntry(entry);
                _eventBus.PublishAsync(new NetworkEntryEvent(entry));
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

                    // Skip SharpInspect internal frames
                    if (typeName.StartsWith("SharpInspect.") || typeName.StartsWith("System.Net.Http")) continue;

                    skip = false;

                    var fileName = frame.GetFileName();
                    var lineNumber = frame.GetFileLineNumber();

                    if (!string.IsNullOrEmpty(fileName))
                        sb.AppendFormat("at {0}.{1}() in {2}:line {3}\n",
                            typeName, method.Name, fileName, lineNumber);
                    else
                        sb.AppendFormat("at {0}.{1}()\n", typeName, method.Name);

                    // Only capture first few frames
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

            // Capture headers
            foreach (var header in request.Headers)
            {
                var value = string.Join(", ", header.Value);
                if (ShouldMaskHeader(header.Key)) value = "***masked***";
                entry.RequestHeaders[header.Key] = value;
            }

            // Capture content headers and body
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

            // Capture initiator (stack trace)
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

            // Capture headers
            foreach (var header in response.Headers)
            {
                var value = string.Join(", ", header.Value);
                if (ShouldMaskHeader(header.Key)) value = "***masked***";
                entry.ResponseHeaders[header.Key] = value;
            }

            // Capture content headers and body
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
                // Buffer the content to allow multiple reads
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
#endif