using System;

namespace SharpInspect.Server.WebServer;

/// <summary>
///     임베디드 SharpInspect 웹 서버를 위한 인터페이스.
/// </summary>
public interface ISharpInspectServer : IDisposable
{
    /// <summary>
    ///     서버가 현재 실행 중인지 여부를 가져옵니다.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    ///     서버의 기본 URL을 가져옵니다.
    /// </summary>
    string BaseUrl { get; }

    /// <summary>
    ///     서버를 시작합니다.
    /// </summary>
    void Start();

    /// <summary>
    ///     서버를 중지합니다.
    /// </summary>
    void Stop();
}
