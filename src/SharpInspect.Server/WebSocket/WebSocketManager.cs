using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using SharpInspect.Core.Events;
using SharpInspect.Server.Json;
#if NET45_OR_GREATER || NETSTANDARD2_0 || NETCOREAPP
using System.Net.WebSockets;
using System.Threading.Tasks;
#endif

namespace SharpInspect.Server.WebSocket;

/// <summary>
///     WebSocket 연결을 관리하고 연결된 클라이언트에 이벤트를 브로드캐스트합니다.
/// </summary>
public class WebSocketManager : IDisposable
{
    private readonly EventBus _eventBus;
    private readonly List<WebSocketClient> _clients;
    private readonly object _clientsLock = new();
    private readonly IDisposable _networkSubscription;
    private readonly IDisposable _consoleSubscription;
    private readonly IDisposable _performanceSubscription;
    private bool _disposed;

    /// <summary>
    ///     연결된 클라이언트 수를 가져옵니다.
    /// </summary>
    public int ClientCount
    {
        get
        {
            lock (_clientsLock)
            {
                return _clients.Count;
            }
        }
    }

    /// <summary>
    ///     새 WebSocketManager를 생성합니다.
    /// </summary>
    public WebSocketManager(EventBus eventBus)
    {
        _eventBus = eventBus ?? EventBus.Instance;
        _clients = new List<WebSocketClient>();

        // 이벤트 구독
        _networkSubscription = _eventBus.Subscribe<NetworkEntryEvent>(OnNetworkEntry);
        _consoleSubscription = _eventBus.Subscribe<ConsoleEntryEvent>(OnConsoleEntry);
        _performanceSubscription = _eventBus.Subscribe<PerformanceEntryEvent>(OnPerformanceEntry);
    }

#if NET45_OR_GREATER || NETSTANDARD2_0 || NETCOREAPP
    /// <summary>
    ///     WebSocket 연결을 수락합니다.
    /// </summary>
    public async void AcceptWebSocket(HttpListenerContext context)
    {
        System.Net.WebSockets.WebSocket webSocket = null;

        try
        {
            var wsContext = await context.AcceptWebSocketAsync(null).ConfigureAwait(false);
            webSocket = wsContext.WebSocket;

            var client = new WebSocketClient(webSocket);

            lock (_clientsLock)
            {
                _clients.Add(client);
            }

            // 환영 메시지 전송
            await SendMessage(client, new WebSocketMessage
            {
                Type = "connected",
                Data = new { message = "Welcome to SharpInspect" }
            }).ConfigureAwait(false);

            // 연결 유지 및 수신 메시지 처리
            await ReceiveLoop(client).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // 연결 실패
        }
        finally
        {
            if (webSocket != null)
                try
                {
                    webSocket.Dispose();
                }
                catch
                {
                }
        }
    }

    private async Task ReceiveLoop(WebSocketClient client)
    {
        var buffer = new byte[4096];

        try
        {
            while (client.WebSocket.State == WebSocketState.Open)
            {
                var result = await client.WebSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None).ConfigureAwait(false);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await client.WebSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Closing",
                        CancellationToken.None).ConfigureAwait(false);
                    break;
                }

                // 현재 수신 메시지를 처리하지 않음
                // 향후 구독/구독취소 기능 추가 가능
            }
        }
        catch
        {
            // 연결 오류
        }
        finally
        {
            RemoveClient(client);
        }
    }

    private async Task SendMessage(WebSocketClient client, WebSocketMessage message)
    {
        if (client.WebSocket.State != WebSocketState.Open)
            return;

        try
        {
            var json = SimpleJson.Serialize(message);
            var bytes = Encoding.UTF8.GetBytes(json);
            var segment = new ArraySegment<byte>(bytes);

            await client.SendAsync(segment).ConfigureAwait(false);
        }
        catch
        {
            RemoveClient(client);
        }
    }

    private void Broadcast(WebSocketMessage message)
    {
        WebSocketClient[] clients;
        lock (_clientsLock)
        {
            clients = _clients.ToArray();
        }

        foreach (var client in clients)
            // 실행 후 잊기
            Task.Run(() => SendMessage(client, message));
    }
#else
        /// <summary>
        /// 이 .NET 버전에서는 WebSocket이 지원되지 않습니다.
        /// </summary>
        public void AcceptWebSocket(HttpListenerContext context)
        {
            throw new NotSupportedException("WebSocket is not supported in this .NET version");
        }

        private void Broadcast(WebSocketMessage message)
        {
            // 지원되지 않음
        }
#endif

    private void OnNetworkEntry(NetworkEntryEvent evt)
    {
        Broadcast(new WebSocketMessage
        {
            Type = "network:entry",
            Data = evt.Entry
        });
    }

    private void OnConsoleEntry(ConsoleEntryEvent evt)
    {
        Broadcast(new WebSocketMessage
        {
            Type = "console:entry",
            Data = evt.Entry
        });
    }

    private void OnPerformanceEntry(PerformanceEntryEvent evt)
    {
        Broadcast(new WebSocketMessage
        {
            Type = "performance:entry",
            Data = evt.Entry
        });
    }

    private void RemoveClient(WebSocketClient client)
    {
        lock (_clientsLock)
        {
            _clients.Remove(client);
        }
    }

    /// <summary>
    ///     모든 WebSocket 연결을 닫습니다.
    /// </summary>
    public void CloseAll()
    {
#if NET45_OR_GREATER || NETSTANDARD2_0 || NETCOREAPP
        WebSocketClient[] clients;
        lock (_clientsLock)
        {
            clients = _clients.ToArray();
            _clients.Clear();
        }

        foreach (var client in clients)
            try
            {
                if (client.WebSocket.State == WebSocketState.Open)
                    client.WebSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Server shutting down",
                        CancellationToken.None).Wait(1000);
                client.WebSocket.Dispose();
            }
            catch
            {
                // 닫기 중 오류 무시
            }
#endif
    }

    /// <summary>
    ///     매니저를 해제합니다.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _networkSubscription?.Dispose();
            _consoleSubscription?.Dispose();
            _performanceSubscription?.Dispose();
            CloseAll();
        }
    }
}

/// <summary>
///     WebSocket 메시지를 나타냅니다.
/// </summary>
public class WebSocketMessage
{
    /// <summary>
    ///     메시지 데이터를 가져오거나 설정합니다.
    /// </summary>
    public object Data { get; set; }

    /// <summary>
    ///     메시지 타입을 가져오거나 설정합니다.
    /// </summary>
    public string Type { get; set; }
}

#if NET45_OR_GREATER || NETSTANDARD2_0 || NETCOREAPP
/// <summary>
///     연결된 WebSocket 클라이언트를 나타냅니다.
/// </summary>
internal class WebSocketClient
{
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public WebSocketClient(System.Net.WebSockets.WebSocket webSocket)
    {
        WebSocket = webSocket;
    }

    public System.Net.WebSockets.WebSocket WebSocket { get; }

    public async Task SendAsync(ArraySegment<byte> data)
    {
        await _sendLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await WebSocket.SendAsync(
                data,
                WebSocketMessageType.Text,
                true,
                CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            _sendLock.Release();
        }
    }
}
#endif
