using System;

namespace SharpInspect.Core.Storage;

/// <summary>
///     용량에 도달하면 가장 오래된 항목을 덮어쓰는 스레드 안전 링 버퍼(순환 버퍼).
///     .NET Framework 3.5+ 호환.
/// </summary>
/// <typeparam name="T">버퍼 요소의 타입.</typeparam>
public class RingBuffer<T>
{
    private readonly object _lock = new();
    private readonly T[] _buffer;
    private int _count;
    private int _head;
    private int _tail;

    /// <summary>
    ///     지정된 용량으로 새 링 버퍼를 생성합니다.
    /// </summary>
    /// <param name="capacity">버퍼가 보관할 수 있는 최대 항목 수.</param>
    public RingBuffer(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");

        Capacity = capacity;
        _buffer = new T[capacity];
        _head = 0;
        _tail = 0;
        _count = 0;
    }

    /// <summary>
    ///     버퍼의 최대 용량을 가져옵니다.
    /// </summary>
    public int Capacity { get; }

    /// <summary>
    ///     버퍼의 현재 항목 수를 가져옵니다.
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
    ///     가장 최근에 추가된 항목을 가져오려고 시도합니다.
    /// </summary>
    /// <param name="item">사용 가능한 경우 가장 최근 항목.</param>
    /// <returns>항목을 찾으면 true, 버퍼가 비어있으면 false.</returns>
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
    ///     버퍼의 모든 항목을 가장 오래된 것부터 최신 순서로 배열로 반환합니다.
    /// </summary>
    /// <returns>버퍼의 모든 항목을 포함하는 배열.</returns>
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
    ///     지정된 오프셋부터 버퍼의 항목을 반환합니다.
    /// </summary>
    /// <param name="offset">가장 오래된 것부터 건너뛸 항목 수.</param>
    /// <param name="limit">반환할 최대 항목 수. 0이면 나머지 전부 반환.</param>
    /// <returns>요청된 항목을 포함하는 배열.</returns>
    public T[] GetRange(int offset, int limit)
    {
        lock (_lock)
        {
            if (offset >= _count)
                return [];

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
    ///     버퍼에 항목을 추가합니다. 버퍼가 가득 차면 가장 오래된 항목을 덮어씁니다.
    /// </summary>
    /// <param name="item">추가할 항목.</param>
    public void Add(T item)
    {
        lock (_lock)
        {
            _buffer[_tail] = item;
            _tail = (_tail + 1) % Capacity;

            if (_count == Capacity)
                // 버퍼가 가득 참, 헤드를 앞으로 이동 (가장 오래된 항목 덮어쓰기)
                _head = (_head + 1) % Capacity;
            else
                _count++;
        }
    }

    /// <summary>
    ///     버퍼의 모든 항목을 지웁니다.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            // GC 허용을 위해 참조 해제
            for (var i = 0; i < Capacity; i++) _buffer[i] = default;
            _head = 0;
            _tail = 0;
            _count = 0;
        }
    }
}