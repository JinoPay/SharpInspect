using System;
using System.Collections.Generic;
using SharpInspect.Core.Models;

namespace SharpInspect.Core.Storage
{
    /// <summary>
    ///     In-memory implementation of ISharpInspectStore using ring buffers.
    /// </summary>
    public class InMemoryStore : ISharpInspectStore
    {
        /// <summary>
        ///     Default capacity for console entries.
        /// </summary>
        public const int DefaultConsoleCapacity = 5000;

        /// <summary>
        ///     Default capacity for network entries.
        /// </summary>
        public const int DefaultNetworkCapacity = 1000;

        private readonly Dictionary<string, NetworkEntry> _networkIndex;
        private readonly object _networkIndexLock = new();
        private readonly RingBuffer<ConsoleEntry> _consoleEntries;
        private readonly RingBuffer<NetworkEntry> _networkEntries;

        /// <summary>
        ///     Creates a new in-memory store with default capacities.
        /// </summary>
        public InMemoryStore()
            : this(DefaultNetworkCapacity, DefaultConsoleCapacity)
        {
        }

        /// <summary>
        ///     Creates a new in-memory store with specified capacities.
        /// </summary>
        public InMemoryStore(int networkCapacity, int consoleCapacity)
        {
            _networkEntries = new RingBuffer<NetworkEntry>(networkCapacity);
            _consoleEntries = new RingBuffer<ConsoleEntry>(consoleCapacity);
            _networkIndex = new Dictionary<string, NetworkEntry>();
        }

        /// <inheritdoc />
        public ConsoleEntry[] GetConsoleEntries()
        {
            return _consoleEntries.GetAll();
        }

        /// <inheritdoc />
        public ConsoleEntry[] GetConsoleEntries(int offset, int limit)
        {
            return _consoleEntries.GetRange(offset, limit);
        }

        /// <inheritdoc />
        public int ConsoleEntryCount => _consoleEntries.Count;

        /// <inheritdoc />
        public int NetworkEntryCount => _networkEntries.Count;

        /// <inheritdoc />
        public NetworkEntry GetNetworkEntry(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            lock (_networkIndexLock)
            {
                NetworkEntry entry;
                if (_networkIndex.TryGetValue(id, out entry)) return entry;
                return null;
            }
        }

        /// <inheritdoc />
        public NetworkEntry[] GetNetworkEntries()
        {
            return _networkEntries.GetAll();
        }

        /// <inheritdoc />
        public NetworkEntry[] GetNetworkEntries(int offset, int limit)
        {
            return _networkEntries.GetRange(offset, limit);
        }

        /// <inheritdoc />
        public void AddConsoleEntry(ConsoleEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException("entry");

            _consoleEntries.Add(entry);
        }

        /// <inheritdoc />
        public void AddNetworkEntry(NetworkEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException("entry");

            _networkEntries.Add(entry);

            lock (_networkIndexLock)
            {
                _networkIndex[entry.Id] = entry;

                // Clean up old entries from index if it gets too large
                if (_networkIndex.Count > _networkEntries.Capacity * 2) RebuildNetworkIndex();
            }
        }

        /// <inheritdoc />
        public void ClearAll()
        {
            ClearNetworkEntries();
            ClearConsoleEntries();
        }

        /// <inheritdoc />
        public void ClearConsoleEntries()
        {
            _consoleEntries.Clear();
        }

        /// <inheritdoc />
        public void ClearNetworkEntries()
        {
            _networkEntries.Clear();
            lock (_networkIndexLock)
            {
                _networkIndex.Clear();
            }
        }

        private void RebuildNetworkIndex()
        {
            _networkIndex.Clear();
            var entries = _networkEntries.GetAll();
            foreach (var entry in entries) _networkIndex[entry.Id] = entry;
        }
    }
}