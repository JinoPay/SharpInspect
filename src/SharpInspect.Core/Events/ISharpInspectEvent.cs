using System;

namespace SharpInspect.Core.Events;

/// <summary>
///     모든 SharpInspect 이벤트의 기본 인터페이스.
/// </summary>
public interface ISharpInspectEvent
{
    /// <summary>
    ///     이벤트가 발생한 타임스탬프를 가져옵니다.
    /// </summary>
    DateTime Timestamp { get; }

    /// <summary>
    ///     이벤트 타입 이름을 가져옵니다.
    /// </summary>
    string EventType { get; }
}

/// <summary>
///     SharpInspect 이벤트의 기본 클래스.
/// </summary>
public abstract class SharpInspectEventBase : ISharpInspectEvent
{
    /// <inheritdoc />
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    /// <inheritdoc />
    public abstract string EventType { get; }
}