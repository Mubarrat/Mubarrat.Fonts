using CommunityToolkit.HighPerformance;
using System.Collections;

namespace Mubarrat.Fonts.Binary;

/// <summary>
/// Provides extension methods for exposing <see cref="ReadOnlyMemory{T}"/>, <see cref="Memory{T}"/>, <see cref="ReadOnlyMemory2D{T}"/>, and <see cref="Memory2D{T}"/> as collection interfaces without copying their elements.
/// </summary>
/// <remarks>
/// The returned collections are views over the supplied memory, not independent copies. Changes made through other writable references to the underlying storage remain observable through these views. The collection interfaces do not expose the underlying memory's contiguity or dimensionality.
/// </remarks>
public static class MemoryExtensions
{
    /// <summary>
    /// Exposes a <see cref="ReadOnlyMemory{T}"/> as an <see cref="IReadOnlyList{T}"/> without copying its elements.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the memory.</typeparam>
    /// <param name="memory">The memory to expose.</param>
    /// <returns>A read-only list view over the supplied memory.</returns>
    public static IReadOnlyList<T> AsReadOnlyList<T>(this ReadOnlyMemory<T> memory) => new ReadOnlyMemoryList<T>(memory);

    /// <summary>
    /// Exposes a <see cref="Memory{T}"/> as an <see cref="IReadOnlyList{T}"/> without copying its elements.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the memory.</typeparam>
    /// <param name="memory">The memory to expose.</param>
    /// <returns>A read-only list view over the supplied memory.</returns>
    public static IReadOnlyList<T> AsReadOnlyList<T>(this Memory<T> memory) => new ReadOnlyMemoryList<T>(memory);

    /// <summary>
    /// Exposes a <see cref="Memory{T}"/> as an <see cref="IList{T}"/> without copying its elements.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the memory.</typeparam>
    /// <param name="memory">The memory to expose.</param>
    /// <returns>A writable list view over the supplied memory. Element values can be changed, but the collection's size cannot be changed.</returns>
    public static IList<T> AsList<T>(this Memory<T> memory) => new MemoryList<T>(memory);

    /// <summary>
    /// Exposes a <see cref="ReadOnlyMemory2D{T}"/> as a flattened <see cref="IReadOnlyList{T}"/> without copying its elements.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the memory.</typeparam>
    /// <param name="memory">The two-dimensional memory to expose.</param>
    /// <returns>A read-only list view enumerating the elements in row-major order.</returns>
    public static IReadOnlyList<T> AsReadOnlyList<T>(this ReadOnlyMemory2D<T> memory) => memory.TryGetMemory(out var singleMemory) ? singleMemory.AsReadOnlyList() : new ReadOnlyMemory2DList<T>(memory);

    /// <summary>
    /// Exposes a <see cref="Memory2D{T}"/> as a flattened <see cref="IReadOnlyList{T}"/> without copying its elements.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the memory.</typeparam>
    /// <param name="memory">The two-dimensional memory to expose.</param>
    /// <returns>A read-only list view enumerating the elements in row-major order.</returns>
    public static IReadOnlyList<T> AsReadOnlyList<T>(this Memory2D<T> memory) => memory.TryGetMemory(out var singleMemory) ? singleMemory.AsReadOnlyList() : new ReadOnlyMemory2DList<T>(memory);

    /// <summary>
    /// Exposes a <see cref="Memory2D{T}"/> as a flattened <see cref="IList{T}"/> without copying its elements.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the memory.</typeparam>
    /// <param name="memory">The two-dimensional memory to expose.</param>
    /// <returns>A writable list view enumerating the elements in row-major order. Element values can be changed, but the collection's size cannot be changed.</returns>
    public static IList<T> AsList<T>(this Memory2D<T> memory) => memory.TryGetMemory(out var singleMemory) ? singleMemory.AsList() : new Memory2DList<T>(memory);

    private sealed class ReadOnlyMemoryList<T>(ReadOnlyMemory<T> memory) : IReadOnlyList<T>
    {
        public int Count => memory.Length;
        public T this[int index] => (uint)index < (uint)Count ? memory.Span[index] : throw new ArgumentOutOfRangeException(nameof(index));
        public IEnumerator<T> GetEnumerator() => new Enumerator(memory);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public sealed class Enumerator(ReadOnlyMemory<T> memory) : IEnumerator<T>
        {
            private int _index = -1;
            public T Current => (uint)_index < (uint)memory.Length ? memory.Span[_index] : throw new InvalidOperationException();
            object IEnumerator.Current => Current!;
            public bool MoveNext()
            {
                if (_index < memory.Length - 1)
                {
                    _index++;
                    return true;
                }
                _index = memory.Length;
                return false;
            }
            public void Reset() => _index = -1;
            public void Dispose() { }
        }
    }

