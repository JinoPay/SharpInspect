using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Windows.Forms;
using SharpInspect;

namespace Sample.WinForms;

public class MainForm : Form
{
    private Button _logButton;
    private Button _openDevToolsButton;
    private Button _requestButton;
    private Button _formRequestButton;
    private readonly HttpClient _httpClient;
    private TextBox _logTextBox;

    public MainForm()
    {
        InitializeComponent();
        _httpClient = SharpInspectDevTools.CreateHttpClient();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _httpClient?.Dispose();
        base.Dispose(disposing);
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

    private void InitializeComponent()
    {
        Text = "SharpInspect - WinForms Sample";
        Size = new Size(600, 400);
        StartPosition = FormStartPosition.CenterScreen;

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

        _formRequestButton = new Button
        {
            Text = "Form Request",
            Width = 100
        };
        _formRequestButton.Click += FormRequestButton_Click;

        panel.Controls.Add(_requestButton);
        panel.Controls.Add(_formRequestButton);
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

        Controls.Add(_logTextBox);
        Controls.Add(panel);

        AppendLog("SharpInspect WinForms Sample");
        AppendLog($"DevTools URL: {SharpInspectDevTools.DevToolsUrl}");
        AppendLog("");
        AppendLog("Click 'Make HTTP Request' to capture network traffic.");
        AppendLog("Click 'Write Log' to capture console output.");
        AppendLog("Click 'Open DevTools' to view captured data.");
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
        SharpInspectDevTools.OpenDevTools();
        AppendLog("Opening DevTools in browser...");
    }

    private async void FormRequestButton_Click(object sender, EventArgs e)
    {
        _formRequestButton.Enabled = false;

        try
        {
            AppendLog("Making form-encoded requests...");

            // application/x-www-form-urlencoded 요청
            AppendLog("  POST (form-urlencoded) https://httpbin.org/post");
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("title", "test"),
                new KeyValuePair<string, string>("body", "WinForms form data"),
                new KeyValuePair<string, string>("userId", "1"),
                new KeyValuePair<string, string>("tags[]", "csharp"),
                new KeyValuePair<string, string>("tags[]", "winforms")
            });
            var response1 = await _httpClient.PostAsync("https://httpbin.org/post", formContent);
            AppendLog($"  Response: {response1.StatusCode}");

            // multipart/form-data 요청
            AppendLog("  POST (multipart/form-data) https://httpbin.org/post");
            var multipartContent = new MultipartFormDataContent();
            multipartContent.Add(new StringContent("John Doe"), "username");
            multipartContent.Add(new StringContent("john@example.com"), "email");
            multipartContent.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("sample file content")), "attachment", "sample.txt");
            var response2 = await _httpClient.PostAsync("https://httpbin.org/post", multipartContent);
            AppendLog($"  Response: {response2.StatusCode}");

            AppendLog("Form requests completed! Check DevTools Network tab.");
        }
        catch (Exception ex)
        {
            AppendLog($"Error: {ex.Message}");
        }
        finally
        {
            _formRequestButton.Enabled = true;
        }
    }

    private async void RequestButton_Click(object sender, EventArgs e)
    {
        _requestButton.Enabled = false;

        try
        {
            AppendLog("Making HTTP requests...");

            // GET 요청
            AppendLog("  GET https://jsonplaceholder.typicode.com/posts/1");
            var response = await _httpClient.GetStringAsync("https://jsonplaceholder.typicode.com/posts/1");
            AppendLog($"  Response received: {response.Length} bytes");

            // POST 요청
            AppendLog("  POST https://jsonplaceholder.typicode.com/posts");
            var content = new StringContent(
                "{\"title\": \"test\", \"body\": \"test body\", \"userId\": 1}",
                Encoding.UTF8,
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
}
