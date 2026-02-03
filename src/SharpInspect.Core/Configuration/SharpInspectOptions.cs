using System.Collections.Generic;
using SharpInspect.Core.Models;

namespace SharpInspect.Core.Configuration
{
    /// <summary>
    ///     Configuration options for SharpInspect.
    /// </summary>
    public class SharpInspectOptions
    {
        /// <summary>
        ///     Creates a new SharpInspectOptions with default values.
        /// </summary>
        public SharpInspectOptions()
        {
            Port = 9229;
            Host = "localhost";
            AutoOpenBrowser = false;
            EnableHotkey = true;
            Hotkey = "F12";
            EnableNetworkCapture = true;
            EnableConsoleCapture = true;
            EnablePerformanceCapture = true;
            MaxNetworkEntries = 1000;
            MaxBodySizeBytes = 1048576; // 1MB
            IgnoreUrlPatterns = new List<string>();
            CaptureRequestBody = true;
            CaptureResponseBody = true;
            MaxConsoleEntries = 5000;
            MaxPerformanceEntries = 2000;
            PerformanceCaptureIntervalMs = 1000;
            MinLogLevel = SharpInspectLogLevel.Trace;
            EnableInDevelopmentOnly = true;
            AccessToken = null;
            MaskedHeaders = new List<string> { "Authorization", "Cookie", "Set-Cookie" };
        }

        /// <summary>
        ///     Gets or sets whether to automatically open the browser when SharpInspect starts.
        ///     Default: false
        /// </summary>
        public bool AutoOpenBrowser { get; set; }

        /// <summary>
        ///     Gets or sets whether to capture request bodies.
        ///     Default: true
        /// </summary>
        public bool CaptureRequestBody { get; set; }

        /// <summary>
        ///     Gets or sets whether to capture response bodies.
        ///     Default: true
        /// </summary>
        public bool CaptureResponseBody { get; set; }

        /// <summary>
        ///     Gets or sets whether to capture console output.
        ///     Default: true
        /// </summary>
        public bool EnableConsoleCapture { get; set; }

        /// <summary>
        ///     Gets or sets whether to enable global hotkey functionality.
        ///     Default: true
        /// </summary>
        public bool EnableHotkey { get; set; }

        /// <summary>
        ///     Gets or sets whether to only enable in development environment.
        ///     Default: true
        /// </summary>
        public bool EnableInDevelopmentOnly { get; set; }

        /// <summary>
        ///     Gets or sets whether to capture network requests.
        ///     Default: true
        /// </summary>
        public bool EnableNetworkCapture { get; set; }

        /// <summary>
        ///     Gets or sets whether to capture performance metrics.
        ///     Default: true
        /// </summary>
        public bool EnablePerformanceCapture { get; set; }

        /// <summary>
        ///     Gets or sets the maximum body size to capture in bytes.
        ///     Default: 1MB (1048576)
        /// </summary>
        public int MaxBodySizeBytes { get; set; }

        /// <summary>
        ///     Gets or sets the maximum number of console entries to store.
        ///     Default: 5000
        /// </summary>
        public int MaxConsoleEntries { get; set; }

        /// <summary>
        ///     Gets or sets the maximum number of network entries to store.
        ///     Default: 1000
        /// </summary>
        public int MaxNetworkEntries { get; set; }

        /// <summary>
        ///     Gets or sets the maximum number of performance entries to store.
        ///     Default: 2000
        /// </summary>
        public int MaxPerformanceEntries { get; set; }

        /// <summary>
        ///     Gets or sets the performance metrics capture interval in milliseconds.
        ///     Default: 1000 (1 second)
        /// </summary>
        public int PerformanceCaptureIntervalMs { get; set; }

        /// <summary>
        ///     Gets or sets the port for the embedded web server.
        ///     Default: 9229
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        ///     Gets or sets URL patterns to ignore for network capture.
        /// </summary>
        public List<string> IgnoreUrlPatterns { get; set; }

        /// <summary>
        ///     Gets or sets header names to mask in the capture.
        ///     Default: Authorization, Cookie, Set-Cookie
        /// </summary>
        public List<string> MaskedHeaders { get; set; }

        /// <summary>
        ///     Gets or sets the minimum log level to capture.
        ///     Default: Trace
        /// </summary>
        public SharpInspectLogLevel MinLogLevel { get; set; }

        /// <summary>
        ///     Gets or sets an optional access token for basic authentication.
        ///     Default: null (no authentication)
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        ///     Gets or sets the host for the embedded web server.
        ///     Default: localhost
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        ///     Gets or sets the hotkey to open the DevTools.
        ///     Default: F12
        /// </summary>
        public string Hotkey { get; set; }

        /// <summary>
        ///     Creates a copy of the current options.
        /// </summary>
        public SharpInspectOptions Clone()
        {
            var clone = new SharpInspectOptions
            {
                Port = Port,
                Host = Host,
                AutoOpenBrowser = AutoOpenBrowser,
                EnableHotkey = EnableHotkey,
                Hotkey = Hotkey,
                EnableNetworkCapture = EnableNetworkCapture,
                EnableConsoleCapture = EnableConsoleCapture,
                EnablePerformanceCapture = EnablePerformanceCapture,
                MaxNetworkEntries = MaxNetworkEntries,
                MaxBodySizeBytes = MaxBodySizeBytes,
                CaptureRequestBody = CaptureRequestBody,
                CaptureResponseBody = CaptureResponseBody,
                MaxConsoleEntries = MaxConsoleEntries,
                MaxPerformanceEntries = MaxPerformanceEntries,
                PerformanceCaptureIntervalMs = PerformanceCaptureIntervalMs,
                MinLogLevel = MinLogLevel,
                EnableInDevelopmentOnly = EnableInDevelopmentOnly,
                AccessToken = AccessToken
            };

            clone.IgnoreUrlPatterns = new List<string>(IgnoreUrlPatterns);
            clone.MaskedHeaders = new List<string>(MaskedHeaders);

            return clone;
        }

        /// <summary>
        ///     Gets the full URL for the DevTools UI.
        /// </summary>
        public string GetDevToolsUrl()
        {
            return string.Format("http://{0}:{1}", Host, Port);
        }
    }
}