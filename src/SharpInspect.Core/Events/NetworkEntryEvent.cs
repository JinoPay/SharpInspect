using System;
using SharpInspect.Core.Models;

namespace SharpInspect.Core.Events
{
    /// <summary>
    /// Event raised when a new network entry is captured.
    /// </summary>
    public class NetworkEntryEvent : SharpInspectEventBase
    {
        /// <summary>
        /// Gets the event type name.
        /// </summary>
        public override string EventType
        {
            get { return "network:entry"; }
        }

        /// <summary>
        /// Gets the captured network entry.
        /// </summary>
        public NetworkEntry Entry { get; private set; }

        /// <summary>
        /// Creates a new network entry event.
        /// </summary>
        public NetworkEntryEvent(NetworkEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException("entry");

            Entry = entry;
        }
    }
}
