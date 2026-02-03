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
    /// Extension methods for IServiceCollection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds SharpInspect services to the service collection.
        /// </summary>
        public static IServiceCollection AddSharpInspect(this IServiceCollection services)
        {
            return AddSharpInspect(services, new SharpInspectOptions());
        }

        /// <summary>
        /// Adds SharpInspect services to the service collection with configuration.
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
        /// Adds SharpInspect services to the service collection with options.
        /// </summary>
        public static IServiceCollection AddSharpInspect(
            this IServiceCollection services,
            SharpInspectOptions options)
        {
            if (options == null)
                options = new SharpInspectOptions();

            // Register core services
            services.AddSingleton(options);
            services.AddSingleton<EventBus>();
            services.AddSingleton<ISharpInspectStore>(sp =>
                new InMemoryStore(options.MaxNetworkEntries, options.MaxConsoleEntries));

            // Register server
            services.AddSingleton<ISharpInspectServer, HttpListenerServer>();

            // Register logger provider
            services.AddSingleton<ILoggerProvider, SharpInspectLoggerProvider>();

            return services;
        }

        /// <summary>
        /// Adds a named HttpClient with SharpInspect interception.
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