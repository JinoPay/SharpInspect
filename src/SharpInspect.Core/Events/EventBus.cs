using System;
using System.Collections.Generic;
#if !NET35
using System.Threading;
#endif

namespace SharpInspect.Core.Events
{
    /// <summary>
    ///     Delegate for handling SharpInspect events.
    /// </summary>
    /// <typeparam name="T">The type of event.</typeparam>
    /// <param name="evt">The event instance.</param>
    public delegate void EventHandler<T>(T evt) where T : ISharpInspectEvent;

    /// <summary>
    ///     A simple in-process event bus for SharpInspect events.
    ///     Thread-safe and compatible with .NET 3.5+.
    /// </summary>
    public class EventBus
    {
        private static EventBus _instance;
        private static readonly object _instanceLock = new();

        private readonly Dictionary<Type, List<Delegate>> _handlers;
        private readonly object _handlersLock = new();

        /// <summary>
        ///     Creates a new event bus instance.
        /// </summary>
        public EventBus()
        {
            _handlers = new Dictionary<Type, List<Delegate>>();
        }

        /// <summary>
        ///     Gets the singleton instance of the event bus.
        /// </summary>
        public static EventBus Instance
        {
            get
            {
                if (_instance == null)
                    lock (_instanceLock)
                    {
                        if (_instance == null) _instance = new EventBus();
                    }

                return _instance;
            }
        }

        /// <summary>
        ///     Subscribes a handler to events of type T.
        /// </summary>
        /// <typeparam name="T">The type of event to subscribe to.</typeparam>
        /// <param name="handler">The handler to invoke when an event is published.</param>
        /// <returns>An IDisposable that unsubscribes the handler when disposed.</returns>
        public IDisposable Subscribe<T>(EventHandler<T> handler) where T : ISharpInspectEvent
        {
            if (handler == null)
                throw new ArgumentNullException("handler");

            var eventType = typeof(T);

            lock (_handlersLock)
            {
                List<Delegate> handlers;
                if (!_handlers.TryGetValue(eventType, out handlers))
                {
                    handlers = new List<Delegate>();
                    _handlers[eventType] = handlers;
                }

                handlers.Add(handler);
            }

            return new Subscription(this, eventType, handler);
        }

        /// <summary>
        ///     Gets the number of subscribers for a specific event type.
        /// </summary>
        public int GetSubscriberCount<T>() where T : ISharpInspectEvent
        {
            var eventType = typeof(T);
            lock (_handlersLock)
            {
                List<Delegate> handlers;
                if (_handlers.TryGetValue(eventType, out handlers)) return handlers.Count;
                return 0;
            }
        }

        /// <summary>
        ///     Clears all subscriptions.
        /// </summary>
        public void ClearAll()
        {
            lock (_handlersLock)
            {
                _handlers.Clear();
            }
        }

        /// <summary>
        ///     Publishes an event to all subscribed handlers.
        /// </summary>
        /// <typeparam name="T">The type of event.</typeparam>
        /// <param name="evt">The event to publish.</param>
        public void Publish<T>(T evt) where T : ISharpInspectEvent
        {
            if (evt == null)
                return;

            var eventType = typeof(T);
            Delegate[] handlersCopy;

            lock (_handlersLock)
            {
                List<Delegate> handlers;
                if (!_handlers.TryGetValue(eventType, out handlers) || handlers.Count == 0)
                    return;

                handlersCopy = handlers.ToArray();
            }

            foreach (var handler in handlersCopy)
                try
                {
                    var typedHandler = handler as EventHandler<T>;
                    if (typedHandler != null) typedHandler(evt);
                }
                catch
                {
                    // Swallow exceptions from handlers to prevent breaking the publish loop
                }
        }

#if !NET35
        /// <summary>
        ///     Publishes an event asynchronously on the thread pool.
        /// </summary>
        /// <typeparam name="T">The type of event.</typeparam>
        /// <param name="evt">The event to publish.</param>
        public void PublishAsync<T>(T evt) where T : ISharpInspectEvent
        {
            if (evt == null)
                return;

            ThreadPool.QueueUserWorkItem(_ => Publish(evt));
        }
#endif

        /// <summary>
        ///     Unsubscribes a handler from events of type T.
        /// </summary>
        /// <typeparam name="T">The type of event to unsubscribe from.</typeparam>
        /// <param name="handler">The handler to remove.</param>
        public void Unsubscribe<T>(EventHandler<T> handler) where T : ISharpInspectEvent
        {
            if (handler == null)
                return;

            var eventType = typeof(T);

            lock (_handlersLock)
            {
                List<Delegate> handlers;
                if (_handlers.TryGetValue(eventType, out handlers)) handlers.Remove(handler);
            }
        }

        private void Unsubscribe(Type eventType, Delegate handler)
        {
            lock (_handlersLock)
            {
                List<Delegate> handlers;
                if (_handlers.TryGetValue(eventType, out handlers)) handlers.Remove(handler);
            }
        }

        private class Subscription : IDisposable
        {
            private readonly Delegate _handler;
            private readonly EventBus _bus;
            private readonly Type _eventType;
            private bool _disposed;

            public Subscription(EventBus bus, Type eventType, Delegate handler)
            {
                _bus = bus;
                _eventType = eventType;
                _handler = handler;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    _bus.Unsubscribe(_eventType, _handler);
                }
            }
        }
    }
}