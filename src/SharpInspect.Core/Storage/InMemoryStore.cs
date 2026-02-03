using System;
using System.Collections.Generic;
using SharpInspect.Core.Models;

namespace SharpInspect.Core.Storage
{
    /// <summary>
    ///     링 버퍼를 사용하는 ISharpInspectStore의 인메모리 구현체.
    /// </summary>
    public class InMemoryStore : ISharpInspectStore
    {
        /// <summary>
        ///     콘솔 엔트리의 기본 용량.
        /// </summary>
        public const int DefaultConsoleCapacity = 5000;

        /// <summary>
        ///     네트워크 엔트리의 기본 용량.
        /// </summary>
        public const int DefaultNetworkCapacity = 1000;

        /// <summary>
        ///     성능 엔트리의 기본 용량.
        /// </summary>
        public const int DefaultPerformanceCapacity = 2000;

        private readonly Dictionary<string, NetworkEntry> _networkIndex;
        private readonly object _networkIndexLock = new();
        private readonly RingBuffer<ConsoleEntry> _consoleEntries;
        private readonly RingBuffer<NetworkEntry> _networkEntries;
        private readonly RingBuffer<PerformanceEntry> _performanceEntries;

        /// <summary>
        ///     기본 용량으로 새 인메모리 스토어를 생성합니다.
        /// </summary>
        public InMemoryStore()
            : this(DefaultNetworkCapacity, DefaultConsoleCapacity, DefaultPerformanceCapacity)
        {
        }

        /// <summary>
        ///     지정된 용량으로 새 인메모리 스토어를 생성합니다.
        /// </summary>
        public InMemoryStore(int networkCapacity, int consoleCapacity)
            : this(networkCapacity, consoleCapacity, DefaultPerformanceCapacity)
        {
        }

        /// <summary>
        ///     모든 엔트리 타입에 대해 지정된 용량으로 새 인메모리 스토어를 생성합니다.
        /// </summary>
        public InMemoryStore(int networkCapacity, int consoleCapacity, int performanceCapacity)
        {
            _networkEntries = new RingBuffer<NetworkEntry>(networkCapacity);
            _consoleEntries = new RingBuffer<ConsoleEntry>(consoleCapacity);
            _performanceEntries = new RingBuffer<PerformanceEntry>(performanceCapacity);
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
        public int PerformanceEntryCount => _performanceEntries.Count;

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
        public PerformanceEntry[] GetPerformanceEntries()
        {
            return _performanceEntries.GetAll();
        }

        /// <inheritdoc />
        public PerformanceEntry[] GetPerformanceEntries(int offset, int limit)
        {
            return _performanceEntries.GetRange(offset, limit);
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

                // 인덱스가 너무 커지면 오래된 엔트리 정리
                if (_networkIndex.Count > _networkEntries.Capacity * 2) RebuildNetworkIndex();
            }
        }

        /// <inheritdoc />
        public void AddPerformanceEntry(PerformanceEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException("entry");

            _performanceEntries.Add(entry);
        }

        /// <inheritdoc />
        public void ClearAll()
        {
            ClearNetworkEntries();
            ClearConsoleEntries();
            ClearPerformanceEntries();
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

        /// <inheritdoc />
        public void ClearPerformanceEntries()
        {
            _performanceEntries.Clear();
        }

        private void RebuildNetworkIndex()
        {
            _networkIndex.Clear();
            var entries = _networkEntries.GetAll();
            foreach (var entry in entries) _networkIndex[entry.Id] = entry;
        }
    }
}
