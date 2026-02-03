using System;
using SharpInspect.Core.Models;

namespace SharpInspect.Core.Events
{
    /// <summary>
    ///     Event raised when a new performance entry is captured.
    /// </summary>
    public class PerformanceEntryEvent : SharpInspectEventBase
    {
        /// <summary>
        ///     Creates a new performance entry event.
        /// </summary>
        public PerformanceEntryEvent(PerformanceEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException("entry");

            Entry = entry;
        }

        /// <summary>
        ///     Gets the captured performance entry.
        /// </summary>
        public PerformanceEntry Entry { get; private set; }

        /// <summary>
        ///     Gets the event type name.
        /// </summary>
        public override string EventType => "performance:entry";
    }
}
