using System;
using System.Collections.Generic;
#if !NET35
using System.Threading;
#endif

namespace SharpInspect.Core.Events
{
    /// <summary>
    ///     SharpInspect 이벤트 처리를 위한 대리자.
    /// </summary>
    /// <typeparam name="T">이벤트의 타입.</typeparam>
    /// <param name="evt">이벤트 인스턴스.</param>
    public delegate void EventHandler<T>(T evt) where T : ISharpInspectEvent;

    /// <summary>
    ///     SharpInspect 이벤트를 위한 간단한 인프로세스 이벤트 버스.
    ///     스레드 안전하며 .NET 3.5+ 호환.
    /// </summary>
    public class EventBus
    {
        private static EventBus _instance;
        private static readonly object _instanceLock = new();

        private readonly Dictionary<Type, List<Delegate>> _handlers;
        private readonly object _handlersLock = new();

        /// <summary>
        ///     새 이벤트 버스 인스턴스를 생성합니다.
        /// </summary>
        public EventBus()
        {
            _handlers = new Dictionary<Type, List<Delegate>>();
        }

        /// <summary>
        ///     이벤트 버스의 싱글턴 인스턴스를 가져옵니다.
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
        ///     T 타입의 이벤트에 핸들러를 구독합니다.
        /// </summary>
        /// <typeparam name="T">구독할 이벤트 타입.</typeparam>
        /// <param name="handler">이벤트가 발행될 때 호출할 핸들러.</param>
        /// <returns>해제 시 핸들러 구독을 취소하는 IDisposable.</returns>
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
        ///     특정 이벤트 타입의 구독자 수를 가져옵니다.
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
        ///     모든 구독을 지웁니다.
        /// </summary>
        public void ClearAll()
        {
            lock (_handlersLock)
            {
                _handlers.Clear();
            }
        }

        /// <summary>
        ///     모든 구독된 핸들러에 이벤트를 발행합니다.
        /// </summary>
        /// <typeparam name="T">이벤트의 타입.</typeparam>
        /// <param name="evt">발행할 이벤트.</param>
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
                    // 발행 루프 중단 방지를 위해 핸들러 예외 무시
                }
        }

#if !NET35
        /// <summary>
        ///     스레드 풀에서 이벤트를 비동기로 발행합니다.
        /// </summary>
        /// <typeparam name="T">이벤트의 타입.</typeparam>
        /// <param name="evt">발행할 이벤트.</param>
        public void PublishAsync<T>(T evt) where T : ISharpInspectEvent
        {
            if (evt == null)
                return;

            ThreadPool.QueueUserWorkItem(_ => Publish(evt));
        }
#endif

        /// <summary>
        ///     T 타입의 이벤트에서 핸들러 구독을 취소합니다.
        /// </summary>
        /// <typeparam name="T">구독 취소할 이벤트 타입.</typeparam>
        /// <param name="handler">제거할 핸들러.</param>
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