    private sealed class ReadOnlyMemory2DList<T>(ReadOnlyMemory2D<T> memory) : IReadOnlyList<T>
    {
        public int Count => (int)memory.Length;
        public T this[int index] => (uint)index < (uint)Count ? memory.Span[index / memory.Width, index % memory.Width] : throw new ArgumentOutOfRangeException(nameof(index));
        public IEnumerator<T> GetEnumerator() => new Enumerator(memory);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public sealed class Enumerator(ReadOnlyMemory2D<T> memory) : IEnumerator<T>
        {
            private int _index = -1;
            public T Current => (uint)_index < (uint)memory.Length ? memory.Span[_index / memory.Width, _index % memory.Width] : throw new InvalidOperationException();
            object IEnumerator.Current => Current!;
            public bool MoveNext()
            {
                if (_index < memory.Length - 1)
                {
                    _index++;
                    return true;
                }
                _index = (int)memory.Length;
                return false;
            }
            public void Reset() => _index = -1;
            public void Dispose() { }
        }
    }

    private sealed class MemoryList<T>(Memory<T> memory) : IList<T>, IReadOnlyList<T>
    {
        public int Count => memory.Length;
        public bool IsReadOnly => false;
        public T this[int index]
        {
            get => (uint)index < (uint)Count ? memory.Span[index] : throw new ArgumentOutOfRangeException(nameof(index));
            set
            {
                if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
                memory.Span[index] = value;
            }
        }

        public int IndexOf(T item) => memory.Span.IndexOf(ref item);
        public bool Contains(T item) => IndexOf(item) >= 0;
        public void CopyTo(T[] array, int arrayIndex) => memory.Span.CopyTo(array.AsSpan(arrayIndex));
        public IEnumerator<T> GetEnumerator() => new Enumerator(memory);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Add(T item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public void Insert(int index, T item) => throw new NotSupportedException();
        public bool Remove(T item) => throw new NotSupportedException();
        public void RemoveAt(int index) => throw new NotSupportedException();

        public sealed class Enumerator(Memory<T> memory) : IEnumerator<T>
        {
            private int _index = -1;
            public T Current => (uint)_index < (uint)memory.Length ? memory.Span[_index] : throw new InvalidOperationException();
            object IEnumerator.Current => Current!;
            public bool MoveNext()
            {
                if (_index < memory.Length - 1)
                {
                    _index++;
                    return true;
                }
                _index = memory.Length;
                return false;
            }
            public void Reset() => _index = -1;
            public void Dispose() { }
        }
    }

    private sealed class Memory2DList<T>(Memory2D<T> memory) : IList<T>, IReadOnlyList<T>
    {
        public int Count => checked((int)memory.Length);
        public bool IsReadOnly => false;
        public T this[int index]
        {
            get => (uint)index < (uint)Count ? memory.Span[index / memory.Width, index % memory.Width] : throw new ArgumentOutOfRangeException(nameof(index));
            set
            {
                if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
                memory.Span[index / memory.Width, index % memory.Width] = value;
            }
        }

        public int IndexOf(T item)
        {
            for (int i = 0; i < Count; i++)
                if (EqualityComparer<T>.Default.Equals(this[i], item)) return i;
            return -1;
        }

        public bool Contains(T item) => IndexOf(item) >= 0;

        public void CopyTo(T[] array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);
            ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
            if (arrayIndex > array.Length || Count > array.Length - arrayIndex)
                throw new ArgumentException("The destination array is too small.", nameof(array));

            for (int i = 0; i < Count; i++)
                array[arrayIndex + i] = this[i];
        }

        public IEnumerator<T> GetEnumerator() => new Enumerator(memory);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Add(T item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public void Insert(int index, T item) => throw new NotSupportedException();
        public bool Remove(T item) => throw new NotSupportedException();
        public void RemoveAt(int index) => throw new NotSupportedException();

        public sealed class Enumerator(Memory2D<T> memory) : IEnumerator<T>
        {
            private int _index = -1;
            public T Current => (uint)_index < (uint)memory.Length ? memory.Span[_index / memory.Width, _index % memory.Width] : throw new InvalidOperationException();
            object IEnumerator.Current => Current!;
            public bool MoveNext()
            {
                if (_index < memory.Length - 1)
                {
                    _index++;
                    return true;
                }
                _index = (int)memory.Length;
                return false;
            }
            public void Reset() => _index = -1;
            public void Dispose() { }
        }
    }
}
