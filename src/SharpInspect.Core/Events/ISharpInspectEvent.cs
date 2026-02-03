using System;

namespace SharpInspect.Core.Events
{
    /// <summary>
    ///     Base interface for all SharpInspect events.
    /// </summary>
    public interface ISharpInspectEvent
    {
        /// <summary>
        ///     Gets the timestamp when the event occurred.
        /// </summary>
        DateTime Timestamp { get; }

        /// <summary>
        ///     Gets the event type name.
        /// </summary>
        string EventType { get; }
    }

    /// <summary>
    ///     Base class for SharpInspect events.
    /// </summary>
    public abstract class SharpInspectEventBase : ISharpInspectEvent
    {
        protected SharpInspectEventBase()
        {
            Timestamp = DateTime.UtcNow;
        }

        /// <inheritdoc />
        public DateTime Timestamp { get; }

        /// <inheritdoc />
        public abstract string EventType { get; }
    }
}