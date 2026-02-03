namespace SharpInspect.Server.Api;

/// <summary>
///     Represents a paginated API response.
/// </summary>
/// <typeparam name="T">The type of items in the response.</typeparam>
public class PagedResponse<T>
{
    /// <summary>
    ///     Gets or sets the limit of items per page.
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    ///     Gets or sets the offset of the current page.
    /// </summary>
    public int Offset { get; set; }

    /// <summary>
    ///     Gets or sets the total number of items.
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    ///     Gets or sets the items in the current page.
    /// </summary>
    public T[] Items { get; set; }
}

/// <summary>
///     Represents the server status response.
/// </summary>
public class StatusResponse
{
    /// <summary>
    ///     Gets or sets the server uptime in seconds.
    /// </summary>
    public double UptimeSeconds { get; set; }

    /// <summary>
    ///     Gets or sets the number of console entries.
    /// </summary>
    public int ConsoleEntryCount { get; set; }

    /// <summary>
    ///     Gets or sets the number of network entries.
    /// </summary>
    public int NetworkEntryCount { get; set; }

    /// <summary>
    ///     Gets or sets the number of connected WebSocket clients.
    /// </summary>
    public int WebSocketClients { get; set; }

    /// <summary>
    ///     Gets or sets the server status.
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    ///     Gets or sets the SharpInspect version.
    /// </summary>
    public string Version { get; set; }
}

/// <summary>
///     Represents a simple message response.
/// </summary>
public class MessageResponse
{
    /// <summary>
    ///     Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    ///     Gets or sets the message.
    /// </summary>
    public string Message { get; set; }
}