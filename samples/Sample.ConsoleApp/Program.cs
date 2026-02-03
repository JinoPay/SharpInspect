using System.Text;
using SharpInspect;

namespace Sample.ConsoleApp;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("=== SharpInspect Sample Console App ===");
        Console.WriteLine();

        // Initialize SharpInspect
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
        Console.WriteLine("Press 'l' to write log messages");
        Console.WriteLine("Press 'o' to open DevTools in browser");
        Console.WriteLine("Press 'q' to quit");
        Console.WriteLine();

        // Create HttpClient with SharpInspect interception
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
                    Console.WriteLine("Unknown command. Use 'r', 'l', 'o', or 'q'.");
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
            // GET request
            Console.WriteLine("  GET https://jsonplaceholder.typicode.com/posts/1");
            var response1 = await httpClient.GetStringAsync("https://jsonplaceholder.typicode.com/posts/1");
            Console.WriteLine($"  Response: {response1.Substring(0, Math.Min(100, response1.Length))}...");

            // GET request with query
            Console.WriteLine("  GET https://jsonplaceholder.typicode.com/comments?postId=1");
            var response2 = await httpClient.GetStringAsync("https://jsonplaceholder.typicode.com/comments?postId=1");
            Console.WriteLine($"  Response: {response2.Substring(0, Math.Min(100, response2.Length))}...");

            // POST request
            Console.WriteLine("  POST https://jsonplaceholder.typicode.com/posts");
            var content = new StringContent(
                """{"title": "foo", "body": "bar", "userId": 1}""",
                Encoding.UTF8,
                "application/json");
            var response3 = await httpClient.PostAsync("https://jsonplaceholder.typicode.com/posts", content);
            var body3 = await response3.Content.ReadAsStringAsync();
            Console.WriteLine(
                $"  Response: {response3.StatusCode} - {body3.Substring(0, Math.Min(100, body3.Length))}...");

            // Multiple GET requests
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

    private static void WriteLogMessages()
    {
        Console.WriteLine("Writing log messages...");

        Console.WriteLine("  [INFO] This is an information message");
        Console.WriteLine("  [DEBUG] This is a debug message with some data: { \"id\": 123 }");
        Console.WriteLine("  [WARNING] This is a warning message");
        Console.Error.WriteLine("  [ERROR] This is an error message");

        // Write some structured output
        Console.WriteLine();
        Console.WriteLine("  Sample data:");
        Console.WriteLine("  {");
        Console.WriteLine("    \"name\": \"SharpInspect\",");
        Console.WriteLine("    \"version\": \"1.0.0\",");
        Console.WriteLine("    \"features\": [\"network\", \"console\", \"performance\"]");
        Console.WriteLine("  }");

        // Simulate some processing
        for (var i = 1; i <= 3; i++) Console.WriteLine($"  Processing step {i}/3...");

        Console.WriteLine("  Log messages written! Check the DevTools Console tab.");
    }
}