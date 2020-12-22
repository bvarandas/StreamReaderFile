using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleTest.Collections.Interfaces;

namespace ConsoleTest.Collections
{

    public sealed class BigList<T> :
        IBigIndexedCollection<T>
    {
        private static readonly IEqualityComparer<T> _comparer = EqualityComparer<T>.Default;
        private BigArray<T> _array;

        /// <summary>
        /// Creates a new biglist with its default capacity.
        /// </summary>
        public BigList() :
            this(InMemoryBigArray.DefaultBlockLength)
        {
        }

        /// <summary>
        /// Creates a new biglist with the given initial capacity.
        /// </summary>
        public BigList(long capacity)
        {
            _array = new InMemoryBigArray<T>(capacity);
        }

        /// <summary>
        /// Creates a new biglist which will use a MemoryMappedFile as
        /// its storage.
        /// </summary>
        [CLSCompliant(false)]
        public BigList(long capacity, int itemLength, MmfReadDelegate<T> readItem, MmfWriteDelegate<T> writeItem)
        {
            _array = new MmfArray<T>(capacity, itemLength, readItem, writeItem);
        }

        /// <summary>
        /// Creates a new biglist which will use a RandomAccessFile as
        /// its storage.
        /// </summary>
        public BigList(long capacity, int itemLength, RafReadDelegate<T> readItem, RafWriteDelegate<T> writeItem)
        {
            _array = new RafArray<T>(capacity, itemLength, readItem, writeItem);
        }

        /// <summary>
        /// Releases all the resources (maybe files) used by this big list.
        /// </summary>
        public void Dispose()
        {
            Disposer.Dispose(ref _array);
        }

        /// <summary>
        /// Gets a value indicating if this list was already disposed.
        /// </summary>
        public bool WasDisposed
        {
            get
            {
                return _array == null;
            }
        }

        private long _count;
        /// <summary>
        /// Gets the number of items in this list.
        /// </summary>
        public long Count
        {
            get
            {
                return _count;
            }
        }

        /// <summary>
        /// Gets the number of items this list supports before doing
        /// new allocations.
        /// </summary>
        public long Capacity
        {
            get
            {
                return _array.Length;
            }
            set
            {
                if (value < _count)
                    throw new InvalidOperationException("The new capacity can't be lower than the actual Count.");

                _array.Resize(value);
            }
        }

        /// <summary>
        /// Gets or sets an item by its index.
        /// </summary>
        public T this[long index]
        {
            get
            {
                if (index < 0 || index >= _count)
                    throw new ArgumentOutOfRangeException("index");

                return _array[index];
            }
            set
            {
                if (index < 0 || index >= _count)
                    throw new ArgumentOutOfRangeException("index");

                _array[index] = value;
            }
        }

        /// <summary>
        /// Removes all items from this list.
        /// </summary>
        public void Clear()
        {
            if (!typeof(T).IsPrimitive)
                for (int i = 0; i < _count; i++)
                    _array[i] = default(T);

            _count = 0;
        }

        /// <summary>
        /// Sets the capacity to be the same as the Count of this list.
        /// </summary>
        public void TrimExcess()
        {
            _array.Resize(_count);
        }

        /// <summary>
        /// Searches for an item considering this array is already sorted.
        /// If an item is not found, the position where it should be put is returned
        /// as a complement value (~position) and will be negative. If it is found,
        /// its 0-based index is returned.
        /// </summary>
        public long BinarySearch(T item)
        {
            return _array.BinarySearch(item, 0, _count, Comparer<T>.Default.Compare);
        }

        /// <summary>
        /// Searches for an item considering this array is already sorted.
        /// If an item is not found, the position where it should be put is returned
        /// as a complement value (~position) and will be negative. If it is found,
        /// its 0-based index is returned.
        /// </summary>
        public long BinarySearch(T item, Comparison<T> comparer)
        {
            return _array.BinarySearch(item, 0, _count, comparer);
        }

        /// <summary>
        /// Searches for an item considering this array is already sorted.
        /// If an item is not found, the position where it should be put is returned
        /// as a complement value (~position) and will be negative. If it is found,
        /// its 0-based index is returned.
        /// </summary>
        public long BinarySearch(T item, long startIndex, long count, Comparison<T> comparer = null)
        {
            if (startIndex + count > _count)
                throw new ArgumentOutOfRangeException("count");

            // the other checks are already done by the array.
            return _array.BinarySearch(item, startIndex, count, comparer);
        }

        /// <summary>
        /// Sorts this list.
        /// </summary>
        public void Sort(Comparison<T> comparer = null)
        {
            _array.Sort(0, _count, comparer);
        }

        /// <summary>
        /// Sort a part of this list.
        /// </summary>
        public void Sort(long startIndex, long count, Comparison<T> comparer)
        {
            if (startIndex + count > _count)
                throw new ArgumentOutOfRangeException("count");

            // the other checks are already done by the array.
            _array.Sort(startIndex, count, comparer);
        }

        /// <summary>
        /// Adds an item to this list.
        /// </summary>
        public void Add(T item)
        {
            if (_count == _array.Length)
                _array.Resize(_count * 2);

            _array[_count] = item;
            _count++;
        }

        /// <summary>
        /// Gets the index of an item in this list, or returns
        /// -1 if it is not on the list.
        /// </summary>
        public long IndexOf(T item)
        {
            return _array.IndexOf(item, 0, _count);
        }

        /// <summary>
        /// Tries to find and remove an item from this list.
        /// Returns true if the item was found an removed, false if it was not
        /// in the list.
        /// </summary>
        public bool Remove(T item)
        {
            long index = IndexOf(item);
            if (index == -1)
                return false;

            RemoveAt(index);
            return true;
        }

        /// <summary>
        /// Removes an item at an specified position.
        /// </summary>
        public void RemoveAt(long index)
        {
            RemoveRange(index, 1);
        }

        /// <summary>
        /// Removes many items at once.
        /// </summary>
        public void RemoveRange(long startIndex, long count)
        {
            if (startIndex < 0 || startIndex > _count)
                throw new ArgumentOutOfRangeException("startIndex");

            if (count == 0)
                return;

            long end = startIndex + count;
            if (count < 0 || end > _count)
                throw new ArgumentOutOfRangeException("count");

            long destinationPosition = startIndex;
            long sourcePosition = end;
            while (destinationPosition < end)
            {
                _array[destinationPosition] = _array[sourcePosition];
                destinationPosition++;
                sourcePosition++;
            }

            if (!typeof(T).IsPrimitive)
            {
                long clearEnd = destinationPosition + count;
                while (destinationPosition < clearEnd)
                {
                    _array[destinationPosition] = default(T);
                    destinationPosition++;
                }
            }
        }

        /// <summary>
        /// Enumerates all items in this list.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            long count = _count;
            for (long i = 0; i < count; i++)
                yield return _array[i];
        }

        /// <summary>
        /// Returns a read-only wrapper of this list.
        /// </summary>
        public BigIndexedReadOnlyCollection<T> AsReadOnly()
        {
            return new BigIndexedReadOnlyCollection<T>(this);
        }


        #region Private Interface Implementations
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        object IBigIndexedReadOnlyCollection.this[long index]
        {
            get
            {
                return this[index];
            }
        }

        object IBigIndexedCollection.this[long index]
        {
            get
            {
                return this[index];
            }
            set
            {
                this[index] = (T)value;
            }
        }

        IBigIndexedReadOnlyCollection IBigIndexedCollection.AsReadOnly()
        {
            return AsReadOnly();
        }

        IBigIndexedReadOnlyCollection<T> IBigIndexedCollection<T>.AsReadOnly()
        {
            return AsReadOnly();
        }
        #endregion
    }
}