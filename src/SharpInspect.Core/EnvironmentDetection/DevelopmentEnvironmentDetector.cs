using System;
using System.Diagnostics;
using SharpInspect.Core.Configuration;

namespace SharpInspect.Core.EnvironmentDetection;

/// <summary>
///     개발 환경 감지 유틸리티.
/// </summary>
public static class DevelopmentEnvironmentDetector
{
    /// <summary>
    ///     현재 환경이 개발 환경인지 판별합니다.
    /// </summary>
    /// <param name="options">SharpInspect 옵션.</param>
    /// <returns>개발 환경이면 true, 프로덕션이면 false.</returns>
    public static bool IsDevelopment(SharpInspectOptions options)
    {
        // EnableInDevelopmentOnly가 false면 항상 활성화
        if (!options.EnableInDevelopmentOnly)
            return true;

        switch (options.DevelopmentDetectionMode)
        {
            case DevelopmentDetectionMode.EnvironmentVariableOnly:
                return CheckEnvironmentVariable();

            case DevelopmentDetectionMode.DebuggerOnly:
                return CheckDebuggerAttached();

            case DevelopmentDetectionMode.Custom:
                return options.CustomDevelopmentCheck?.Invoke() ?? CheckAuto();

            case DevelopmentDetectionMode.Auto:
            default:
                return CheckAuto();
        }
    }

    /// <summary>
    ///     Auto 모드: 환경 변수 우선, 디버거 연결 폴백.
    /// </summary>
    private static bool CheckAuto()
    {
        // 1. 환경 변수 우선 확인
        var envResult = CheckEnvironmentVariableWithResult();
        if (envResult.HasValue)
            return envResult.Value;

        // 2. 디버거 연결 상태로 폴백
        return CheckDebuggerAttached();
    }

    /// <summary>
    ///     디버거 연결 상태 확인.
    /// </summary>
    private static bool CheckDebuggerAttached()
    {
        return Debugger.IsAttached;
    }

    /// <summary>
    ///     환경 변수만으로 개발 환경 여부 확인.
    ///     환경 변수가 설정되지 않은 경우 false 반환.
    /// </summary>
    private static bool CheckEnvironmentVariable()
    {
        var result = CheckEnvironmentVariableWithResult();
        return result ?? false;
    }

    /// <summary>
    ///     환경 변수로 개발 환경 여부 확인.
    ///     환경 변수가 설정되지 않은 경우 null 반환.
    /// </summary>
    private static bool? CheckEnvironmentVariableWithResult()
    {
        var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                  ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        if (string.IsNullOrEmpty(env))
            return null;

        return string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase);
    }
}