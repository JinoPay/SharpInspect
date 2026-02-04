using System;
using SharpInspect.Core.Models;

namespace SharpInspect.Core.Events;

/// <summary>
///     새 콘솔 엔트리가 캡처될 때 발생하는 이벤트.
/// </summary>
public class ConsoleEntryEvent : SharpInspectEventBase
{
    /// <summary>
    ///     새 콘솔 엔트리 이벤트를 생성합니다.
    /// </summary>
    public ConsoleEntryEvent(ConsoleEntry entry)
    {
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        Entry = entry;
    }

    /// <summary>
    ///     캡처된 콘솔 엔트리를 가져옵니다.
    /// </summary>
    public ConsoleEntry Entry { get; private set; }

    /// <summary>
    ///     이벤트 타입 이름을 가져옵니다.
    /// </summary>
    public override string EventType => "console:entry";
}