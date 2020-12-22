using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ConsoleTest.Collections.Interfaces;

namespace ConsoleTest.Collections
{
    /// <summary>
    /// This is a base class that should be inherited by array-like classes
	/// that can have more than 2 billion items.
    /// </summary>
    public abstract class BigArray<T> :
        IBigIndexedCollection<T>,
        IAdvancedDisposable
    {
        internal static readonly IEqualityComparer<T> _comparer = EqualityComparer<T>.Default;

        internal BigArray()
        {
        }

        /// <summary>
        /// Releases all the resources (maybe files) used by this big array.
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// Gets a value indicating if this array was already disposed.
        /// </summary>
        public abstract bool WasDisposed { get; }

        internal long _length;
        /// <summary>
        /// Gets the total length of this array.
        /// </summary>
        public long Length
        {
            get
            {
                return _length;
            }
        }


        /// <summary>
        /// Must be overriden in a sub-class to tell if sorts should be
        /// in parallel or not. Memory arrays (or memory mapped arrays)
        /// benefit from parallel sort, but file persisted arrays don't.
        /// </summary>
        protected abstract bool UseParallelSort { get; }

        /// <summary>
        /// Gets or sets an item by index.
        /// </summary>
        public abstract T this[long index] { get; set; }

        /// <summary>
        /// Resizes this array.
        /// </summary>
        public abstract void Resize(long newLength);

        /// <summary>
        /// Creates a new bigarray of the same type of this one (Mmf or in memory BigArray)
        /// with the given length.
        /// </summary>
        public abstract BigArray<T> CreateNew(long length);

        /// <summary>
        /// Verifies if an item exists in this array.
        /// </summary>
        public bool Contains(T item)
        {
            return IndexOf(item) != -1;
        }

        /// <summary>
        /// Verifies if an item exists in this array.
        /// </summary>
        public bool Contains(T item, long startIndex, long count)
        {
            return IndexOf(item, startIndex, count) != -1;
        }

        /// <summary>
        /// Gets the 0-based index of an item in this array, or returns -1
        /// if it does not exist.
        /// </summary>
        public long IndexOf(T item)
        {
            return IndexOf(item, 0, _length);
        }

        /// <summary>
        /// Gets the index of an item in this array, or returns -1
        /// if it does not exist.
        /// </summary>
        public virtual long IndexOf(T item, long startIndex, long count)
        {
            if (startIndex < 0 || startIndex >= _length)
                throw new ArgumentOutOfRangeException("startIndex");

            if (count == 0)
                return -1;

            long end = startIndex + count;
            if (count < 0 || end > _length)
                throw new ArgumentOutOfRangeException("count");

            for (long i = startIndex; i < end; i++)
            {
                T otherItem = this[i];
                if (_comparer.Equals(item, otherItem))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Searches for an item considering this array is already sorted.
        /// If an item is not found, the position where it should be put is returned
        /// as a complement value (~position) and will be negative. If it is found,
        /// its 0-based index is returned.
        /// </summary>
        public long BinarySearch(T item)
        {
            return _BinarySearch(item, 0, _length, Comparer<T>.Default.Compare);
        }

        /// <summary>
        /// Searches for an item considering this array is already sorted.
        /// If an item is not found, the position where it should be put is returned
        /// as a complement value (~position) and will be negative. If it is found,
        /// its 0-based index is returned.
        /// </summary>
        public long BinarySearch(T item, Comparison<T> comparer)
        {
            if (comparer == null)
                comparer = Comparer<T>.Default.Compare;

            return _BinarySearch(item, 0, _length, comparer);
        }

        /// <summary>
        /// Searches for an item considering this array is already sorted.
        /// If an item is not found, the position where it should be put is returned
        /// as a complement value (~position) and will be negative. If it is found,
        /// its 0-based index is returned.
        /// </summary>
        public long BinarySearch(T item, long startIndex, long count, Comparison<T> comparer = null)
        {
            if (startIndex < 0 || startIndex > _length)
                throw new ArgumentOutOfRangeException("startIndex");

            if (count == 0)
                return ~startIndex;

            long end = startIndex + count;
            if (count < 0 || end > _length)
                throw new ArgumentOutOfRangeException("count");

            if (comparer == null)
                comparer = Comparer<T>.Default.Compare;

            return _BinarySearch(item, startIndex, count, comparer);
        }
        private long _BinarySearch(T item, long startIndex, long count, Comparison<T> comparer)
        {
            while (true)
            {
                if (count == 0)
                    return ~startIndex;

                long middle = count / 2;
                long position = startIndex + middle;
                int comparison = comparer(item, this[position]);
                if (comparison == 0)
                    return position;

                if (comparison < 0)
                {
                    count = middle;
                }
                else
                {
                    middle++;
                    startIndex += middle;
                    count -= middle;
                }
            }
        }

        /// <summary>
        /// Sorts this array.
        /// </summary>
        public void Sort(Comparison<T> comparer = null)
        {
            if (comparer == null)
                comparer = Comparer<T>.Default.Compare;

            _BigArrayParallelSort parallelSort = null;
            if (UseParallelSort)
                parallelSort = new _BigArrayParallelSort();

            _Sort(parallelSort, 0, _length, comparer);

            if (parallelSort != null)
                parallelSort.Wait();
        }

        /// <summary>
        /// Sorts this array.
        /// </summary>
        public void Sort(long startIndex, long count, Comparison<T> comparer)
        {
            if (startIndex < 0 || startIndex > _length)
                throw new ArgumentOutOfRangeException("startIndex");

            if (count == 0)
                return;

            long end = startIndex + count;
            if (count < 0 || end > _length)
                throw new ArgumentOutOfRangeException("count");

            if (comparer == null)
                comparer = Comparer<T>.Default.Compare;

            _BigArrayParallelSort parallelSort = null;
            if (UseParallelSort)
                parallelSort = new _BigArrayParallelSort();

            _Sort(parallelSort, startIndex, count, comparer);

            if (parallelSort != null)
                parallelSort.Wait();
        }
        private void _Sort(_BigArrayParallelSort parallelSort, long startIndex, long count, Comparison<T> comparer)
        {
            if (count <= 1)
                return;

            long pivotOffset = _Partition(startIndex, count, comparer);

            // if we don't have more than 10k items, we don't need to try to run in parallel.
            if (parallelSort == null || count < 10000)
                _Sort(parallelSort, startIndex, pivotOffset, comparer);
            else
            {
                // before putting another task to the threadpool, we verify if the amount of parallel
                // work is not exceeding the number of CPUs.
                // Even if the threadpool can be bigger than the number of CPUs, sorting is a no-wait
                // operation and so putting an extra work to do will only increase the number of task
                // switches.
                int parallelCount = Interlocked.Increment(ref _BigArrayParallelSort._parallelSortCount);
                if (parallelCount >= Environment.ProcessorCount)
                {
                    // we have too many threads in parallel
                    // (note that the first thread never stops, that's why I used >= operator).
                    Interlocked.Decrement(ref _BigArrayParallelSort._parallelSortCount);

                    // do a normal sub-sort.
                    _Sort(parallelSort, startIndex, pivotOffset, comparer);
                }
                else
                {
                    bool shouldProcessNormally = false;

                    // ok, we have the right to process in parallel, so let's start by saying we
                    // are processing in parallel.
                    Interlocked.Increment(ref parallelSort._executingCount);
                    try
                    {
                        ThreadPool.QueueUserWorkItem
                        (
                            (x) =>
                            {
                                // ok, finally we can sort. But, if an exception is thrown, we should redirect it to the
                                // main thread.
                                try
                                {
                                    _Sort(parallelSort, startIndex, pivotOffset, comparer);
                                }
                                catch (Exception exception)
                                {
                                    // here we store the exception.
                                    lock (parallelSort)
                                    {
                                        var exceptions = parallelSort._exceptions;
                                        if (exceptions == null)
                                        {
                                            exceptions = new List<Exception>();
                                            parallelSort._exceptions = exceptions;
                                        }

                                        exceptions.Add(exception);
                                    }
                                }
                                finally
                                {
                                    // Independent if we had an exception or not, we should decrement
                                    // both counters.
                                    Interlocked.Decrement(ref _BigArrayParallelSort._parallelSortCount);

                                    int parallelRemaining = Interlocked.Decrement(ref parallelSort._executingCount);

                                    // if we were the last parallel thread, we must notify the main thread if it is waiting
                                    // for us.
                                    if (parallelRemaining == 0)
                                        lock (parallelSort)
                                            Monitor.Pulse(parallelSort);
                                }
                            }
                        );
                    }
                    catch
                    {
                        // if an exception was thrown trying to call the thread pool, we simple reduce the
                        // count number and do the sort normally.
                        // The sort is out of the catch in case an Abort is done.
                        Interlocked.Decrement(ref parallelSort._executingCount);
                        Interlocked.Decrement(ref _BigArrayParallelSort._parallelSortCount);
                        shouldProcessNormally = true;
                    }

                    if (shouldProcessNormally)
                        _Sort(parallelSort, startIndex, pivotOffset, comparer);
                }
            }

            _Sort(parallelSort, startIndex + pivotOffset + 1, count - pivotOffset - 1, comparer);
        }
        private long _Partition(long startIndex, long count, Comparison<T> comparer)
        {
            long pivotIndex = startIndex + count / 2;
            T pivotValue = this[pivotIndex];

            long right = startIndex + count - 1;
            if (pivotIndex != right)
                this[pivotIndex] = this[right];

            long storeIndex = startIndex;
            for (long index = startIndex; index < right; index++)
            {
                T valueAtIndex = this[index];
                if (comparer(valueAtIndex, pivotValue) >= 0)
                    continue;

                if (index != storeIndex)
                {
                    this[index] = this[storeIndex];
                    this[storeIndex] = valueAtIndex;
                }

                storeIndex++;
            }

            if (right != storeIndex)
                this[right] = this[storeIndex];

            this[storeIndex] = pivotValue;

            return storeIndex - startIndex;
        }

        /// <summary>
        /// Swaps two items in this array.
        /// </summary>
        public void Swap(long position1, long position2)
        {
            if (position1 < 0 || position1 >= _length)
                throw new ArgumentOutOfRangeException("position1");

            if (position2 < 0 || position2 >= _length)
                throw new ArgumentOutOfRangeException("position2");

            if (position1 == position2)
                return;

            T value1 = this[position1];
            this[position1] = this[position2];
            this[position2] = value1;
        }

        /// <summary>
        /// Returns a read-only wrapper to access this big array.
        /// </summary>
        /// <returns></returns>
        public BigIndexedReadOnlyCollection<T> AsReadOnly()
        {
            return new BigIndexedReadOnlyCollection<T>(this);
        }

        /// <summary>
        /// Enumerates all items in this array.
        /// </summary>
        public virtual IEnumerator<T> GetEnumerator()
        {
            for (long i = 0; i < _length; i++)
                yield return this[i];
        }

        #region Private interface implementations
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
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

        long IBigIndexedReadOnlyCollection.Count
        {
            get
            {
                return Length;
            }
        }

        object IBigIndexedReadOnlyCollection.this[long index]
        {
            get
            {
                return this[index];
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
