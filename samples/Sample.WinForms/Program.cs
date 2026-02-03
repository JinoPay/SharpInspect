using System;
using System.Windows.Forms;
using SharpInspect;

namespace Sample.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // 애플리케이션 시작 전 SharpInspect 초기화
        SharpInspectDevTools.Initialize(options =>
        {
            options.Port = 9229;
            options.EnableConsoleCapture = true;
            options.EnableNetworkCapture = true;
        });

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());

        // 애플리케이션 종료 시 SharpInspect 종료
        SharpInspectDevTools.Shutdown();
    }
}
