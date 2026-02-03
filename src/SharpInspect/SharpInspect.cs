using System;
using SharpInspect.Core.Configuration;
using SharpInspect.Core.Events;
using SharpInspect.Core.Interceptors;
using SharpInspect.Core.Logging;
using SharpInspect.Core.Storage;

#if !NET35
using SharpInspect.Server.WebServer;
#endif

namespace SharpInspect
{
    /// <summary>
    /// Main entry point for SharpInspect DevTools.
    /// </summary>
    public static class SharpInspectDevTools
    {
        private static readonly object _lock = new object();
        private static bool _initialized;
        private static SharpInspectOptions _options;
        private static InMemoryStore _store;
        private static EventBus _eventBus;
        private static ConsoleHook _consoleHook;
        private static TraceHook _traceHook;

#if !NET35
        private static ISharpInspectServer _server;
#endif

        /// <summary>
        /// Gets whether SharpInspect has been initialized.
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
        /// Gets the current options.
        /// </summary>
        public static SharpInspectOptions Options
        {
            get { return _options; }
        }

        /// <summary>
        /// Gets the storage instance.
        /// </summary>
        public static ISharpInspectStore Store
        {
            get { return _store; }
        }

        /// <summary>
        /// Gets the event bus instance.
        /// </summary>
        public static EventBus EventBus
        {
            get { return _eventBus; }
        }

        /// <summary>
        /// Gets the DevTools URL.
        /// </summary>
        public static string DevToolsUrl
        {
            get { return _options?.GetDevToolsUrl(); }
        }

        /// <summary>
        /// Initializes SharpInspect with default options.
        /// </summary>
        public static void Initialize()
        {
            Initialize(new SharpInspectOptions());
        }

        /// <summary>
        /// Initializes SharpInspect with a configuration action.
        /// </summary>
        public static void Initialize(Action<SharpInspectOptions> configure)
        {
            var options = new SharpInspectOptions();
            configure?.Invoke(options);
            Initialize(options);
        }

        /// <summary>
        /// Initializes SharpInspect with the specified options.
        /// </summary>
        public static void Initialize(SharpInspectOptions options)
        {
            lock (_lock)
            {
                if (_initialized)
                {
                    throw new InvalidOperationException("SharpInspect has already been initialized. Call Shutdown() first.");
                }

                _options = options ?? new SharpInspectOptions();
                _eventBus = new EventBus();
                _store = new InMemoryStore(_options.MaxNetworkEntries, _options.MaxConsoleEntries);

                // Initialize HTTP interceptor for .NET Framework
                HttpWebRequestInterceptor.Initialize(_store, _options, _eventBus);

                // Initialize console hook
                if (_options.EnableConsoleCapture)
                {
                    _consoleHook = new ConsoleHook(_store, _options, _eventBus);
                    _traceHook = new TraceHook(_store, _options, _eventBus);
                }

#if !NET35
                // Start web server
                _server = new HttpListenerServer(_store, _options, _eventBus);
                _server.Start();

                if (_options.AutoOpenBrowser)
                {
                    OpenBrowser(_options.GetDevToolsUrl());
                }
#endif

                _initialized = true;

                Console.WriteLine("SharpInspect DevTools initialized at " + _options.GetDevToolsUrl());
            }
        }

        /// <summary>
        /// Shuts down SharpInspect and releases resources.
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

                _store?.ClearAll();
                _eventBus?.ClearAll();

                _initialized = false;
            }
        }

        /// <summary>
        /// Opens the DevTools in the default browser.
        /// </summary>
        public static void OpenDevTools()
        {
            if (!_initialized || _options == null)
                return;

            OpenBrowser(_options.GetDevToolsUrl());
        }

#if !NET35
        /// <summary>
        /// Creates a new HttpClient with SharpInspect interception enabled.
        /// </summary>
        public static System.Net.Http.HttpClient CreateHttpClient()
        {
            if (!_initialized)
                throw new InvalidOperationException("SharpInspect must be initialized before creating an HttpClient.");

            var handler = new SharpInspectHandler(_store, _options, _eventBus);
            return new System.Net.Http.HttpClient(handler);
        }

        /// <summary>
        /// Creates a SharpInspectHandler that can be used with HttpClient.
        /// </summary>
        public static SharpInspectHandler CreateHandler()
        {
            if (!_initialized)
                throw new InvalidOperationException("SharpInspect must be initialized before creating a handler.");

            return new SharpInspectHandler(_store, _options, _eventBus);
        }

        /// <summary>
        /// Creates a SharpInspectHandler with a custom inner handler.
        /// </summary>
        public static SharpInspectHandler CreateHandler(System.Net.Http.HttpMessageHandler innerHandler)
        {
            if (!_initialized)
                throw new InvalidOperationException("SharpInspect must be initialized before creating a handler.");

            return new SharpInspectHandler(_store, _options, _eventBus, innerHandler);
        }
#endif

        private static void OpenBrowser(string url)
        {
            try
            {
#if NETFRAMEWORK
                System.Diagnostics.Process.Start(url);
#else
                // Cross-platform browser opening
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                    System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                    System.Runtime.InteropServices.OSPlatform.Linux))
                {
                    System.Diagnostics.Process.Start("xdg-open", url);
                }
                else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                    System.Runtime.InteropServices.OSPlatform.OSX))
                {
                    System.Diagnostics.Process.Start("open", url);
                }
#endif
            }
            catch
            {
                // Ignore browser open failures
            }
        }
    }

    /// <summary>
    /// Disposable wrapper for SharpInspect that shuts down on dispose.
    /// </summary>
    public class SharpInspectSession : IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// Creates and initializes a new SharpInspect session.
        /// </summary>
        public SharpInspectSession()
        {
            SharpInspectDevTools.Initialize();
        }

        /// <summary>
        /// Creates and initializes a new SharpInspect session with options.
        /// </summary>
        public SharpInspectSession(SharpInspectOptions options)
        {
            SharpInspectDevTools.Initialize(options);
        }

        /// <summary>
        /// Creates and initializes a new SharpInspect session with configuration.
        /// </summary>
        public SharpInspectSession(Action<SharpInspectOptions> configure)
        {
            SharpInspectDevTools.Initialize(configure);
        }

        /// <summary>
        /// Disposes the session and shuts down SharpInspect.
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
