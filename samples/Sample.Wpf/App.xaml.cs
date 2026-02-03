using System.Windows;
using SharpInspect;

namespace Sample.Wpf;

/// <summary>
/// WPF 애플리케이션 진입점
/// </summary>
public partial class App : Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // 애플리케이션 시작 전 SharpInspect 초기화
        SharpInspectDevTools.Initialize(options =>
        {
            options.Port = 9229;
            options.EnableConsoleCapture = true;
            options.EnableNetworkCapture = true;
        });
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        // 애플리케이션 종료 시 SharpInspect 종료
        SharpInspectDevTools.Shutdown();
    }
}
