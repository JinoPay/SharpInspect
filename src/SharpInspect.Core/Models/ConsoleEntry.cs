using System;

namespace SharpInspect.Core.Models;

/// <summary>
///     모든 .NET 버전과 호환되는 로그 레벨 열거형.
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
///     캡처된 콘솔/로그 엔트리를 나타냅니다.
/// </summary>
public class ConsoleEntry
{
    /// <summary>
    ///     고유 ID와 현재 타임스탬프로 새 ConsoleEntry를 생성합니다.
    /// </summary>
    public ConsoleEntry()
    {
        Id = Guid.NewGuid().ToString("N");
        Timestamp = DateTime.UtcNow;
        Level = SharpInspectLogLevel.Information;
    }

    /// <summary>
    ///     지정된 메시지와 레벨로 새 ConsoleEntry를 생성합니다.
    /// </summary>
    public ConsoleEntry(string message, SharpInspectLogLevel level = SharpInspectLogLevel.Information)
        : this()
    {
        Message = message;
        Level = level;
    }

    /// <summary>
    ///     로그 엔트리가 생성된 타임스탬프.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    ///     로그 레벨 (Trace, Debug, Info, Warning, Error, Critical).
    /// </summary>
    public SharpInspectLogLevel Level { get; set; }

    /// <summary>
    ///     로거 카테고리 또는 소스 이름.
    /// </summary>
    public string Category { get; set; }

    /// <summary>
    ///     내부 예외를 포함한 전체 예외 상세 정보.
    /// </summary>
    public string ExceptionDetails { get; set; }

    /// <summary>
    ///     예외가 로깅된 경우의 예외 메시지.
    /// </summary>
    public string ExceptionMessage { get; set; }

    /// <summary>
    ///     예외가 로깅된 경우의 예외 타입 이름.
    /// </summary>
    public string ExceptionType { get; set; }

    /// <summary>
    ///     이 엔트리의 고유 식별자.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    ///     로그 메시지 내용.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    ///     이 로그를 생성한 소스 코드 위치.
    /// </summary>
    public string Source { get; set; }

    /// <summary>
    ///     사용 가능한 경우의 스택 트레이스.
    /// </summary>
    public string StackTrace { get; set; }

    /// <summary>
    ///     예외로부터 새 ConsoleEntry를 생성합니다.
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