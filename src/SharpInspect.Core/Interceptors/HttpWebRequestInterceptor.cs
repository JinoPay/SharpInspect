using System;
using System.Collections.Generic;
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
    /// Utility class for intercepting HttpWebRequest operations.
    /// Compatible with .NET Framework 3.5+.
    /// </summary>
    public static class HttpWebRequestInterceptor
    {
        private static ISharpInspectStore _store;
        private static SharpInspectOptions _options;
        private static EventBus _eventBus;
        private static bool _initialized;
        private static readonly object _initLock = new object();

        /// <summary>
        /// Initializes the interceptor with the specified dependencies.
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

        /// <summary>
        /// Creates a wrapper around HttpWebRequest that captures the request/response.
        /// </summary>
        public static HttpWebResponse GetResponseWithCapture(HttpWebRequest request)
        {
            if (!_initialized || !_options.EnableNetworkCapture)
            {
                return (HttpWebResponse)request.GetResponse();
            }

            if (ShouldIgnoreUrl(request.RequestUri))
            {
                return (HttpWebResponse)request.GetResponse();
            }

            var entry = new NetworkEntry();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Capture request
                CaptureRequest(entry, request);

                // Get response
                var response = (HttpWebResponse)request.GetResponse();

                stopwatch.Stop();

                // Capture response
                CaptureResponse(entry, response, stopwatch.Elapsed);

                // Store and publish
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
        /// Wraps a synchronous request/response capture for any HttpWebRequest.
        /// Usage: Use this before manually calling GetResponse().
        /// </summary>
        public static NetworkEntry CaptureRequestStart(HttpWebRequest request)
        {
            if (!_initialized || !_options.EnableNetworkCapture)
            {
                return null;
            }

            if (ShouldIgnoreUrl(request.RequestUri))
            {
                return null;
            }

            var entry = new NetworkEntry();
            CaptureRequest(entry, request);
            return entry;
        }

        /// <summary>
        /// Completes the capture after receiving a response.
        /// </summary>
        public static void CaptureRequestEnd(NetworkEntry entry, HttpWebResponse response, TimeSpan elapsed)
        {
            if (entry == null || !_initialized)
                return;

            CaptureResponse(entry, response, elapsed);
            StoreAndPublish(entry);
        }

        /// <summary>
        /// Completes the capture after an error.
        /// </summary>
        public static void CaptureRequestError(NetworkEntry entry, Exception ex, TimeSpan elapsed)
        {
            if (entry == null || !_initialized)
                return;

            CaptureError(entry, ex, elapsed);
            StoreAndPublish(entry);
        }

        private static void CaptureRequest(NetworkEntry entry, HttpWebRequest request)
        {
            entry.Method = request.Method;
            entry.Url = request.RequestUri.ToString();
            entry.Host = request.RequestUri.Host;
            entry.Path = request.RequestUri.AbsolutePath;
            entry.QueryString = request.RequestUri.Query;

            // Capture headers
            if (request.Headers != null)
            {
                foreach (string key in request.Headers.AllKeys)
                {
                    var value = request.Headers[key];
                    if (ShouldMaskHeader(key))
                    {
                        value = "***masked***";
                    }
                    entry.RequestHeaders[key] = value;
                }
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

            // Capture headers
            if (response.Headers != null)
            {
                foreach (string key in response.Headers.AllKeys)
                {
                    var value = response.Headers[key];
                    if (ShouldMaskHeader(key))
                    {
                        value = "***masked***";
                    }
                    entry.ResponseHeaders[key] = value;
                }
            }

            entry.ResponseContentType = response.ContentType;
            entry.ResponseContentLength = response.ContentLength;

            // Capture response body if enabled and within size limit
            if (_options.CaptureResponseBody &&
                response.ContentLength <= _options.MaxBodySizeBytes &&
                response.ContentLength != -1)
            {
                entry.ResponseBody = ReadResponseBody(response);
            }
        }

        private static void CaptureError(NetworkEntry entry, Exception ex, TimeSpan elapsed)
        {
            entry.IsError = true;
            entry.ErrorMessage = ex.Message;
            entry.TotalMs = elapsed.TotalMilliseconds;
        }

        private static string ReadResponseBody(HttpWebResponse response)
        {
            try
            {
                using (var stream = response.GetResponseStream())
                {
                    if (stream == null)
                        return null;

                    // We need to buffer the stream to allow the caller to read it again
                    // This is a limitation - for large responses, consider not capturing
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

        private static void StoreAndPublish(NetworkEntry entry)
        {
            if (_store != null)
            {
                _store.AddNetworkEntry(entry);
            }

            if (_eventBus != null)
            {
#if NET35
                // Synchronous publish for .NET 3.5
                _eventBus.Publish(new NetworkEntryEvent(entry));
#else
                _eventBus.PublishAsync(new NetworkEntryEvent(entry));
#endif
            }
        }

        private static bool ShouldIgnoreUrl(Uri uri)
        {
            if (uri == null || _options == null || _options.IgnoreUrlPatterns == null)
                return false;

            var url = uri.ToString();
            foreach (var pattern in _options.IgnoreUrlPatterns)
            {
                if (url.Contains(pattern))
                    return true;
            }
            return false;
        }

        private static bool ShouldMaskHeader(string headerName)
        {
            if (_options == null || _options.MaskedHeaders == null)
                return false;

            foreach (var masked in _options.MaskedHeaders)
            {
                if (string.Equals(headerName, masked, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
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

                    // Skip SharpInspect and System.Net internal frames
                    if (typeName.StartsWith("SharpInspect.") ||
                        typeName.StartsWith("System.Net"))
                    {
                        continue;
                    }

                    var fileName = frame.GetFileName();
                    var lineNumber = frame.GetFileLineNumber();

                    if (!string.IsNullOrEmpty(fileName))
                    {
                        sb.AppendFormat("at {0}.{1}() in {2}:line {3}\n",
                            typeName, method.Name, fileName, lineNumber);
                    }
                    else
                    {
                        sb.AppendFormat("at {0}.{1}()\n", typeName, method.Name);
                    }

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
    }
}
