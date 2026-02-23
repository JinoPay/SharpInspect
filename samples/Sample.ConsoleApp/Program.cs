using System.Text;
using SharpInspect;

namespace Sample.ConsoleApp;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("=== SharpInspect Sample Console App ===");
        Console.WriteLine();

        // SharpInspect 초기화
        SharpInspectDevTools.Initialize(options =>
        {
            options.Port = 9229;
            options.EnableConsoleCapture = true;
            options.EnableNetworkCapture = true;
        });

        Console.WriteLine("SharpInspect is now running!");
        Console.WriteLine($"Open your browser at: {SharpInspectDevTools.DevToolsUrl}");
        Console.WriteLine();
        Console.WriteLine("Press 'r' to make HTTP requests");
        Console.WriteLine("Press 'f' to make form-encoded requests");
        Console.WriteLine("Press 'l' to write log messages");
        Console.WriteLine("Press 'o' to open DevTools in browser");
        Console.WriteLine("Press 'q' to quit");
        Console.WriteLine();

        // SharpInspect 인터셉션이 포함된 HttpClient 생성
        using var httpClient = SharpInspectDevTools.CreateHttpClient();

        var running = true;
        while (running)
        {
            Console.Write("> ");
            var key = Console.ReadKey();
            Console.WriteLine();

            switch (key.KeyChar)
            {
                case 'r':
                    await MakeHttpRequests(httpClient);
                    break;

                case 'f':
                    await MakeFormRequests(httpClient);
                    break;

                case 'l':
                    WriteLogMessages();
                    break;

                case 'o':
                    SharpInspectDevTools.OpenDevTools();
                    Console.WriteLine("Opening DevTools in browser...");
                    break;

                case 'q':
                    running = false;
                    break;

                default:
                    Console.WriteLine("Unknown command. Use 'r', 'f', 'l', 'o', or 'q'.");
                    break;
            }
        }

        Console.WriteLine("Shutting down...");
        SharpInspectDevTools.Shutdown();
    }

    private static async Task MakeHttpRequests(HttpClient httpClient)
    {
        Console.WriteLine("Making HTTP requests...");

        try
        {
            // GET 요청
            Console.WriteLine("  GET https://jsonplaceholder.typicode.com/posts/1");
            var response1 = await httpClient.GetStringAsync("https://jsonplaceholder.typicode.com/posts/1");
            Console.WriteLine($"  Response: {response1.Substring(0, Math.Min(100, response1.Length))}...");

            // 쿼리 포함 GET 요청
            Console.WriteLine("  GET https://jsonplaceholder.typicode.com/comments?postId=1");
            var response2 = await httpClient.GetStringAsync("https://jsonplaceholder.typicode.com/comments?postId=1");
            Console.WriteLine($"  Response: {response2.Substring(0, Math.Min(100, response2.Length))}...");

            // POST 요청
            Console.WriteLine("  POST https://jsonplaceholder.typicode.com/posts");
            var content = new StringContent(
                """{"title": "foo", "body": "bar", "userId": 1}""",
                Encoding.UTF8,
                "application/json");
            var response3 = await httpClient.PostAsync("https://jsonplaceholder.typicode.com/posts", content);
            var body3 = await response3.Content.ReadAsStringAsync();
            Console.WriteLine(
                $"  Response: {response3.StatusCode} - {body3.Substring(0, Math.Min(100, body3.Length))}...");

            // 다중 GET 요청
            Console.WriteLine("  Making 5 more GET requests...");
            for (var i = 1; i <= 5; i++)
            {
                await httpClient.GetStringAsync($"https://jsonplaceholder.typicode.com/posts/{i}");
                Console.WriteLine($"    GET posts/{i} completed");
            }

            Console.WriteLine("  All requests completed! Check the DevTools Network tab.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error: {ex.Message}");
        }
    }

    private static async Task MakeFormRequests(HttpClient httpClient)
    {
        Console.WriteLine("Making form-encoded requests...");

        try
        {
            // application/x-www-form-urlencoded 요청
            Console.WriteLine("  POST (form-urlencoded) https://httpbin.org/post");
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("title", "foo"),
                new KeyValuePair<string, string>("body", "bar baz"),
                new KeyValuePair<string, string>("userId", "1"),
                new KeyValuePair<string, string>("tags[]", "csharp"),
                new KeyValuePair<string, string>("tags[]", "dotnet")
            });
            var response1 = await httpClient.PostAsync("https://httpbin.org/post", formContent);
            Console.WriteLine($"  Response: {response1.StatusCode}");

            // multipart/form-data 요청
            Console.WriteLine("  POST (multipart/form-data) https://httpbin.org/post");
            var multipartContent = new MultipartFormDataContent();
            multipartContent.Add(new StringContent("John Doe"), "username");
            multipartContent.Add(new StringContent("john@example.com"), "email");
            multipartContent.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("sample file content")), "attachment", "sample.txt");
            var response2 = await httpClient.PostAsync("https://httpbin.org/post", multipartContent);
            Console.WriteLine($"  Response: {response2.StatusCode}");

            Console.WriteLine("  Form requests completed! Check the DevTools Network tab.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error: {ex.Message}");
        }
    }

    private static void WriteLogMessages()
    {
        Console.WriteLine("Writing log messages...");

        Console.WriteLine("  [INFO] This is an information message");
        Console.WriteLine("  [DEBUG] This is a debug message with some data: { \"id\": 123 }");
        Console.WriteLine("  [WARNING] This is a warning message");
        Console.Error.WriteLine("  [ERROR] This is an error message");

        // 구조화된 출력 작성
        Console.WriteLine();
        Console.WriteLine("  Sample data:");
        Console.WriteLine("  {");
        Console.WriteLine("    \"name\": \"SharpInspect\",");
        Console.WriteLine("    \"version\": \"1.0.0\",");
        Console.WriteLine("    \"features\": [\"network\", \"console\", \"performance\"]");
        Console.WriteLine("  }");

        // 처리 시뮬레이션
        for (var i = 1; i <= 3; i++) Console.WriteLine($"  Processing step {i}/3...");

        Console.WriteLine("  Log messages written! Check the DevTools Console tab.");
    }
}
