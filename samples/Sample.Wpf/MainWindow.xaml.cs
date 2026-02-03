using System.Net.Http;
using System.Text;
using System.Windows;
using SharpInspect;

namespace Sample.Wpf;

/// <summary>
/// SharpInspect WPF 샘플 메인 윈도우
/// </summary>
public partial class MainWindow : Window
{
    private readonly HttpClient _httpClient;

    public MainWindow()
    {
        InitializeComponent();

        // SharpInspect 인터셉션이 포함된 HttpClient 생성
        _httpClient = SharpInspectDevTools.CreateHttpClient();

        // 초기 로그 메시지 출력
        AppendLog("SharpInspect WPF Sample");
        AppendLog($"DevTools URL: {SharpInspectDevTools.DevToolsUrl}");
        AppendLog("");
        AppendLog("Click 'Make HTTP Request' to capture network traffic.");
        AppendLog("Click 'Write Log' to capture console output.");
        AppendLog("Click 'Open DevTools' to view captured data.");
    }

    /// <summary>
    /// 로그 메시지를 텍스트 박스에 추가
    /// </summary>
    private void AppendLog(string message)
    {
        Dispatcher.Invoke(() =>
        {
            LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            LogTextBox.ScrollToEnd();
        });
    }

    /// <summary>
    /// HTTP 요청 버튼 클릭 핸들러
    /// </summary>
    private async void RequestButton_Click(object sender, RoutedEventArgs e)
    {
        RequestButton.IsEnabled = false;

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
                """{"title": "WPF Sample", "body": "Test from WPF app", "userId": 1}""",
                Encoding.UTF8,
                "application/json");
            var postResponse = await _httpClient.PostAsync("https://jsonplaceholder.typicode.com/posts", content);
            AppendLog($"  Response: {postResponse.StatusCode}");

            // PUT 요청
            AppendLog("  PUT https://jsonplaceholder.typicode.com/posts/1");
            var putContent = new StringContent(
                """{"id": 1, "title": "Updated Title", "body": "Updated body", "userId": 1}""",
                Encoding.UTF8,
                "application/json");
            var putResponse = await _httpClient.PutAsync("https://jsonplaceholder.typicode.com/posts/1", putContent);
            AppendLog($"  Response: {putResponse.StatusCode}");

            // DELETE 요청
            AppendLog("  DELETE https://jsonplaceholder.typicode.com/posts/1");
            var deleteResponse = await _httpClient.DeleteAsync("https://jsonplaceholder.typicode.com/posts/1");
            AppendLog($"  Response: {deleteResponse.StatusCode}");

            AppendLog("All requests completed! Check DevTools Network tab.");
        }
        catch (Exception ex)
        {
            AppendLog($"Error: {ex.Message}");
        }
        finally
        {
            RequestButton.IsEnabled = true;
        }
    }

    /// <summary>
    /// 로그 작성 버튼 클릭 핸들러
    /// </summary>
    private void LogButton_Click(object sender, RoutedEventArgs e)
    {
        AppendLog("Writing log messages...");

        Console.WriteLine("[INFO] Sample information message from WPF");
        Console.WriteLine("[DEBUG] Debug message with data: userId=123, action=click");
        Console.WriteLine("[WARNING] Warning: Resource usage is high");
        Console.Error.WriteLine("[ERROR] Error: Connection timeout occurred");

        // JSON 형식 로그
        Console.WriteLine("JSON data: " + """{"app": "WPF", "version": "1.0", "timestamp": """ + $"\"{DateTime.Now:o}\"" + "}");

        // 처리 시뮬레이션
        for (var i = 1; i <= 3; i++)
        {
            Console.WriteLine($"[TRACE] Processing step {i}/3...");
        }

        AppendLog("Log messages written! Check DevTools Console tab.");
    }

    /// <summary>
    /// DevTools 열기 버튼 클릭 핸들러
    /// </summary>
    private void OpenDevToolsButton_Click(object sender, RoutedEventArgs e)
    {
        SharpInspectDevTools.OpenDevTools();
        AppendLog("Opening DevTools in browser...");
    }

    /// <summary>
    /// 윈도우 종료 시 리소스 정리
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        _httpClient?.Dispose();
        base.OnClosed(e);
    }
}
