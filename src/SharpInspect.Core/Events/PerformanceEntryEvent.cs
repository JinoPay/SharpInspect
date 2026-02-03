using System;
using SharpInspect.Core.Models;

namespace SharpInspect.Core.Events
{
    /// <summary>
    ///     새 성능 엔트리가 캡처될 때 발생하는 이벤트.
    /// </summary>
    public class PerformanceEntryEvent : SharpInspectEventBase
    {
        /// <summary>
        ///     새 성능 엔트리 이벤트를 생성합니다.
        /// </summary>
        public PerformanceEntryEvent(PerformanceEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException("entry");

            Entry = entry;
        }

        /// <summary>
        ///     캡처된 성능 엔트리를 가져옵니다.
        /// </summary>
        public PerformanceEntry Entry { get; private set; }

        /// <summary>
        ///     이벤트 타입 이름을 가져옵니다.
        /// </summary>
        public override string EventType => "performance:entry";
    }
}
