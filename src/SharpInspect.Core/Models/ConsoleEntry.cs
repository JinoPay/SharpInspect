using System;

namespace SharpInspect.Core.Models
{
    /// <summary>
    ///     Log level enumeration compatible with all .NET versions.
    /// </summary>
    public enum SharpInspectLogLevel
    {
        Trace = 0,
        Debug = 1,
        Information = 2,
        Warning = 3,
        Error = 4,
        Critical = 5
    }

    /// <summary>
    ///     Represents a captured console/log entry.
    /// </summary>
    public class ConsoleEntry
    {
        /// <summary>
        ///     Creates a new ConsoleEntry with a unique ID and current timestamp.
        /// </summary>
        public ConsoleEntry()
        {
            Id = Guid.NewGuid().ToString("N");
            Timestamp = DateTime.UtcNow;
            Level = SharpInspectLogLevel.Information;
        }

        /// <summary>
        ///     Creates a new ConsoleEntry with the specified message and level.
        /// </summary>
        public ConsoleEntry(string message, SharpInspectLogLevel level = SharpInspectLogLevel.Information)
            : this()
        {
            Message = message;
            Level = level;
        }

        /// <summary>
        ///     Timestamp when the log entry was created.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        ///     Log level (Trace, Debug, Info, Warning, Error, Critical).
        /// </summary>
        public SharpInspectLogLevel Level { get; set; }

        /// <summary>
        ///     Logger category or source name.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        ///     Full exception details including inner exceptions.
        /// </summary>
        public string ExceptionDetails { get; set; }

        /// <summary>
        ///     Exception message if an exception was logged.
        /// </summary>
        public string ExceptionMessage { get; set; }

        /// <summary>
        ///     Exception type name if an exception was logged.
        /// </summary>
        public string ExceptionType { get; set; }

        /// <summary>
        ///     Unique identifier for this entry.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     Log message content.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        ///     Source code location that generated this log.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        ///     Stack trace if available.
        /// </summary>
        public string StackTrace { get; set; }

        /// <summary>
        ///     Creates a new ConsoleEntry from an exception.
        /// </summary>
        public static ConsoleEntry FromException(Exception ex, string message = null)
        {
            var entry = new ConsoleEntry
            {
                Level = SharpInspectLogLevel.Error,
                Message = message ?? ex.Message,
                ExceptionType = ex.GetType().FullName,
                ExceptionMessage = ex.Message,
                ExceptionDetails = ex.ToString(),
                StackTrace = ex.StackTrace
            };
            return entry;
        }
    }
}