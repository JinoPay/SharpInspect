using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using SharpInspect.Core.Configuration;
using SharpInspect.Core.Events;
using SharpInspect.Core.Models;
using SharpInspect.Core.Storage;
using SharpInspect.Server.Api;
using SharpInspect.Server.Json;
using SharpInspect.Server.StaticFiles;
using SharpInspect.Server.WebSocket;

namespace SharpInspect.Server.WebServer
{
    /// <summary>
    /// HTTP server implementation using HttpListener.
    /// Compatible with .NET Framework 4.6.2+ and .NET Standard 2.0+.
    /// </summary>
    public class HttpListenerServer : ISharpInspectServer
    {
        private readonly ISharpInspectStore _store;
        private readonly SharpInspectOptions _options;
        private readonly EventBus _eventBus;
        private readonly EmbeddedResourceProvider _staticFiles;
        private readonly WebSocketManager _webSocketManager;

        private HttpListener _listener;
        private Thread _listenerThread;
        private bool _isRunning;
        private DateTime _startTime;
        private readonly object _lock = new object();

        /// <summary>
        /// Gets whether the server is running.
        /// </summary>
        public bool IsRunning
        {
            get
            {
                lock (_lock)
                {
                    return _isRunning;
                }
            }
        }

        /// <summary>
        /// Gets the base URL of the server.
        /// </summary>
        public string BaseUrl
        {
            get { return string.Format("http://{0}:{1}/", _options.Host, _options.Port); }
        }

