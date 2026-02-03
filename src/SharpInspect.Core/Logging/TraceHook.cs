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
    ///     Hooks Debug.WriteLine and Trace.WriteLine to capture trace output.
    ///     Compatible with .NET Framework 3.5+.
    /// </summary>
    public class TraceHook : TraceListener, IDisposable
    {
        private readonly EventBus _eventBus;
        private readonly ISharpInspectStore _store;
        private readonly SharpInspectOptions _options;
        private readonly StringBuilder _buffer;
        private bool _disposed;

        /// <summary>
        ///     Creates a new TraceHook and registers it as a trace listener.
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

            // Add this listener to the trace listeners collection
            Trace.Listeners.Add(this);
#if NETFRAMEWORK || NET35
            Debug.Listeners.Add(this);
#endif
        }

        /// <summary>
        ///     Gets the name of this listener.
        /// </summary>
        public override string Name => "SharpInspectTraceHook";

        /// <summary>
        ///     Writes trace information including category.
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
        ///     Writes trace information with formatted message.
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
        ///     Writes a message to the trace output.
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
        ///     Writes a message followed by a line terminator.
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
        ///     Disposes the hook and removes it from the trace listeners.
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

                    // Skip SharpInspect and System internal frames
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