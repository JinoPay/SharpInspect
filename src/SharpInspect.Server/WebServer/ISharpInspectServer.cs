using System;

namespace SharpInspect.Server.WebServer
{
    /// <summary>
    /// Interface for the embedded SharpInspect web server.
    /// </summary>
    public interface ISharpInspectServer : IDisposable
    {
        /// <summary>
        /// Gets whether the server is currently running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Gets the base URL of the server.
        /// </summary>
        string BaseUrl { get; }

        /// <summary>
        /// Starts the server.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the server.
        /// </summary>
        void Stop();
    }
}
