using SharpInspect.Core.Models;

namespace SharpInspect.Core.Storage
{
    /// <summary>
    ///     Interface for storing and retrieving captured data.
    /// </summary>
    public interface ISharpInspectStore
    {
        /// <summary>
        ///     Gets the count of console entries.
        /// </summary>
        int ConsoleEntryCount { get; }

        /// <summary>
        ///     Gets the count of network entries.
        /// </summary>
        int NetworkEntryCount { get; }

        /// <summary>
        ///     Gets all console entries.
        /// </summary>
        ConsoleEntry[] GetConsoleEntries();

        /// <summary>
        ///     Gets console entries with pagination.
        /// </summary>
        ConsoleEntry[] GetConsoleEntries(int offset, int limit);

        /// <summary>
        ///     Gets a specific network entry by ID.
        /// </summary>
        NetworkEntry GetNetworkEntry(string id);

        /// <summary>
        ///     Gets all network entries.
        /// </summary>
        NetworkEntry[] GetNetworkEntries();

        /// <summary>
        ///     Gets network entries with pagination.
        /// </summary>
        NetworkEntry[] GetNetworkEntries(int offset, int limit);

        /// <summary>
        ///     Adds a console entry to the store.
        /// </summary>
        void AddConsoleEntry(ConsoleEntry entry);

        /// <summary>
        ///     Adds a network entry to the store.
        /// </summary>
        void AddNetworkEntry(NetworkEntry entry);

        /// <summary>
        ///     Clears all stored data.
        /// </summary>
        void ClearAll();

        /// <summary>
        ///     Clears all console entries.
        /// </summary>
        void ClearConsoleEntries();

        /// <summary>
        ///     Clears all network entries.
        /// </summary>
        void ClearNetworkEntries();
    }
}