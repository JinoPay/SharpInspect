using System;
using System.Net;
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

namespace SharpInspect.Server.WebServer;

/// <summary>
///     HttpListener를 사용하는 HTTP 서버 구현체.
///     .NET Framework 4.6.2+ 및 .NET Standard 2.0+ 호환.
/// </summary>
public class HttpListenerServer : ISharpInspectServer
{
    private readonly EmbeddedResourceProvider _staticFiles;
    private readonly EventBus _eventBus;
    private readonly ISharpInspectStore _store;
    private readonly object _lock = new();
    private readonly SharpInspectOptions _options;
    private readonly WebSocketManager _webSocketManager;
    private bool _isRunning;
    private DateTime _startTime;

    private HttpListener _listener;
    private Thread _listenerThread;

    /// <summary>
    ///     새 HttpListenerServer를 생성합니다.
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
    ///     서버를 해제합니다.
    /// </summary>
    public void Dispose()
    {
        Stop();
    }

    /// <summary>
    ///     서버가 실행 중인지 여부를 가져옵니다.
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
    ///     서버의 기본 URL을 가져옵니다.
    /// </summary>
    public string BaseUrl => string.Format("http://{0}:{1}/", _options.Host, _options.Port);

    /// <summary>
    ///     서버를 시작합니다.
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
                    string.Format(
                        "Failed to start HTTP listener on {0}. Port may be in use or require administrator privileges.",
                        BaseUrl),
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
    ///     서버를 중지합니다.
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
                // 종료 중 오류 무시
            }

            _webSocketManager.CloseAll();
        }
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
                PerformanceEntryCount = _store.PerformanceEntryCount,
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

        // GET /api/performance
        if (path == "/api/performance" && method == "GET")
        {
            var offset = GetQueryInt(request, "offset", 0);
            var limit = GetQueryInt(request, "limit", 100);
            var entries = _store.GetPerformanceEntries(offset, limit);
            var pagedResponse = new PagedResponse<PerformanceEntry>
            {
                Items = entries,
                Total = _store.PerformanceEntryCount,
                Offset = offset,
                Limit = limit
            };
            WriteJson(response, pagedResponse);
            return;
        }

        // POST /api/performance/clear
        if (path == "/api/performance/clear" && method == "POST")
        {
            _store.ClearPerformanceEntries();
            WriteJson(response, new MessageResponse { Success = true, Message = "Performance entries cleared" });
            return;
        }

        // GET /api/application
        if (path == "/api/application" && method == "GET")
        {
            var info = _store.GetApplicationInfo();
            if (info != null)
            {
                WriteJson(response, info);
            }
            else
            {
                response.StatusCode = 404;
                WriteJson(response, new MessageResponse { Success = false, Message = "Application info not available" });
            }

            return;
        }

        // POST /api/application/refresh
        if (path == "/api/application/refresh" && method == "POST")
        {
            WriteJson(response, new MessageResponse { Success = true, Message = "Application info refresh requested" });
            return;
        }

        // 찾을 수 없음
        response.StatusCode = 404;
        WriteJson(response, new MessageResponse { Success = false, Message = "API endpoint not found" });
    }

    private void HandleRequest(HttpListenerContext context)
    {
        try
        {
            var request = context.Request;
            var response = context.Response;

            // CORS 헤더 추가
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, DELETE, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");

            // 프리플라이트 요청 처리
            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 204;
                response.Close();
                return;
            }

            // 인증이 설정된 경우 확인
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

            // 요청 라우팅
            if (path.StartsWith("/api/"))
                HandleApiRequest(request, response, path);
            else if (path == "/ws")
                HandleWebSocketRequest(context);
            else
                HandleStaticRequest(request, response, path);
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
                // 오류 응답 작성 중 오류 무시
            }
        }
    }

    private void HandleStaticRequest(HttpListenerRequest request, HttpListenerResponse response, string path)
    {
        // index.html로 기본 설정
        if (path == "/" || path == "") path = "/index.html";

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

    private void HandleWebSocketRequest(HttpListenerContext context)
    {
        if (!context.Request.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            WriteJson(context.Response,
                new MessageResponse { Success = false, Message = "WebSocket upgrade required" });
            return;
        }

#if NET45_OR_GREATER || NETSTANDARD2_0 || NETCOREAPP
        _webSocketManager.AcceptWebSocket(context);
#else
            context.Response.StatusCode = 501;
            WriteJson(context.Response, new MessageResponse { Success = false, Message =
 "WebSocket not supported in this .NET version" });
#endif
    }

    private void ListenerLoop()
    {
        while (_isRunning)
            try
            {
                var context = _listener.GetContext();
                ThreadPool.QueueUserWorkItem(_ => HandleRequest(context));
            }
            catch (HttpListenerException)
            {
                // 리스너가 중지됨
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch
            {
                // 기타 오류에서 리스닝 계속
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
}
