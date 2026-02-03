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
///     Manages WebSocket connections and broadcasts events to connected clients.
/// </summary>
public class WebSocketManager : IDisposable
{
    private readonly EventBus _eventBus;
    private readonly List<WebSocketClient> _clients;
    private readonly object _clientsLock = new();
    private readonly IDisposable _networkSubscription;
    private readonly IDisposable _consoleSubscription;
    private bool _disposed;

    /// <summary>
    ///     Gets the number of connected clients.
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
    ///     Creates a new WebSocketManager.
    /// </summary>
    public WebSocketManager(EventBus eventBus)
    {
        _eventBus = eventBus ?? EventBus.Instance;
        _clients = new List<WebSocketClient>();

        // Subscribe to events
        _networkSubscription = _eventBus.Subscribe<NetworkEntryEvent>(OnNetworkEntry);
        _consoleSubscription = _eventBus.Subscribe<ConsoleEntryEvent>(OnConsoleEntry);
    }

#if NET45_OR_GREATER || NETSTANDARD2_0 || NETCOREAPP
    /// <summary>
    ///     Accepts a WebSocket connection.
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

            // Send welcome message
            await SendMessage(client, new WebSocketMessage
            {
                Type = "connected",
                Data = new { message = "Welcome to SharpInspect" }
            }).ConfigureAwait(false);

            // Keep connection alive and handle incoming messages
            await ReceiveLoop(client).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Connection failed
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

                // We don't process incoming messages currently
                // but could add subscribe/unsubscribe functionality here
            }
        }
        catch
        {
            // Connection error
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
            // Fire and forget
            Task.Run(() => SendMessage(client, message));
    }
#else
        /// <summary>
        /// WebSocket is not supported in this .NET version.
        /// </summary>
        public void AcceptWebSocket(HttpListenerContext context)
        {
            throw new NotSupportedException("WebSocket is not supported in this .NET version");
        }

        private void Broadcast(WebSocketMessage message)
        {
            // Not supported
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

    private void RemoveClient(WebSocketClient client)
    {
        lock (_clientsLock)
        {
            _clients.Remove(client);
        }
    }

    /// <summary>
    ///     Closes all WebSocket connections.
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
                // Ignore errors during close
            }
#endif
    }

    /// <summary>
    ///     Disposes the manager.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _networkSubscription?.Dispose();
            _consoleSubscription?.Dispose();
            CloseAll();
        }
    }
}

/// <summary>
///     Represents a WebSocket message.
/// </summary>
public class WebSocketMessage
{
    /// <summary>
    ///     Gets or sets the message data.
    /// </summary>
    public object Data { get; set; }

    /// <summary>
    ///     Gets or sets the message type.
    /// </summary>
    public string Type { get; set; }
}

#if NET45_OR_GREATER || NETSTANDARD2_0 || NETCOREAPP
/// <summary>
///     Represents a connected WebSocket client.
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