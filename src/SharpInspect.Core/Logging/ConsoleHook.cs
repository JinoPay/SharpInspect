using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using SharpInspect.Core.Configuration;
using SharpInspect.Core.Models;
using SharpInspect.Core.Storage;

namespace SharpInspect.Core.Logging
{
    /// <summary>
    ///     Console.WriteLine과 Console.Write를 후킹하여 콘솔 출력을 캡처합니다.
    ///     .NET Framework 3.5+ 호환.
    /// </summary>
    public class ConsoleHook : IDisposable
    {
        private readonly InterceptingTextWriter _interceptedError;
        private readonly InterceptingTextWriter _interceptedOut;
        private readonly ISharpInspectStore _store;
        private readonly SharpInspectOptions _options;
        private readonly TextWriter _originalError;
        private readonly TextWriter _originalOut;
        private bool _disposed;

        /// <summary>
        ///     새 ConsoleHook을 생성하고 콘솔 출력 인터셉션을 시작합니다.
        /// </summary>
        public ConsoleHook(
            ISharpInspectStore store,
            SharpInspectOptions options)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _options = options ?? throw new ArgumentNullException(nameof(options));

            // 원본 Writer 저장
            _originalOut = Console.Out;
            _originalError = Console.Error;

            // 인터셉팅 Writer 생성
            _interceptedOut = new InterceptingTextWriter(
                _originalOut,
                msg => OnConsoleWrite(msg, SharpInspectLogLevel.Information));

            _interceptedError = new InterceptingTextWriter(
                _originalError,
                msg => OnConsoleWrite(msg, SharpInspectLogLevel.Error));

            // 콘솔 Writer 교체
            Console.SetOut(_interceptedOut);
            Console.SetError(_interceptedError);
        }

        /// <summary>
        ///     [Obsolete] 하위 호환성을 위한 생성자. EventBus 파라미터는 무시됩니다.
        /// </summary>
        [Obsolete("EventBus는 더 이상 직접 전달할 필요가 없습니다. Store에서 자동으로 이벤트를 발행합니다.")]
        public ConsoleHook(
            ISharpInspectStore store,
            SharpInspectOptions options,
            Events.EventBus eventBus)
            : this(store, options)
        {
        }

        /// <summary>
        ///     후킹을 해제하고 원본 콘솔 Writer를 복원합니다.
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

                    // SharpInspect 및 System 내부 프레임 건너뛰기
                    if (typeName.StartsWith("SharpInspect.") ||
                        typeName.StartsWith("System."))
                        continue;

                    var fileName = frame.GetFileName();
                    var lineNumber = frame.GetFileLineNumber();

                    if (!string.IsNullOrEmpty(fileName))
                        return $"{typeName}.{method.Name}() in {Path.GetFileName(fileName)}:line {lineNumber}";
                    return $"{typeName}.{method.Name}()";
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

            // Store에서 자동으로 이벤트 발행
            _store.AddConsoleEntry(entry);
        }

        /// <summary>
        ///     쓰기를 인터셉트하여 원본 Writer와 콜백 모두에 전달하는 TextWriter.
        /// </summary>
        private class InterceptingTextWriter(TextWriter inner, Action<string> onWrite) : TextWriter
        {
            private readonly StringBuilder _lineBuffer = new();

            public override Encoding Encoding => inner.Encoding;

            public override void Write(char value)
            {
                inner.Write(value);

                if (value == '\n')
                    FlushLine();
                else if (value != '\r') _lineBuffer.Append(value);
            }

            public override void Write(string value)
            {
                inner.Write(value);

                if (value == null)
                    return;

                foreach (var c in value)
                    if (c == '\n')
                        FlushLine();
                    else if (c != '\r') _lineBuffer.Append(c);
            }

            public override void WriteLine(string value)
            {
                inner.WriteLine(value);
                _lineBuffer.Append(value);
                FlushLine();
            }

            public override void WriteLine()
            {
                inner.WriteLine();
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
                        onWrite(_lineBuffer.ToString());
                    }
                    catch
                    {
                        // 콜백 오류 무시
                    }

                    _lineBuffer.Length = 0;
                }
            }
        }
    }
}
