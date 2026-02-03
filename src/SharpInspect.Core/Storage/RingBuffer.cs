using System;

namespace SharpInspect.Core.Storage
{
    /// <summary>
    ///     A thread-safe ring buffer (circular buffer) that overwrites oldest items when capacity is reached.
    ///     Compatible with .NET Framework 3.5+.
    /// </summary>
    /// <typeparam name="T">The type of elements in the buffer.</typeparam>
    public class RingBuffer<T>
    {
        private readonly object _lock = new();
        private readonly T[] _buffer;
        private int _count;
        private int _head;
        private int _tail;

        /// <summary>
        ///     Creates a new ring buffer with the specified capacity.
        /// </summary>
        /// <param name="capacity">Maximum number of items the buffer can hold.</param>
        public RingBuffer(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException("capacity", "Capacity must be greater than zero.");

            Capacity = capacity;
            _buffer = new T[capacity];
            _head = 0;
            _tail = 0;
            _count = 0;
        }

        /// <summary>
        ///     Gets the maximum capacity of the buffer.
        /// </summary>
        public int Capacity { get; }

        /// <summary>
        ///     Gets the current number of items in the buffer.
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _count;
                }
            }
        }

        /// <summary>
        ///     Tries to get the most recently added item.
        /// </summary>
        /// <param name="item">The most recent item if available.</param>
        /// <returns>True if an item was found, false if the buffer is empty.</returns>
        public bool TryGetLatest(out T item)
        {
            lock (_lock)
            {
                if (_count == 0)
                {
                    item = default;
                    return false;
                }

                var lastIndex = (_tail - 1 + Capacity) % Capacity;
                item = _buffer[lastIndex];
                return true;
            }
        }

        /// <summary>
        ///     Returns all items in the buffer as an array, ordered from oldest to newest.
        /// </summary>
        /// <returns>An array containing all items in the buffer.</returns>
        public T[] GetAll()
        {
            lock (_lock)
            {
                var result = new T[_count];
                if (_count == 0)
                    return result;

                var index = 0;
                var current = _head;
                for (var i = 0; i < _count; i++)
                {
                    result[index++] = _buffer[current];
                    current = (current + 1) % Capacity;
                }

                return result;
            }
        }

        /// <summary>
        ///     Returns items from the buffer starting at the specified offset.
        /// </summary>
        /// <param name="offset">Number of items to skip from the oldest.</param>
        /// <param name="limit">Maximum number of items to return. If 0, returns all remaining items.</param>
        /// <returns>An array containing the requested items.</returns>
        public T[] GetRange(int offset, int limit)
        {
            lock (_lock)
            {
                if (offset >= _count)
                    return new T[0];

                var available = _count - offset;
                var take = limit <= 0 || limit > available ? available : limit;

                var result = new T[take];
                var current = (_head + offset) % Capacity;

                for (var i = 0; i < take; i++)
                {
                    result[i] = _buffer[current];
                    current = (current + 1) % Capacity;
                }

                return result;
            }
        }

        /// <summary>
        ///     Adds an item to the buffer. If the buffer is full, the oldest item is overwritten.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Add(T item)
        {
            lock (_lock)
            {
                _buffer[_tail] = item;
                _tail = (_tail + 1) % Capacity;

                if (_count == Capacity)
                    // Buffer is full, move head forward (overwrite oldest)
                    _head = (_head + 1) % Capacity;
                else
                    _count++;
            }
        }

        /// <summary>
        ///     Clears all items from the buffer.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                // Clear references to allow GC
                for (var i = 0; i < Capacity; i++) _buffer[i] = default;
                _head = 0;
                _tail = 0;
                _count = 0;
            }
        }
    }
}