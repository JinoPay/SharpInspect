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
    ///     Hooks Console.WriteLine and Console.Write to capture console output.
    ///     Compatible with .NET Framework 3.5+.
    /// </summary>
    public class ConsoleHook : IDisposable
    {
        private readonly EventBus _eventBus;
        private readonly InterceptingTextWriter _interceptedError;
        private readonly InterceptingTextWriter _interceptedOut;
        private readonly ISharpInspectStore _store;
        private readonly SharpInspectOptions _options;
        private readonly TextWriter _originalError;
        private readonly TextWriter _originalOut;
        private bool _disposed;

        /// <summary>
        ///     Creates a new ConsoleHook and starts intercepting console output.
        /// </summary>
        public ConsoleHook(
            ISharpInspectStore store,
            SharpInspectOptions options,
            EventBus eventBus = null)
        {
            _store = store ?? throw new ArgumentNullException("store");
            _options = options ?? throw new ArgumentNullException("options");
            _eventBus = eventBus ?? EventBus.Instance;

            // Save original writers
            _originalOut = Console.Out;
            _originalError = Console.Error;

            // Create intercepting writers
            _interceptedOut = new InterceptingTextWriter(
                _originalOut,
                msg => OnConsoleWrite(msg, SharpInspectLogLevel.Information));

            _interceptedError = new InterceptingTextWriter(
                _originalError,
                msg => OnConsoleWrite(msg, SharpInspectLogLevel.Error));

            // Replace console writers
            Console.SetOut(_interceptedOut);
            Console.SetError(_interceptedError);
        }

        /// <summary>
        ///     Disposes the hook and restores original console writers.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                Console.SetOut(_originalOut);
                Console.SetError(_originalError);
                _interceptedOut.Dispose();
                _interceptedError.Dispose();
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

        private void OnConsoleWrite(string message, SharpInspectLogLevel level)
        {
            if (!_options.EnableConsoleCapture)
                return;

            if (string.IsNullOrEmpty(message))
                return;

            if (level < _options.MinLogLevel)
                return;

            var entry = new ConsoleEntry
            {
                Message = message,
                Level = level,
                Category = level == SharpInspectLogLevel.Error ? "Console.Error" : "Console.Out",
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
        ///     TextWriter that intercepts writes and forwards them to both the original writer and a callback.
        /// </summary>
        private class InterceptingTextWriter : TextWriter
        {
            private readonly Action<string> _onWrite;
            private readonly StringBuilder _lineBuffer;
            private readonly TextWriter _inner;

            public InterceptingTextWriter(TextWriter inner, Action<string> onWrite)
            {
                _inner = inner;
                _onWrite = onWrite;
                _lineBuffer = new StringBuilder();
            }

            public override Encoding Encoding => _inner.Encoding;

            public override void Write(char value)
            {
                _inner.Write(value);

                if (value == '\n')
                    FlushLine();
                else if (value != '\r') _lineBuffer.Append(value);
            }

            public override void Write(string value)
            {
                _inner.Write(value);

                if (value == null)
                    return;

                foreach (var c in value)
                    if (c == '\n')
                        FlushLine();
                    else if (c != '\r') _lineBuffer.Append(c);
            }

            public override void WriteLine(string value)
            {
                _inner.WriteLine(value);
                _lineBuffer.Append(value);
                FlushLine();
            }

            public override void WriteLine()
            {
                _inner.WriteLine();
                FlushLine();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) FlushLine();
                base.Dispose(disposing);
            }

            private void FlushLine()
            {
                if (_lineBuffer.Length > 0)
                {
                    try
                    {
                        _onWrite(_lineBuffer.ToString());
                    }
                    catch
                    {
                        // Ignore errors in callback
                    }

                    _lineBuffer.Length = 0;
                }
            }
        }
    }
}