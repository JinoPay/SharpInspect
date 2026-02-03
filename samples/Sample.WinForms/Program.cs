using System;
using System.Windows.Forms;

namespace Sample.WinForms
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // Initialize SharpInspect before the application starts
            SharpInspect.SharpInspectDevTools.Initialize(options =>
            {
                options.Port = 9229;
                options.EnableConsoleCapture = true;
                options.EnableNetworkCapture = true;
            });

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());

            // Shutdown SharpInspect when the application closes
            SharpInspect.SharpInspectDevTools.Shutdown();
        }
    }
}
