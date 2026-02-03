using System;
using System.Collections.Generic;

namespace SharpInspect.Core.Storage
{
    /// <summary>
    /// A thread-safe ring buffer (circular buffer) that overwrites oldest items when capacity is reached.
    /// Compatible with .NET Framework 3.5+.
    /// </summary>
    /// <typeparam name="T">The type of elements in the buffer.</typeparam>
    public class RingBuffer<T>
    {
        private readonly T[] _buffer;
        private readonly object _lock = new object();
        private readonly int _capacity;
        private int _head;
        private int _tail;
        private int _count;

        /// <summary>
        /// Gets the maximum capacity of the buffer.
        /// </summary>
        public int Capacity
        {
            get { return _capacity; }
        }

        /// <summary>
        /// Gets the current number of items in the buffer.
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
        /// Creates a new ring buffer with the specified capacity.
        /// </summary>
        /// <param name="capacity">Maximum number of items the buffer can hold.</param>
        public RingBuffer(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException("capacity", "Capacity must be greater than zero.");

            _capacity = capacity;
            _buffer = new T[capacity];
            _head = 0;
            _tail = 0;
            _count = 0;
        }

        /// <summary>
        /// Adds an item to the buffer. If the buffer is full, the oldest item is overwritten.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Add(T item)
        {
            lock (_lock)
            {
                _buffer[_tail] = item;
                _tail = (_tail + 1) % _capacity;

                if (_count == _capacity)
                {
                    // Buffer is full, move head forward (overwrite oldest)
                    _head = (_head + 1) % _capacity;
                }
                else
                {
                    _count++;
                }
            }
        }

        /// <summary>
        /// Returns all items in the buffer as an array, ordered from oldest to newest.
        /// </summary>
        /// <returns>An array containing all items in the buffer.</returns>
        public T[] GetAll()
        {
            lock (_lock)
            {
                var result = new T[_count];
                if (_count == 0)
                    return result;

                int index = 0;
                int current = _head;
                for (int i = 0; i < _count; i++)
                {
                    result[index++] = _buffer[current];
                    current = (current + 1) % _capacity;
                }

                return result;
            }
        }

        /// <summary>
        /// Returns items from the buffer starting at the specified offset.
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

                int available = _count - offset;
                int take = (limit <= 0 || limit > available) ? available : limit;

                var result = new T[take];
                int current = (_head + offset) % _capacity;

                for (int i = 0; i < take; i++)
                {
                    result[i] = _buffer[current];
                    current = (current + 1) % _capacity;
                }

                return result;
            }
        }

        /// <summary>
        /// Clears all items from the buffer.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                // Clear references to allow GC
                for (int i = 0; i < _capacity; i++)
                {
                    _buffer[i] = default(T);
                }
                _head = 0;
                _tail = 0;
                _count = 0;
            }
        }

        /// <summary>
        /// Tries to get the most recently added item.
        /// </summary>
        /// <param name="item">The most recent item if available.</param>
        /// <returns>True if an item was found, false if the buffer is empty.</returns>
        public bool TryGetLatest(out T item)
        {
            lock (_lock)
            {
                if (_count == 0)
                {
                    item = default(T);
                    return false;
                }

                int lastIndex = (_tail - 1 + _capacity) % _capacity;
                item = _buffer[lastIndex];
                return true;
            }
        }
    }
}
