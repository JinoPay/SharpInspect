using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using SharpInspect.Core.Configuration;
using SharpInspect.Core.Events;
using SharpInspect.Core.Models;
using SharpInspect.Core.Storage;

namespace SharpInspect.Core.Logging
{
    /// <summary>
    ///     Debug.WriteLine과 Trace.WriteLine을 후킹하여 트레이스 출력을 캡처합니다.
    ///     .NET Framework 3.5+ 호환.
    /// </summary>
    public class TraceHook : TraceListener, IDisposable
    {
        private readonly EventBus _eventBus;
        private readonly ISharpInspectStore _store;
        private readonly SharpInspectOptions _options;
        private readonly StringBuilder _buffer;
        private bool _disposed;

        /// <summary>
        ///     새 TraceHook을 생성하고 트레이스 리스너로 등록합니다.
        /// </summary>
        public TraceHook(
            ISharpInspectStore store,
            SharpInspectOptions options,
            EventBus eventBus = null)
        {
            _store = store ?? throw new ArgumentNullException("store");
            _options = options ?? throw new ArgumentNullException("options");
            _eventBus = eventBus ?? EventBus.Instance;
            _buffer = new StringBuilder();

            // 이 리스너를 트레이스 리스너 컬렉션에 추가
            Trace.Listeners.Add(this);
#if NETFRAMEWORK || NET35
            Debug.Listeners.Add(this);
#endif
        }

        /// <summary>
        ///     이 리스너의 이름을 가져옵니다.
        /// </summary>
        public override string Name => "SharpInspectTraceHook";

        /// <summary>
        ///     카테고리를 포함한 트레이스 정보를 작성합니다.
        /// </summary>
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id,
            string message)
        {
            if (!_options.EnableConsoleCapture)
                return;

            var level = MapEventType(eventType);
            if (_options.MinLogLevel > level)
                return;

            var entry = new ConsoleEntry
            {
                Message = message,
                Level = level,
                Category = source ?? "Trace",
                Source = GetSource()
            };

            _store.AddConsoleEntry(entry);

#if NET35
            _eventBus.Publish(new ConsoleEntryEvent(entry));
#else
            _eventBus.PublishAsync(new ConsoleEntryEvent(entry));
#endif
        }

        /// <summary>
        ///     서식이 지정된 메시지로 트레이스 정보를 작성합니다.
        /// </summary>
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id,
            string format, params object[] args)
        {
            string message;
            try
            {
                message = args != null && args.Length > 0
                    ? string.Format(format, args)
                    : format;
            }
            catch
            {
                message = format;
            }

            TraceEvent(eventCache, source, eventType, id, message);
        }

        /// <summary>
        ///     트레이스 출력에 메시지를 작성합니다.
        /// </summary>
        public override void Write(string message)
        {
            if (message == null)
                return;

            lock (_buffer)
            {
                _buffer.Append(message);
            }
        }

        /// <summary>
        ///     메시지에 줄 종결자를 붙여 작성합니다.
        /// </summary>
        public override void WriteLine(string message)
        {
            string fullMessage;
            lock (_buffer)
            {
                _buffer.Append(message);
                fullMessage = _buffer.ToString();
                _buffer.Length = 0;
            }

            if (!_options.EnableConsoleCapture)
                return;

            if (string.IsNullOrEmpty(fullMessage))
                return;

            var level = SharpInspectLogLevel.Debug;
            if (_options.MinLogLevel > level)
                return;

            var entry = new ConsoleEntry
            {
                Message = fullMessage,
                Level = level,
                Category = "Trace",
                Source = GetSource()
            };

            _store.AddConsoleEntry(entry);

#if NET35
            _eventBus.Publish(new ConsoleEntryEvent(entry));
#else
            _eventBus.PublishAsync(new ConsoleEntryEvent(entry));
#endif
        }

        /// <summary>
        ///     후킹을 해제하고 트레이스 리스너에서 제거합니다.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                if (disposing)
                {
                    Trace.Listeners.Remove(this);
#if NETFRAMEWORK || NET35
                    Debug.Listeners.Remove(this);
#endif
                }
            }

            base.Dispose(disposing);
        }

        private SharpInspectLogLevel MapEventType(TraceEventType eventType)
        {
            switch (eventType)
            {
                case TraceEventType.Critical:
                    return SharpInspectLogLevel.Critical;
                case TraceEventType.Error:
                    return SharpInspectLogLevel.Error;
                case TraceEventType.Warning:
                    return SharpInspectLogLevel.Warning;
                case TraceEventType.Information:
                    return SharpInspectLogLevel.Information;
                case TraceEventType.Verbose:
                    return SharpInspectLogLevel.Trace;
                default:
                    return SharpInspectLogLevel.Debug;
            }
        }

        private string GetSource()
        {
            try
            {
                var stackTrace = new StackTrace(true);
                var frames = stackTrace.GetFrames();
                if (frames == null)
                    return null;

                foreach (var frame in frames)
                {
                    var method = frame.GetMethod();
                    if (method == null)
                        continue;

                    var declaringType = method.DeclaringType;
                    if (declaringType == null)
                        continue;

                    var typeName = declaringType.FullName ?? "";

                    // SharpInspect 및 System 내부 프레임 건너뛰기
                    if (typeName.StartsWith("SharpInspect.") ||
                        typeName.StartsWith("System."))
                        continue;

                    var fileName = frame.GetFileName();
                    var lineNumber = frame.GetFileLineNumber();

                    if (!string.IsNullOrEmpty(fileName))
                        return string.Format("{0}.{1}() in {2}:line {3}",
                            typeName, method.Name, Path.GetFileName(fileName), lineNumber);
                    return string.Format("{0}.{1}()", typeName, method.Name);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
