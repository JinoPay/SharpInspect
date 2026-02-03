#if MODERN_DOTNET || NETSTANDARD2_0
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharpInspect.Core.Configuration;
using SharpInspect.Core.EnvironmentDetection;
using SharpInspect.Server.WebServer;

namespace SharpInspect.Extensions
{
    /// <summary>
    /// Extension methods for IHostBuilder.
    /// </summary>
    public static class HostBuilderExtensions
    {
        /// <summary>
        /// Adds SharpInspect to the host builder with default options.
        /// </summary>
        public static IHostBuilder UseSharpInspect(this IHostBuilder hostBuilder)
        {
            return UseSharpInspect(hostBuilder, new SharpInspectOptions());
        }

        /// <summary>
        /// Adds SharpInspect to the host builder with configuration.
        /// </summary>
        public static IHostBuilder UseSharpInspect(
            this IHostBuilder hostBuilder,
            Action<SharpInspectOptions> configure)
        {
            var options = new SharpInspectOptions();
            configure?.Invoke(options);
            return UseSharpInspect(hostBuilder, options);
        }

        /// <summary>
        /// Adds SharpInspect to the host builder with options.
        /// IHostEnvironment를 통해 환경을 감지하여 개발 환경에서만 활성화합니다.
        /// </summary>
        public static IHostBuilder UseSharpInspect(
            this IHostBuilder hostBuilder,
            SharpInspectOptions options)
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

    /// <summary>
    /// Hosted service that manages the SharpInspect server lifecycle.
    /// </summary>
    internal class SharpInspectHostedService : IHostedService
    {
        private readonly ISharpInspectServer _server;
        private readonly SharpInspectOptions _options;

        public SharpInspectHostedService(
            ISharpInspectServer server,
            SharpInspectOptions options)
        {
            _server = server;
            _options = options;
        }

        public System.Threading.Tasks.Task StartAsync(System.Threading.CancellationToken cancellationToken)
        {
            _server.Start();

            Console.WriteLine($"SharpInspect DevTools started at {_options.GetDevToolsUrl()}");

            if (_options.AutoOpenBrowser)
            {
                OpenBrowser(_options.GetDevToolsUrl());
            }

            return System.Threading.Tasks.Task.CompletedTask;
        }

        public System.Threading.Tasks.Task StopAsync(System.Threading.CancellationToken cancellationToken)
        {
            _server.Stop();
            return System.Threading.Tasks.Task.CompletedTask;
        }

        private void OpenBrowser(string url)
        {
            try
            {
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                    System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                    System.Runtime.InteropServices.OSPlatform.Linux))
                {
                    System.Diagnostics.Process.Start("xdg-open", url);
                }
                else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                    System.Runtime.InteropServices.OSPlatform.OSX))
                {
                    System.Diagnostics.Process.Start("open", url);
                }
            }
            catch
            {
                // Ignore browser open failures
            }
        }
    }
}
#endif