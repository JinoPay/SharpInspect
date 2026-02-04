#if MODERN_DOTNET || NETSTANDARD2_0
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharpInspect.Core.Configuration;
using SharpInspect.Core.EnvironmentDetection;
using SharpInspect.Server.WebServer;

namespace SharpInspect.Extensions;

/// <summary>
///     Extension methods for IHostBuilder.
/// </summary>
public static class HostBuilderExtensions
{
    extension(IHostBuilder hostBuilder)
    {
        /// <summary>
        ///     Adds SharpInspect to the host builder with default options.
        /// </summary>
        public IHostBuilder UseSharpInspect()
        {
            return UseSharpInspect(hostBuilder, new SharpInspectOptions());
        }

        /// <summary>
        ///     Adds SharpInspect to the host builder with configuration.
        /// </summary>
        public IHostBuilder UseSharpInspect(Action<SharpInspectOptions> configure)
        {
            var options = new SharpInspectOptions();
            configure?.Invoke(options);
            return UseSharpInspect(hostBuilder, options);
        }

        /// <summary>
        ///     Adds SharpInspect to the host builder with options.
        ///     IHostEnvironment를 통해 환경을 감지하여 개발 환경에서만 활성화합니다.
        /// </summary>
        public IHostBuilder UseSharpInspect(SharpInspectOptions options)
        {
            return hostBuilder.ConfigureServices((context, services) =>
            {
                if (options == null)
                    options = new SharpInspectOptions();

                // IHostEnvironment를 통한 환경 감지 (Auto 모드일 때 우선 적용)
                if (options.EnableInDevelopmentOnly &&
                    options.DevelopmentDetectionMode == DevelopmentDetectionMode.Auto)
                {
                    // IHostEnvironment가 있으면 해당 정보 사용
                    if (!context.HostingEnvironment.IsDevelopment())
                        return;
                }
                else if (!DevelopmentEnvironmentDetector.IsDevelopment(options))
                {
                    // 다른 모드는 기존 로직 사용
                    return;
                }

                services.AddSharpInspect(options);

                // Add hosted service to start/stop the server with the host
                services.AddHostedService<SharpInspectHostedService>();
            });
        }
    }
}

/// <summary>
///     Hosted service that manages the SharpInspect server lifecycle.
/// </summary>
internal class SharpInspectHostedService(
    ISharpInspectServer server,
    SharpInspectOptions options)
    : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        server.Start();

        Console.WriteLine($"SharpInspect DevTools started at {options.GetDevToolsUrl()}");

        if (options.AutoOpenBrowser) OpenBrowser(options.GetDevToolsUrl());

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        server.Stop();
        return Task.CompletedTask;
    }

    private void OpenBrowser(string url)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(
                    OSPlatform.Windows))
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            else if (RuntimeInformation.IsOSPlatform(
                         OSPlatform.Linux))
                Process.Start("xdg-open", url);
            else if (RuntimeInformation.IsOSPlatform(
                         OSPlatform.OSX))
                Process.Start("open", url);
        }
        catch
        {
            // Ignore browser open failures
        }
    }
}

#endif