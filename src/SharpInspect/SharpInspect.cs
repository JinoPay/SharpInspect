using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SharpInspect.Core.Configuration;
using SharpInspect.Core.Events;
using SharpInspect.Core.Interceptors;
using SharpInspect.Core.Logging;
using SharpInspect.Core.Storage;
#if !NET35
using System.Net.Http;
using SharpInspect.Server.WebServer;
#endif

namespace SharpInspect
{
    /// <summary>
    ///     Main entry point for SharpInspect DevTools.
    /// </summary>
    public static class SharpInspectDevTools
    {
        private static bool _initialized;
        private static ConsoleHook _consoleHook;
        private static InMemoryStore _store;

#if !NET35
        private static ISharpInspectServer _server;
#endif
        private static readonly object _lock = new();
        private static PerformanceInterceptor _performanceInterceptor;
        private static TraceHook _traceHook;

        /// <summary>
        ///     Gets whether SharpInspect has been initialized.
        /// </summary>
        public static bool IsInitialized
        {
            get
            {
                lock (_lock)
                {
                    return _initialized;
                }
            }
        }

        /// <summary>
        ///     Gets the event bus instance.
        /// </summary>
        public static EventBus EventBus { get; private set; }

        /// <summary>
        ///     Gets the storage instance.
        /// </summary>
        public static ISharpInspectStore Store => _store;

        /// <summary>
        ///     Gets the current options.
        /// </summary>
        public static SharpInspectOptions Options { get; private set; }

        /// <summary>
        ///     Gets the DevTools URL.
        /// </summary>
        public static string DevToolsUrl => Options?.GetDevToolsUrl();

        /// <summary>
        ///     Initializes SharpInspect with default options.
        /// </summary>
        public static void Initialize()
        {
            Initialize(new SharpInspectOptions());
        }

        /// <summary>
        ///     Initializes SharpInspect with a configuration action.
        /// </summary>
        public static void Initialize(Action<SharpInspectOptions> configure)
        {
            var options = new SharpInspectOptions();
            configure?.Invoke(options);
            Initialize(options);
        }

        /// <summary>
        ///     Initializes SharpInspect with the specified options.
        /// </summary>
        public static void Initialize(SharpInspectOptions options)
        {
            lock (_lock)
            {
                if (_initialized)
                    throw new InvalidOperationException(
                        "SharpInspect has already been initialized. Call Shutdown() first.");

                Options = options ?? new SharpInspectOptions();
                EventBus = new EventBus();
                _store = new InMemoryStore(Options.MaxNetworkEntries, Options.MaxConsoleEntries, Options.MaxPerformanceEntries);

                // Initialize HTTP interceptor for .NET Framework
                HttpWebRequestInterceptor.Initialize(_store, Options, EventBus);

                // Initialize console hook
                if (Options.EnableConsoleCapture)
                {
                    _consoleHook = new ConsoleHook(_store, Options, EventBus);
                    _traceHook = new TraceHook(_store, Options, EventBus);
                }

                // Initialize performance capture
                if (Options.EnablePerformanceCapture)
                {
                    _performanceInterceptor = new PerformanceInterceptor(_store, Options, EventBus);
                }

#if !NET35
                // Start web server
                _server = new HttpListenerServer(_store, Options, EventBus);
                _server.Start();

                if (Options.AutoOpenBrowser) OpenBrowser(Options.GetDevToolsUrl());
#endif

                _initialized = true;

                Console.WriteLine("SharpInspect DevTools initialized at " + Options.GetDevToolsUrl());
            }
        }

        /// <summary>
        ///     Opens the DevTools in the default browser.
        /// </summary>
        public static void OpenDevTools()
        {
            if (!_initialized || Options == null)
                return;

            OpenBrowser(Options.GetDevToolsUrl());
        }

        /// <summary>
        ///     Shuts down SharpInspect and releases resources.
        /// </summary>
        public static void Shutdown()
        {
            lock (_lock)
            {
                if (!_initialized)
                    return;

#if !NET35
                _server?.Dispose();
                _server = null;
#endif

                _consoleHook?.Dispose();
                _consoleHook = null;

                _traceHook?.Dispose();
                _traceHook = null;

                _performanceInterceptor?.Dispose();
                _performanceInterceptor = null;

                _store?.ClearAll();
                EventBus?.ClearAll();

                _initialized = false;
            }
        }

        private static void OpenBrowser(string url)
        {
            try
            {
#if NETFRAMEWORK
                Process.Start(url);
#else
                // Cross-platform browser opening
                if (RuntimeInformation.IsOSPlatform(
                        OSPlatform.Windows))
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                else if (RuntimeInformation.IsOSPlatform(
                             OSPlatform.Linux))
                    Process.Start("xdg-open", url);
                else if (RuntimeInformation.IsOSPlatform(
                             OSPlatform.OSX))
                    Process.Start("open", url);
#endif
            }
            catch
            {
                // Ignore browser open failures
            }
        }

#if !NET35
        /// <summary>
        ///     Creates a new HttpClient with SharpInspect interception enabled.
        /// </summary>
        public static HttpClient CreateHttpClient()
        {
            if (!_initialized)
                throw new InvalidOperationException("SharpInspect must be initialized before creating an HttpClient.");

            var handler = new SharpInspectHandler(_store, Options, EventBus);
            return new HttpClient(handler);
        }

        /// <summary>
        ///     Creates a SharpInspectHandler that can be used with HttpClient.
        /// </summary>
        public static SharpInspectHandler CreateHandler()
        {
            if (!_initialized)
                throw new InvalidOperationException("SharpInspect must be initialized before creating a handler.");

            return new SharpInspectHandler(_store, Options, EventBus);
        }

        /// <summary>
        ///     Creates a SharpInspectHandler with a custom inner handler.
        /// </summary>
        public static SharpInspectHandler CreateHandler(HttpMessageHandler innerHandler)
        {
            if (!_initialized)
                throw new InvalidOperationException("SharpInspect must be initialized before creating a handler.");

            return new SharpInspectHandler(_store, Options, EventBus, innerHandler);
        }
#endif
    }

    /// <summary>
    ///     Disposable wrapper for SharpInspect that shuts down on dispose.
    /// </summary>
    public class SharpInspectSession : IDisposable
    {
        private bool _disposed;

        /// <summary>
        ///     Creates and initializes a new SharpInspect session.
        /// </summary>
        public SharpInspectSession()
        {
            SharpInspectDevTools.Initialize();
        }

        /// <summary>
        ///     Creates and initializes a new SharpInspect session with options.
        /// </summary>
        public SharpInspectSession(SharpInspectOptions options)
        {
            SharpInspectDevTools.Initialize(options);
        }

        /// <summary>
        ///     Creates and initializes a new SharpInspect session with configuration.
        /// </summary>
        public SharpInspectSession(Action<SharpInspectOptions> configure)
        {
            SharpInspectDevTools.Initialize(configure);
        }

        /// <summary>
        ///     Disposes the session and shuts down SharpInspect.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                SharpInspectDevTools.Shutdown();
            }
        }
    }
}