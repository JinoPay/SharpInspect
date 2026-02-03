# SharpInspect

**Chrome DevTools-style inspector for any .NET application**

Monitor HTTP requests, console logs, performance metrics, and application info in real-time.

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-3.5%20%7C%204.6.2%20%7C%206.0%20%7C%208.0%20%7C%209.0-purple.svg)

**English** | [한국어](README.ko.md)

## Features

- **Framework Agnostic**: Works with WinForms, WPF, Console, ASP.NET Core, and more
- **One-Line Setup**: Start with `SharpInspectDevTools.Initialize()`
- **Real-Time Monitoring**: WebSocket-powered live data streaming
- **Chrome DevTools UI**: Familiar, intuitive interface
- **Zero Dependencies**: No external NuGet packages required
- **Development-Only by Default**: Automatically disabled in production environments

## Supported Platforms

| Platform | Version |
|----------|---------|
| .NET Framework | 3.5+, 4.6.2+ |
| .NET | 6.0, 8.0, 9.0 |
| .NET Standard | 2.0 |

## Quick Start

### 1. Install

```bash
# NuGet (coming soon)
dotnet add package SharpInspect

# Or add project reference
```

### 2. Initialize

```csharp
using SharpInspect;

// Initialize at app startup
SharpInspectDevTools.Initialize();

// Or with options
SharpInspectDevTools.Initialize(options =>
{
    options.Port = 9229;
    options.AutoOpenBrowser = true;
});
```

### 3. Open in Browser

```
http://localhost:9229
```

## Usage Examples

### Console App

```csharp
using SharpInspect;

class Program
{
    static async Task Main()
    {
        SharpInspectDevTools.Initialize();

        // Create HttpClient with automatic capture
        using var client = SharpInspectDevTools.CreateHttpClient();

        // HTTP requests appear in DevTools Network tab
        var response = await client.GetStringAsync("https://api.example.com/data");

        // Console output appears in DevTools Console tab
        Console.WriteLine("Data received!");

        SharpInspectDevTools.Shutdown();
    }
}
```

### WinForms / WPF

```csharp
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        SharpInspectDevTools.Initialize();
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SharpInspectDevTools.Shutdown();
        base.OnExit(e);
    }
}
```

### Using Statement (Auto-Shutdown)

```csharp
using (new SharpInspectSession())
{
    // Your app logic
    // Automatically shuts down when block exits
}
```

## Configuration

```csharp
SharpInspectDevTools.Initialize(options =>
{
    // Server
    options.Port = 9229;
    options.Host = "localhost";
    options.AutoOpenBrowser = true;
    options.OpenInAppMode = true;  // Opens as standalone window (Chrome/Edge)

    // Capture toggles
    options.EnableNetworkCapture = true;
    options.EnableConsoleCapture = true;
    options.EnablePerformanceCapture = true;
    options.EnableApplicationCapture = true;

    // Storage limits (ring buffer)
    options.MaxNetworkEntries = 1000;
    options.MaxConsoleEntries = 5000;
    options.MaxPerformanceEntries = 2000;
    options.MaxBodySizeBytes = 1048576;  // 1MB

    // Development-only mode (enabled by default)
    options.EnableInDevelopmentOnly = true;
    options.DevelopmentDetectionMode = DevelopmentDetectionMode.Auto;

    // Security
    options.MaskedHeaders.Add("X-API-Key");
    options.AccessToken = "my-secret-token";
});
```

### Development Detection Modes

SharpInspect runs only in development environments by default:

```csharp
// Auto (default): Environment variable first, then debugger attached
options.DevelopmentDetectionMode = DevelopmentDetectionMode.Auto;

// Environment variable only: DOTNET_ENVIRONMENT or ASPNETCORE_ENVIRONMENT = "Development"
options.DevelopmentDetectionMode = DevelopmentDetectionMode.EnvironmentVariableOnly;

// Debugger only: Debugger.IsAttached
options.DevelopmentDetectionMode = DevelopmentDetectionMode.DebuggerOnly;

// Custom: Your own logic
options.DevelopmentDetectionMode = DevelopmentDetectionMode.Custom;
options.CustomDevelopmentCheck = () => MyConfig.IsDevMode;

// Force enable in all environments
options.EnableInDevelopmentOnly = false;
```

## DevTools UI Features

### Network Tab
- Request/response list with timing info
- Status code color coding (2xx green, 4xx orange, 5xx red)
- Headers and body inspection (JSON formatted)
- Timing breakdown (DNS, TCP, TLS, TTFB)
- Filtering and search
- Clear button

### Console Tab
- Log level color coding
- Real-time streaming
- Stack trace display for exceptions
- Filtering and search

### Performance Tab
- CPU usage monitoring
- Memory metrics (working set, GC heap)
- GC collection counts
- Thread count tracking

### Application Tab
- App info (name, version, runtime, PID)
- Environment variables
- Loaded assemblies list

## REST API

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/status` | GET | Server status |
| `/api/network` | GET | Network entries (paginated) |
| `/api/network/{id}` | GET | Single network entry |
| `/api/network/clear` | POST | Clear network logs |
| `/api/console` | GET | Console entries (paginated) |
| `/api/console/clear` | POST | Clear console logs |
| `/api/performance` | GET | Performance entries (paginated) |
| `/api/performance/clear` | POST | Clear performance logs |
| `/api/application` | GET | Application info |
| `/ws` | WebSocket | Real-time event stream |

## Project Structure

```
SharpInspect/
├── src/
│   ├── SharpInspect.Core/       # Core models, storage, events, interceptors
│   ├── SharpInspect.Server/     # Embedded web server (REST API, WebSocket)
│   └── SharpInspect/            # Public API, DI extensions
└── samples/
    ├── Sample.ConsoleApp/       # .NET 8 console example
    └── Sample.WinForms/         # .NET Framework 4.6.2 WinForms example
```

## Build

```bash
# Build all
dotnet build SharpInspect.sln

# Run sample
dotnet run --project samples/Sample.ConsoleApp
```

## Security Considerations

- Binds to `localhost` only by default
- Auto-disabled in production (EnableInDevelopmentOnly = true)
- Sensitive headers masked automatically (Authorization, Cookie)
- Optional token-based authentication

## Roadmap

### Completed
- [x] Network Tab (HTTP capture with timing)
- [x] Console Tab (log capture)
- [x] Performance Tab (CPU, memory, GC metrics)
- [x] Application Tab (app info, env vars, assemblies)
- [x] Real-time WebSocket streaming
- [x] Chrome DevTools-style UI
- [x] Development-only mode with multiple detection strategies
- [x] Multi-framework support (.NET Framework 3.5 ~ .NET 9.0)

### Planned
- [ ] HAR export
- [ ] Custom panel plugin system
- [ ] Request replay
- [ ] Performance timeline view
- [ ] Dark mode UI
- [ ] NuGet package release

## Contributing

Issues and PRs welcome!

## License

MIT License
