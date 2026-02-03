using System;
using SharpInspect.Core.Models;

namespace SharpInspect.Core.Events
{
    /// <summary>
    /// Event raised when a new console entry is captured.
    /// </summary>
    public class ConsoleEntryEvent : SharpInspectEventBase
    {
        /// <summary>
        /// Gets the event type name.
        /// </summary>
        public override string EventType
        {
            get { return "console:entry"; }
        }

        /// <summary>
        /// Gets the captured console entry.
        /// </summary>
        public ConsoleEntry Entry { get; private set; }

        /// <summary>
        /// Creates a new console entry event.
        /// </summary>
        public ConsoleEntryEvent(ConsoleEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException("entry");

            Entry = entry;
        }
    }
}
