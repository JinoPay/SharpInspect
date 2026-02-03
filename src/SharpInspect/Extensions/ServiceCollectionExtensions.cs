#if MODERN_DOTNET || NETSTANDARD2_0
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpInspect.Core.Configuration;
using SharpInspect.Core.Events;
using SharpInspect.Core.Interceptors;
using SharpInspect.Core.Logging;
using SharpInspect.Core.Storage;
using SharpInspect.Server.WebServer;

namespace SharpInspect.Extensions
{
    /// <summary>
    /// IServiceCollection 확장 메서드.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 서비스 컬렉션에 SharpInspect 서비스를 추가합니다.
        /// </summary>
        public static IServiceCollection AddSharpInspect(this IServiceCollection services)
        {
            return AddSharpInspect(services, new SharpInspectOptions());
        }

        /// <summary>
        /// 설정과 함께 서비스 컬렉션에 SharpInspect 서비스를 추가합니다.
        /// </summary>
        public static IServiceCollection AddSharpInspect(
            this IServiceCollection services,
            Action<SharpInspectOptions> configure)
        {
            var options = new SharpInspectOptions();
            configure?.Invoke(options);
            return AddSharpInspect(services, options);
        }

        /// <summary>
        /// 옵션과 함께 서비스 컬렉션에 SharpInspect 서비스를 추가합니다.
        /// </summary>
        public static IServiceCollection AddSharpInspect(
            this IServiceCollection services,
            SharpInspectOptions options)
        {
            if (options == null)
                options = new SharpInspectOptions();

            // 핵심 서비스 등록
            services.AddSingleton(options);
            services.AddSingleton<EventBus>();
            services.AddSingleton<ISharpInspectStore>(sp =>
                new InMemoryStore(options.MaxNetworkEntries, options.MaxConsoleEntries, options.MaxPerformanceEntries));

            // 서버 등록
            services.AddSingleton<ISharpInspectServer, HttpListenerServer>();

            // 로거 프로바이더 등록
            services.AddSingleton<ILoggerProvider, SharpInspectLoggerProvider>();

            return services;
        }

        /// <summary>
        /// SharpInspect 인터셉션이 포함된 명명된 HttpClient를 추가합니다.
        /// </summary>
        public static IHttpClientBuilder AddSharpInspectHttpClient(
            this IServiceCollection services,
            string name)
        {
            return services.AddHttpClient(name)
                .AddHttpMessageHandler(sp =>
                {
                    var store = sp.GetRequiredService<ISharpInspectStore>();
                    var options = sp.GetRequiredService<SharpInspectOptions>();
                    var eventBus = sp.GetRequiredService<EventBus>();
                    return new SharpInspectHandler(store, options, eventBus);
                });
        }
    }
}
#endif