        /// <summary>
        /// Creates a new HttpListenerServer.
        /// </summary>
        public HttpListenerServer(
            ISharpInspectStore store,
            SharpInspectOptions options,
            EventBus eventBus = null)
        {
            _store = store ?? throw new ArgumentNullException("store");
            _options = options ?? throw new ArgumentNullException("options");
            _eventBus = eventBus ?? EventBus.Instance;
            _staticFiles = new EmbeddedResourceProvider();
            _webSocketManager = new WebSocketManager(_eventBus);
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        public void Start()
        {
            lock (_lock)
            {
                if (_isRunning)
                    return;

                _listener = new HttpListener();
                _listener.Prefixes.Add(BaseUrl);

                try
                {
                    _listener.Start();
                }
                catch (HttpListenerException ex)
                {
                    throw new InvalidOperationException(
                        string.Format("Failed to start HTTP listener on {0}. Port may be in use or require administrator privileges.", BaseUrl),
                        ex);
                }

                _isRunning = true;
                _startTime = DateTime.UtcNow;

                _listenerThread = new Thread(ListenerLoop)
                {
                    IsBackground = true,
                    Name = "SharpInspect-HttpListener"
                };
                _listenerThread.Start();
            }
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        public void Stop()
        {
            lock (_lock)
            {
                if (!_isRunning)
                    return;

                _isRunning = false;

                try
                {
                    _listener.Stop();
                    _listener.Close();
                }
                catch
                {
                    // Ignore errors during shutdown
                }

                _webSocketManager.CloseAll();
            }
        }

        /// <summary>
        /// Disposes the server.
        /// </summary>
        public void Dispose()
        {
            Stop();
        }

        private void ListenerLoop()
        {
            while (_isRunning)
            {
                try
                {
                    var context = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => HandleRequest(context));
                }
                catch (HttpListenerException)
                {
                    // Listener was stopped
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch
                {
                    // Continue listening on other errors
                }
            }
        }

        private void HandleRequest(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;

                // Add CORS headers
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, DELETE, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");

                // Handle preflight
                if (request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = 204;
                    response.Close();
                    return;
                }

                // Check authentication if configured
                if (!string.IsNullOrEmpty(_options.AccessToken))
                {
                    var authHeader = request.Headers["Authorization"];
                    if (string.IsNullOrEmpty(authHeader) || authHeader != "Bearer " + _options.AccessToken)
                    {
                        response.StatusCode = 401;
                        WriteJson(response, new MessageResponse { Success = false, Message = "Unauthorized" });
                        return;
                    }
                }

                var path = request.Url.AbsolutePath.ToLowerInvariant();

                // Route request
                if (path.StartsWith("/api/"))
                {
                    HandleApiRequest(request, response, path);
                }
                else if (path == "/ws")
                {
                    HandleWebSocketRequest(context);
                }
                else
                {
                    HandleStaticRequest(request, response, path);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    context.Response.StatusCode = 500;
                    WriteJson(context.Response, new MessageResponse
                    {
                        Success = false,
                        Message = "Internal server error: " + ex.Message
                    });
                }
                catch
                {
                    // Ignore errors writing error response
                }
            }
        }

        private void HandleApiRequest(HttpListenerRequest request, HttpListenerResponse response, string path)
        {
            var method = request.HttpMethod;

            // GET /api/status
            if (path == "/api/status" && method == "GET")
            {
                var status = new StatusResponse
                {
                    Status = "running",
                    Version = "1.0.0",
                    UptimeSeconds = (DateTime.UtcNow - _startTime).TotalSeconds,
                    NetworkEntryCount = _store.NetworkEntryCount,
                    ConsoleEntryCount = _store.ConsoleEntryCount,
                    WebSocketClients = _webSocketManager.ClientCount
                };
                WriteJson(response, status);
                return;
            }

            // GET /api/network
            if (path == "/api/network" && method == "GET")
            {
                var offset = GetQueryInt(request, "offset", 0);
                var limit = GetQueryInt(request, "limit", 100);
                var entries = _store.GetNetworkEntries(offset, limit);
                var pagedResponse = new PagedResponse<NetworkEntry>
                {
                    Items = entries,
                    Total = _store.NetworkEntryCount,
                    Offset = offset,
                    Limit = limit
                };
                WriteJson(response, pagedResponse);
                return;
            }

            // GET /api/network/{id}
            if (path.StartsWith("/api/network/") && method == "GET")
            {
                var id = path.Substring("/api/network/".Length);
                var entry = _store.GetNetworkEntry(id);
                if (entry != null)
                {
                    WriteJson(response, entry);
                }
                else
                {
                    response.StatusCode = 404;
                    WriteJson(response, new MessageResponse { Success = false, Message = "Entry not found" });
                }
                return;
            }

            // POST /api/network/clear
            if (path == "/api/network/clear" && method == "POST")
            {
                _store.ClearNetworkEntries();
                WriteJson(response, new MessageResponse { Success = true, Message = "Network entries cleared" });
                return;
            }

            // GET /api/console
            if (path == "/api/console" && method == "GET")
            {
                var offset = GetQueryInt(request, "offset", 0);
                var limit = GetQueryInt(request, "limit", 100);
                var entries = _store.GetConsoleEntries(offset, limit);
                var pagedResponse = new PagedResponse<ConsoleEntry>
                {
                    Items = entries,
                    Total = _store.ConsoleEntryCount,
                    Offset = offset,
                    Limit = limit
                };
                WriteJson(response, pagedResponse);
                return;
            }

            // POST /api/console/clear
            if (path == "/api/console/clear" && method == "POST")
            {
                _store.ClearConsoleEntries();
                WriteJson(response, new MessageResponse { Success = true, Message = "Console entries cleared" });
                return;
            }

            // Not found
            response.StatusCode = 404;
            WriteJson(response, new MessageResponse { Success = false, Message = "API endpoint not found" });
        }

        private void HandleWebSocketRequest(HttpListenerContext context)
        {
            if (!context.Request.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                WriteJson(context.Response, new MessageResponse { Success = false, Message = "WebSocket upgrade required" });
                return;
            }

#if NET45_OR_GREATER || NETSTANDARD2_0 || NETCOREAPP
            _webSocketManager.AcceptWebSocket(context);
#else
            context.Response.StatusCode = 501;
            WriteJson(context.Response, new MessageResponse { Success = false, Message = "WebSocket not supported in this .NET version" });
#endif
        }

        private void HandleStaticRequest(HttpListenerRequest request, HttpListenerResponse response, string path)
        {
            // Default to index.html
            if (path == "/" || path == "")
            {
                path = "/index.html";
            }

            var content = _staticFiles.GetContent(path);
            if (content != null)
            {
                response.ContentType = _staticFiles.GetContentType(path);
                response.ContentLength64 = content.Length;
                response.OutputStream.Write(content, 0, content.Length);
            }
            else
            {
                response.StatusCode = 404;
                WriteText(response, "File not found: " + path);
            }
        }

        private void WriteJson(HttpListenerResponse response, object obj)
        {
            var json = SimpleJson.Serialize(obj);
            var bytes = Encoding.UTF8.GetBytes(json);
            response.ContentType = "application/json; charset=utf-8";
            response.ContentLength64 = bytes.Length;
            response.OutputStream.Write(bytes, 0, bytes.Length);
            response.Close();
        }

        private void WriteText(HttpListenerResponse response, string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            response.ContentType = "text/plain; charset=utf-8";
            response.ContentLength64 = bytes.Length;
            response.OutputStream.Write(bytes, 0, bytes.Length);
            response.Close();
        }

        private int GetQueryInt(HttpListenerRequest request, string name, int defaultValue)
        {
            var value = request.QueryString[name];
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            int result;
            if (int.TryParse(value, out result))
                return result;

            return defaultValue;
        }
    }
}
