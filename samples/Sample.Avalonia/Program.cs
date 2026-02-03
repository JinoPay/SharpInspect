using Avalonia;
using SharpInspect;

namespace Sample.Avalonia;

internal class Program
{
    /// <summary>
    /// 애플리케이션 진입점
    /// </summary>
    [STAThread]
    public static void Main(string[] args)
    {
        // SharpInspect 초기화
        SharpInspectDevTools.Initialize(options =>
        {
            options.Port = 9229;
            options.EnableConsoleCapture = true;
            options.EnableNetworkCapture = true;
        });

        Console.WriteLine("SharpInspect is now running!");
        Console.WriteLine($"Open your browser at: {SharpInspectDevTools.DevToolsUrl}");

        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            SharpInspectDevTools.Shutdown();
        }
    }

    /// <summary>
    /// Avalonia 앱 빌더 구성
    /// </summary>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
