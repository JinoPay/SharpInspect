using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SharpInspect;

namespace Sample.Avalonia;

/// <summary>
/// 메인 윈도우 코드-비하인드
/// </summary>
public partial class MainWindow : Window
{
    private readonly HttpClient _httpClient;
    private readonly StringBuilder _output = new();

    public MainWindow()
    {
        InitializeComponent();

        // SharpInspect 인터셉션이 포함된 HttpClient 생성
        _httpClient = SharpInspectDevTools.CreateHttpClient();

        // DevTools URL 표시
        DevToolsUrlText.Text = $"DevTools URL: {SharpInspectDevTools.DevToolsUrl}";

        AppendOutput("=== SharpInspect Avalonia Sample ===");
        AppendOutput($"DevTools: {SharpInspectDevTools.DevToolsUrl}");
        AppendOutput("");
        AppendOutput("준비 완료. 버튼을 클릭하여 테스트하세요.");
    }

    /// <summary>
    /// HTTP 요청 보내기 버튼 클릭 핸들러
    /// </summary>
    private async void OnMakeRequestsClick(object? sender, RoutedEventArgs e)
    {
        StatusText.Text = "상태: HTTP 요청 중...";
        AppendOutput("");
        AppendOutput("--- HTTP 요청 시작 ---");

        try
        {
            // GET 요청
            AppendOutput("GET https://jsonplaceholder.typicode.com/posts/1");
            var response1 = await _httpClient.GetStringAsync("https://jsonplaceholder.typicode.com/posts/1");
            AppendOutput($"  응답: {response1.Substring(0, Math.Min(80, response1.Length))}...");

            // 쿼리 포함 GET 요청
            AppendOutput("GET https://jsonplaceholder.typicode.com/comments?postId=1");
            var response2 = await _httpClient.GetStringAsync("https://jsonplaceholder.typicode.com/comments?postId=1");
            AppendOutput($"  응답: {response2.Substring(0, Math.Min(80, response2.Length))}...");

            // POST 요청
            AppendOutput("POST https://jsonplaceholder.typicode.com/posts");
            var content = new StringContent(
                """{"title": "foo", "body": "bar", "userId": 1}""",
                Encoding.UTF8,
                "application/json");
            var response3 = await _httpClient.PostAsync("https://jsonplaceholder.typicode.com/posts", content);
            var body3 = await response3.Content.ReadAsStringAsync();
            AppendOutput($"  응답: {response3.StatusCode} - {body3.Substring(0, Math.Min(60, body3.Length))}...");

            // 다중 GET 요청
            AppendOutput("다중 요청 (posts/1 ~ posts/5)...");
            for (var i = 1; i <= 5; i++)
            {
                await _httpClient.GetStringAsync($"https://jsonplaceholder.typicode.com/posts/{i}");
                AppendOutput($"  GET posts/{i} 완료");
            }

            AppendOutput("--- 모든 요청 완료! DevTools Network 탭을 확인하세요. ---");
            StatusText.Text = "상태: HTTP 요청 완료";
        }
        catch (Exception ex)
        {
            AppendOutput($"오류: {ex.Message}");
            StatusText.Text = $"상태: 오류 - {ex.Message}";
        }
    }

    /// <summary>
    /// 로그 메시지 작성 버튼 클릭 핸들러
    /// </summary>
    private void OnWriteLogsClick(object? sender, RoutedEventArgs e)
    {
        StatusText.Text = "상태: 로그 작성 중...";
        AppendOutput("");
        AppendOutput("--- 로그 메시지 작성 ---");

        Console.WriteLine("[INFO] 정보 메시지입니다");
        AppendOutput("  [INFO] 정보 메시지");

        Console.WriteLine("[DEBUG] 디버그 메시지: { \"id\": 123, \"name\": \"test\" }");
        AppendOutput("  [DEBUG] 디버그 메시지");

        Console.WriteLine("[WARNING] 경고 메시지입니다");
        AppendOutput("  [WARNING] 경고 메시지");

        Console.Error.WriteLine("[ERROR] 오류 메시지입니다");
        AppendOutput("  [ERROR] 오류 메시지");

        // 구조화된 출력
        Console.WriteLine("샘플 데이터:");
        Console.WriteLine("{");
        Console.WriteLine("  \"app\": \"SharpInspect\",");
        Console.WriteLine("  \"framework\": \"Avalonia\",");
        Console.WriteLine("  \"features\": [\"network\", \"console\", \"performance\"]");
        Console.WriteLine("}");
        AppendOutput("  구조화된 JSON 데이터 출력");

        // 처리 시뮬레이션
        for (var i = 1; i <= 3; i++)
        {
            Console.WriteLine($"처리 단계 {i}/3 진행 중...");
        }
        AppendOutput("  처리 단계 1~3 출력");

        AppendOutput("--- 로그 작성 완료! DevTools Console 탭을 확인하세요. ---");
        StatusText.Text = "상태: 로그 작성 완료";
    }

    /// <summary>
    /// DevTools 열기 버튼 클릭 핸들러
    /// </summary>
    private void OnOpenDevToolsClick(object? sender, RoutedEventArgs e)
    {
        SharpInspectDevTools.OpenDevTools();
        AppendOutput("");
        AppendOutput("브라우저에서 DevTools를 여는 중...");
        StatusText.Text = "상태: DevTools 열기";
    }

    /// <summary>
    /// 출력 영역에 텍스트 추가
    /// </summary>
    private void AppendOutput(string text)
    {
        _output.AppendLine(text);
        OutputText.Text = _output.ToString();
    }
}
