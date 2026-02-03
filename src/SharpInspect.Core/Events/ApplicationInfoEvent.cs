using System;
using SharpInspect.Core.Models;

namespace SharpInspect.Core.Events
{
    /// <summary>
    ///     애플리케이션 정보가 갱신될 때 발생하는 이벤트.
    /// </summary>
    public class ApplicationInfoEvent : SharpInspectEventBase
    {
        /// <summary>
        ///     새 애플리케이션 정보 이벤트를 생성합니다.
        /// </summary>
        public ApplicationInfoEvent(ApplicationInfo info)
        {
            if (info == null)
                throw new ArgumentNullException("info");

            Info = info;
        }

        /// <summary>
        ///     갱신된 애플리케이션 정보를 가져옵니다.
        /// </summary>
        public ApplicationInfo Info { get; private set; }

        /// <summary>
        ///     이벤트 타입 이름을 가져옵니다.
        /// </summary>
        public override string EventType => "application:info";
    }
}
