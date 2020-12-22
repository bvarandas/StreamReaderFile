using ConsoleTest.Collections;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ConsoleTest.Indexing
{
    /// <summary>
    /// This class is an ordered index that can be added to an Indexer object.
    /// As an ordered index, it allows you to find items already sorted or simple
    /// to search items with a key greater than another key, less than another key
    /// or similar.
    /// </summary>
    public sealed class OrderedIndex<TKey, TItem> :
        AbstractIndex<TKey, TItem>,
        IOrderedIndex<TKey, TItem>
    {
        #region Private nested class - _CompareKey
        private sealed class _CompareKey :
            IComparer<KeyValuePair<TKey, TItem>>
        {
            private readonly Comparison<TKey> _comparer;
            internal _CompareKey(Comparison<TKey> comparer)
            {
                _comparer = comparer;
            }

            public int Compare(KeyValuePair<TKey, TItem> x, KeyValuePair<TKey, TItem> y)
            {
                return _comparer(x.Key, y.Key);
            }
        }
        #endregion
        #region Private field - _items
        private BigArray<long> _items;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates an index giving it a name and the delegate to extract the key from an item.
        /// </summary>
        public OrderedIndex(string name, Func<TItem, TKey> extractKey, StorageMode storageMode) :
            this(name, extractKey, storageMode, null)
        {
        }

        /// <summary>
        /// Creates an index giving it a name, the delegate to extract the key from an item
        /// and also the comparer to use to compare the keys.
        /// </summary>
        public OrderedIndex(string name, Func<TItem, TKey> extractKey, StorageMode storageMode, Comparison<TKey> comparer) :
            base(name, extractKey, storageMode)
        {
            if (comparer == null)
                comparer = Comparer<TKey>.Default.Compare;

            _comparer = comparer;
            _comparer2 = (x, y) => _comparer(_GetKey(x), _GetKey(y));
        }
        #endregion
        #region Dispose
        /// <summary>
        /// Releases the items arrays and deletes files if necessary.
        /// </summary>
        protected override void OnDispose()
        {
            Disposer.Dispose(ref _items);
        }
        #endregion

        #region Properties
        #region this[]
        /// <summary>
        /// Gets an item by its ordered position.
        /// </summary>
        public TItem this[long position]
        {
            get
            {
                if (_items == null)
                    throw new ArgumentOutOfRangeException("position");

                long indexInItemsArray = _items[position];
                return _indexer._items[indexInItemsArray];
            }
        }
        #endregion

        #region Comparer
        private readonly Comparison<long> _comparer2;
        private readonly Comparison<TKey> _comparer;
        /// <summary>
        /// Gets the comparer used to compare keys.
        /// </summary>
        public Comparison<TKey> Comparer
        {
            get
            {
                return _comparer;
            }
        }
        #endregion
        #endregion
        #region Methods
        #region _GetKey
        private TKey _actualKey;
        private TKey _GetKey(long index)
        {
            if (index == -1)
                return _actualKey;

            var item = _indexer._items[index];
            var key = _extractKey(item);
            return key;
        }
        #endregion

        #region Committing
        /// <summary>
        /// Adds all the new items and sorts this index.
        /// </summary>
        protected internal override void Committing()
        {
            var indexerList = _indexer._items;
            long uncommittedCount = indexerList.Count;

            if (_items != null)
                _items.Resize(uncommittedCount);
            else
            {
                switch (StorageMode)
                {
                    case StorageMode.InMemory:
                        _items = new InMemoryBigArray<long>(uncommittedCount);
                        break;

                    case StorageMode.MemoryMappedFiles:
                        _items = new MmfArray<long>(uncommittedCount, 8, _MmfDelegates._readLongDelegate, _MmfDelegates._writeLongDelegate);
                        break;

                    case StorageMode.RandomAccessFiles:
                        _items = new RafArray<long>(uncommittedCount, 8, _RafDelegates._readLongDelegate, _RafDelegates._writeLongDelegate);
                        break;

                    default:
                        throw new InvalidOperationException("Internal error. Invalid StorageMode.");
                }

            }

            for (long i = _indexer.CommittedCount; i < uncommittedCount; i++)
                _items[i] = i;

            _items.Sort(_comparer2);
        }
        #endregion

        #region BinarySearch
        /// <summary>
        /// Does a binary search for a given key.
        /// If the item does not exist, the complement (~result)
        /// is returned, which contains the position where the item should be.
        /// Note that if there are many pairs with the same key, it will
        /// return the position of a valid pair, but it is not guaranteed to
        /// be the first or the last position with such key.
        /// </summary>
        public long BinarySearch(TKey key)
        {
            try
            {
                _actualKey = key;
                long result = _items.BinarySearch(-1, _comparer2);
                return result;
            }
            finally
            {
                _actualKey = default(TKey);
            }
        }
        #endregion
        #region BinarySearchFirst
        /// <summary>
        /// Does a binary search for the given key and, if there are many
        /// pairs with the same key, it is guaranteed that the result will
        /// be the position of the first item with such key.
        /// </summary>
        public long BinarySearchFirst(TKey key)
        {
            long result = BinarySearch(key);
            if (result < 0)
                return result;

            for (long i = result - 1; i >= 0; i--)
            {
                if (_comparer(_GetKey(i), key) != 0)
                    return i + 1;
            }

            return 0;
        }
        #endregion
        #region BinarySearchLast
        /// <summary>
        /// Does a binary search for the given key and, if there are many
        /// pairs with the same key, it is guaranteed that the result will
        /// be the position of the last item with such key.
        /// </summary>
        public long BinarySearchLast(TKey key)
        {
            long result = BinarySearch(key);
            if (result < 0)
                return result;

            long count = _items.Length;
            for (long i = result + 1; i < count; i++)
            {
                if (_comparer(_GetKey(i), key) != 0)
                    return i - 1;
            }

            return count - 1;
        }
        #endregion

        #region EnumerateItems
        /// <summary>
        /// Enumerates all items that have the given key.
        /// </summary>
        public override IEnumerable<TItem> EnumerateItems(TKey key)
        {
            long position = BinarySearchFirst(key);
            if (position < 0)
                yield break;

            yield return this[position];

            long count = _items.Length;
            for (long i = position + 1; i < count; i++)
            {
                TItem item = this[i];
                if (_comparer(key, _extractKey(item)) != 0)
                    yield break;

                yield return item;
            }
        }
        #endregion
        #region EnumerateItemsInRange
        /// <summary>
        /// Enumerates all items that fall between the minimumKey and maximumKey, including
        /// items with those keys (that is, &gt;= and &lt;= operators);
        /// </summary>
        public IEnumerable<TItem> EnumerateItemsInRange(TKey minimumKey, TKey maximumKey)
        {
            long position = BinarySearchFirst(minimumKey);
            if (position < 0)
                position = ~position;
            else
            {
                yield return this[position];
                position++;
            }

            long count = _items.Length;
            for (; position < count; position++)
            {
                var item = this[position];
                if (_comparer(_extractKey(item), maximumKey) > 0)
                    yield break;

                yield return item;
            }
        }
        #endregion
        #region EnumerateAllItems
        /// <summary>
        /// Enumerates all items in this index, correctly ordered.
        /// </summary>
        public IEnumerable<TItem> EnumerateAllItems()
        {
            long count = _items.Length;
            for (long i = 0; i < count; i++)
                yield return this[i];
        }
        #endregion
        #region EnumerateItemsGreaterThan
        /// <summary>
        /// Returns all items that have a key greater, but not equal, to the given
        /// key.
        /// </summary>
        public IEnumerable<TItem> EnumerateItemsGreaterThan(TKey key)
        {
            long position = BinarySearchLast(key);
            if (position < 0)
                position = ~position;
            else
                position++;

            long count = _items.Length;
            for (; position < count; position++)
            {
                var item = this[position];
                yield return item;
            }
        }
        #endregion
        #region EnumerateItemsGreaterThanOrEqualTo
        /// <summary>
        /// Returns all items that have a key greater or equal to the given
        /// key.
        /// </summary>
        public IEnumerable<TItem> EnumerateItemsGreaterThanOrEqualTo(TKey key)
        {
            long position = BinarySearchFirst(key);
            if (position < 0)
                position = ~position;
            else
            {
                yield return this[position];
                position++;
            }

            long count = _items.Length;
            for (; position < count; position++)
            {
                var item = this[position];
                yield return item;
            }
        }
        #endregion
        #region EnumerateItemsLessThan
        /// <summary>
        /// Returns all items that have a key less, but not equal, to the given
        /// key.
        /// </summary>
        public IEnumerable<TItem> EnumerateItemsLessThan(TKey key)
        {
            long count = _items.Length;
            for (long i = 0; i < count; i++)
            {
                var item = this[i];

                if (_comparer(_extractKey(item), key) >= 0)
                    yield break;

                yield return item;
            }
        }
        #endregion
        #region EnumerateItemsLessThanOrEqualTo
        /// <summary>
        /// Returns all items that have a key less or equal to the given
        /// key.
        /// </summary>
        public IEnumerable<TItem> EnumerateItemsLessThanOrEqualTo(TKey key)
        {
            long count = _items.Length;
            for (long i = 0; i < count; i++)
            {
                var item = this[i];

                if (_comparer(_extractKey(item), key) > 0)
                    yield break;

                yield return item;
            }
        }
        #endregion
        #endregion

        #region Private interface implementations
        object IOrderedIndex.this[long position]
        {
            get
            {
                return this[position];
            }
        }
        IIndexer IIndex.Indexer
        {
            get
            {
                return Indexer;
            }
        }
        IEnumerable IIndex.EnumerateItems(object key)
        {
            return EnumerateItems((TKey)key);
        }
        IEnumerable<TItem> IOrderedIndex<TItem>.EnumerateItemsInRange(object minimumKey, object maximumKey)
        {
            return EnumerateItemsInRange((TKey)minimumKey, (TKey)maximumKey);
        }
        IEnumerable<TItem> IOrderedIndex<TItem>.EnumerateItemsGreaterThan(object key)
        {
            return EnumerateItemsGreaterThan((TKey)key);
        }
        IEnumerable<TItem> IOrderedIndex<TItem>.EnumerateItemsGreaterThanOrEqualTo(object key)
        {
            return EnumerateItemsGreaterThanOrEqualTo((TKey)key);
        }
        IEnumerable<TItem> IOrderedIndex<TItem>.EnumerateItemsLessThan(object key)
        {
            return EnumerateItemsLessThan((TKey)key);
        }
        IEnumerable<TItem> IOrderedIndex<TItem>.EnumerateItemsLessThanOrEqualTo(object key)
        {
            return EnumerateItemsLessThanOrEqualTo((TKey)key);
        }

        long IOrderedIndex.BinarySearch(object key)
        {
            return BinarySearch((TKey)key);
        }
        long IOrderedIndex.BinarySearchFirst(object key)
        {
            return BinarySearchFirst((TKey)key);
        }
        long IOrderedIndex.BinarySearchLast(object key)
        {
            return BinarySearchLast((TKey)key);
        }

        IEnumerable IOrderedIndex.EnumerateItemsInRange(object minimumKey, object maximumKey)
        {
            return EnumerateItemsInRange((TKey)minimumKey, (TKey)maximumKey);
        }
        IEnumerable IOrderedIndex.EnumerateAllItems()
        {
            return EnumerateAllItems();
        }
        IEnumerable IOrderedIndex.EnumerateItemsGreaterThan(object key)
        {
            return EnumerateItemsGreaterThan((TKey)key);
        }
        IEnumerable IOrderedIndex.EnumerateItemsGreaterThanOrEqualTo(object key)
        {
            return EnumerateItemsGreaterThanOrEqualTo((TKey)key);
        }
        IEnumerable IOrderedIndex.EnumerateItemsLessThan(object key)
        {
            return EnumerateItemsLessThan((TKey)key);
        }
        IEnumerable IOrderedIndex.EnumerateItemsLessThanOrEqualTo(object key)
        {
            return EnumerateItemsLessThanOrEqualTo((TKey)key);
        }

        Delegate IOrderedIndex.Comparer
        {
            get
            {
                return Comparer;
            }
        }
        #endregion
    }
}
