namespace SharpInspect.Server.Api;

/// <summary>
///     페이지네이션된 API 응답을 나타냅니다.
/// </summary>
/// <typeparam name="T">응답 항목의 타입.</typeparam>
public class PagedResponse<T>
{
    /// <summary>
    ///     페이지당 항목 제한 수를 가져오거나 설정합니다.
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    ///     현재 페이지의 오프셋을 가져오거나 설정합니다.
    /// </summary>
    public int Offset { get; set; }

    /// <summary>
    ///     전체 항목 수를 가져오거나 설정합니다.
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    ///     현재 페이지의 항목을 가져오거나 설정합니다.
    /// </summary>
    public T[] Items { get; set; }
}

/// <summary>
///     서버 상태 응답을 나타냅니다.
/// </summary>
public class StatusResponse
{
    /// <summary>
    ///     서버 가동 시간(초)을 가져오거나 설정합니다.
    /// </summary>
    public double UptimeSeconds { get; set; }

    /// <summary>
    ///     콘솔 엔트리 수를 가져오거나 설정합니다.
    /// </summary>
    public int ConsoleEntryCount { get; set; }

    /// <summary>
    ///     네트워크 엔트리 수를 가져오거나 설정합니다.
    /// </summary>
    public int NetworkEntryCount { get; set; }

    /// <summary>
    ///     성능 엔트리 수를 가져오거나 설정합니다.
    /// </summary>
    public int PerformanceEntryCount { get; set; }

    /// <summary>
    ///     연결된 WebSocket 클라이언트 수를 가져오거나 설정합니다.
    /// </summary>
    public int WebSocketClients { get; set; }

    /// <summary>
    ///     서버 상태를 가져오거나 설정합니다.
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    ///     SharpInspect 버전을 가져오거나 설정합니다.
    /// </summary>
    public string Version { get; set; }
}

/// <summary>
///     간단한 메시지 응답을 나타냅니다.
/// </summary>
public class MessageResponse
{
    /// <summary>
    ///     작업 성공 여부를 가져오거나 설정합니다.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    ///     메시지를 가져오거나 설정합니다.
    /// </summary>
    public string Message { get; set; }
}
