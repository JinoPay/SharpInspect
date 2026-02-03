#if MODERN_DOTNET || NETSTANDARD2_0
using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;
using SharpInspect.Core.Configuration;
using SharpInspect.Core.Events;
using SharpInspect.Core.Models;
using SharpInspect.Core.Storage;

namespace SharpInspect.Core.Logging
{
    /// <summary>
    ///     ILogger implementation that captures log entries for SharpInspect.
    ///     Available for .NET Standard 2.0+ and .NET 6+.
    /// </summary>
    public class SharpInspectLogger : ILogger
    {
        private readonly EventBus _eventBus;
        private readonly ISharpInspectStore _store;
        private readonly SharpInspectOptions _options;
        private readonly string _categoryName;

        /// <summary>
        ///     Creates a new SharpInspectLogger.
        /// </summary>
        public SharpInspectLogger(
            string categoryName,
            ISharpInspectStore store,
            SharpInspectOptions options,
            EventBus eventBus = null)
        {
            _categoryName = categoryName ?? "";
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _eventBus = eventBus ?? EventBus.Instance;
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            if (!_options.EnableConsoleCapture)
                return false;

            var minLevel = MapLogLevel(_options.MinLogLevel);
            return logLevel >= minLevel;
        }

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state)
        {
            // Scopes are not captured currently
            return NullScope.Instance;
        }

        /// <inheritdoc />
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);
            if (string.IsNullOrEmpty(message) && exception == null)
                return;

            var entry = new ConsoleEntry
            {
                Message = message,
                Level = MapToSharpInspectLevel(logLevel),
                Category = _categoryName,
                Source = GetSource()
            };

            if (exception != null)
            {
                entry.ExceptionType = exception.GetType().FullName;
                entry.ExceptionMessage = exception.Message;
                entry.ExceptionDetails = exception.ToString();
                entry.StackTrace = exception.StackTrace;
            }

            _store.AddConsoleEntry(entry);
            _eventBus.PublishAsync(new ConsoleEntryEvent(entry));
        }

        private LogLevel MapLogLevel(SharpInspectLogLevel level)
        {
            switch (level)
            {
                case SharpInspectLogLevel.Trace:
                    return LogLevel.Trace;
                case SharpInspectLogLevel.Debug:
                    return LogLevel.Debug;
                case SharpInspectLogLevel.Information:
                    return LogLevel.Information;
                case SharpInspectLogLevel.Warning:
                    return LogLevel.Warning;
                case SharpInspectLogLevel.Error:
                    return LogLevel.Error;
                case SharpInspectLogLevel.Critical:
                    return LogLevel.Critical;
                default:
                    return LogLevel.Information;
            }
        }

        private SharpInspectLogLevel MapToSharpInspectLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace:
                    return SharpInspectLogLevel.Trace;
                case LogLevel.Debug:
                    return SharpInspectLogLevel.Debug;
                case LogLevel.Information:
                    return SharpInspectLogLevel.Information;
                case LogLevel.Warning:
                    return SharpInspectLogLevel.Warning;
                case LogLevel.Error:
                    return SharpInspectLogLevel.Error;
                case LogLevel.Critical:
                    return SharpInspectLogLevel.Critical;
                default:
                    return SharpInspectLogLevel.Information;
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

                    // Skip SharpInspect and Microsoft.Extensions.Logging internal frames
                    if (typeName.StartsWith("SharpInspect.") ||
                        typeName.StartsWith("Microsoft.Extensions.Logging"))
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

        private class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }

    /// <summary>
    ///     ILoggerProvider that creates SharpInspectLogger instances.
    /// </summary>
    public class SharpInspectLoggerProvider : ILoggerProvider
    {
        private readonly EventBus _eventBus;
        private readonly ISharpInspectStore _store;
        private readonly SharpInspectOptions _options;

        /// <summary>
        ///     Creates a new SharpInspectLoggerProvider.
        /// </summary>
        public SharpInspectLoggerProvider(
            ISharpInspectStore store,
            SharpInspectOptions options,
            EventBus eventBus = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _eventBus = eventBus ?? EventBus.Instance;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // Nothing to dispose
        }

        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName)
        {
            return new SharpInspectLogger(categoryName, _store, _options, _eventBus);
        }
    }
}
#endif