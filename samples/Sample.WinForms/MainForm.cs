using System;
using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sample.WinForms
{
    public class MainForm : Form
    {
        private Button _requestButton;
        private Button _logButton;
        private Button _openDevToolsButton;
        private TextBox _logTextBox;
        private HttpClient _httpClient;

        public MainForm()
        {
            InitializeComponent();
            _httpClient = SharpInspect.SharpInspectDevTools.CreateHttpClient();
        }

        private void InitializeComponent()
        {
            this.Text = "SharpInspect - WinForms Sample";
            this.Size = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterScreen;

            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(5),
                FlowDirection = FlowDirection.LeftToRight
            };

            _requestButton = new Button
            {
                Text = "Make HTTP Request",
                Width = 130
            };
            _requestButton.Click += RequestButton_Click;

            _logButton = new Button
            {
                Text = "Write Log",
                Width = 100
            };
            _logButton.Click += LogButton_Click;

            _openDevToolsButton = new Button
            {
                Text = "Open DevTools",
                Width = 100
            };
            _openDevToolsButton.Click += OpenDevToolsButton_Click;

            panel.Controls.Add(_requestButton);
            panel.Controls.Add(_logButton);
            panel.Controls.Add(_openDevToolsButton);

            _logTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 9)
            };

            this.Controls.Add(_logTextBox);
            this.Controls.Add(panel);

            AppendLog("SharpInspect WinForms Sample");
            AppendLog($"DevTools URL: {SharpInspect.SharpInspectDevTools.DevToolsUrl}");
            AppendLog("");
            AppendLog("Click 'Make HTTP Request' to capture network traffic.");
            AppendLog("Click 'Write Log' to capture console output.");
            AppendLog("Click 'Open DevTools' to view captured data.");
        }

        private async void RequestButton_Click(object sender, EventArgs e)
        {
            _requestButton.Enabled = false;

            try
            {
                AppendLog("Making HTTP requests...");

                // GET request
                AppendLog("  GET https://jsonplaceholder.typicode.com/posts/1");
                var response = await _httpClient.GetStringAsync("https://jsonplaceholder.typicode.com/posts/1");
                AppendLog($"  Response received: {response.Length} bytes");

                // POST request
                AppendLog("  POST https://jsonplaceholder.typicode.com/posts");
                var content = new StringContent(
                    "{\"title\": \"test\", \"body\": \"test body\", \"userId\": 1}",
                    System.Text.Encoding.UTF8,
                    "application/json");
                var postResponse = await _httpClient.PostAsync("https://jsonplaceholder.typicode.com/posts", content);
                AppendLog($"  Response: {postResponse.StatusCode}");

                AppendLog("Requests completed! Check DevTools.");
            }
            catch (Exception ex)
            {
                AppendLog($"Error: {ex.Message}");
            }
            finally
            {
                _requestButton.Enabled = true;
            }
        }

        private void LogButton_Click(object sender, EventArgs e)
        {
            AppendLog("Writing log messages...");

            Console.WriteLine("[INFO] Sample information message from WinForms");
            Console.WriteLine("[DEBUG] Debug message with data: userId=123");
            Console.WriteLine("[WARNING] Warning message");
            Console.Error.WriteLine("[ERROR] Error message");

            Console.WriteLine("JSON data: {\"app\": \"WinForms\", \"timestamp\": \"" + DateTime.Now.ToString("o") + "\"}");

            AppendLog("Log messages written! Check DevTools Console tab.");
        }

        private void OpenDevToolsButton_Click(object sender, EventArgs e)
        {
            SharpInspect.SharpInspectDevTools.OpenDevTools();
            AppendLog("Opening DevTools in browser...");
        }

        private void AppendLog(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => AppendLog(message)));
                return;
            }

            _logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _httpClient?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
