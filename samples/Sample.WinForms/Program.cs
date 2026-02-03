using System;
using System.Windows.Forms;
using SharpInspect;

namespace Sample.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Initialize SharpInspect before the application starts
        SharpInspectDevTools.Initialize(options =>
        {
            options.Port = 9229;
            options.EnableConsoleCapture = true;
            options.EnableNetworkCapture = true;
        });

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());

        // Shutdown SharpInspect when the application closes
        SharpInspectDevTools.Shutdown();
    }
}