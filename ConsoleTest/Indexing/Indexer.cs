using ConsoleTest.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest.Indexing
{
    /// <summary>
    /// This is a dictionary like class that allows items to be indexed by many different
    /// indexes/keys.
    /// </summary>
    /// <typeparam name="TItem">The type of the items that will be put into this dictionary.</typeparam>
    public sealed class Indexer<TItem> :
        IIndexer<TItem>,
        IAdvancedDisposable
    {
        #region Private and Internal Fields
        internal BigList<TItem> _items;
        private readonly BigIndexedReadOnlyCollection<TItem> _allItemsReadOnly;
        internal Dictionary<string, AbstractIndex<TItem>> _indexes = new Dictionary<string, AbstractIndex<TItem>>();
        private readonly StorageMode _storageMode;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new indexer instance.
        /// </summary>
        public Indexer()
        {
            _items = new BigList<TItem>();
            _allItemsReadOnly = _items.AsReadOnly();
        }

        /// <summary>
        /// Initializes a new indexer that will use MemoryMappedFiles for its item
        /// storage.
        /// </summary>
        [CLSCompliant(false)]
        public Indexer(int itemLength, MmfReadDelegate<TItem> readItem, MmfWriteDelegate<TItem> writeItem)
        {
            _items = new BigList<TItem>(InMemoryBigArray.DefaultBlockLength, itemLength, readItem, writeItem);
            _allItemsReadOnly = _items.AsReadOnly();
            _storageMode = StorageMode.MemoryMappedFiles;
        }

        /// <summary>
        /// Initializes a new indexer that will use RandomAccesFiles for its item
        /// storage.
        /// </summary>
        public Indexer(int itemLength, RafReadDelegate<TItem> readItem, RafWriteDelegate<TItem> writeItem)
        {
            _items = new BigList<TItem>(InMemoryBigArray.DefaultBlockLength, itemLength, readItem, writeItem);
            _allItemsReadOnly = _items.AsReadOnly();
            _storageMode = StorageMode.RandomAccessFiles;
        }
        #endregion
        #region Dispose
        /// <summary>
        /// Releases all the resources used by this indexer and by
        /// its indexes, deleting temporary files if necessary.
        /// </summary>
        public void Dispose()
        {
            var indexes = _indexes;
            if (indexes != null)
            {
                _indexes = null;

                foreach (var index in indexes.Values)
                {
                    index._indexer = null;
                    index.Dispose();
                }
            }

            Disposer.Dispose(ref _items);
        }
        #endregion

        #region Properties
        #region WasDisposed
        /// <summary>
        /// Gets a value indicating if this indexer was already disposed.
        /// </summary>
        public bool WasDisposed
        {
            get
            {
                return _items == null;
            }
        }
        #endregion

        #region StorageMode
        /// <summary>
        /// Gets the storage mode used by this indexer. This is also the default
        /// storage mode that will be used by the indexes created by it.
        /// </summary>
        public StorageMode StorageMode
        {
            get
            {
                return _storageMode;
            }
        }
        #endregion
        #region InTransaction
        /// <summary>
        /// Gets a value indicating if this indexer has at least one active transaction.
        /// </summary>
        public bool InTransaction
        {
            get
            {
                return _transactionCount > 0;
            }
        }
        #endregion

        #region UncommittedCount
        /// <summary>
        /// Gets the number of items actually added into this indexer, even
        /// if they aren't committed yet.
        /// </summary>
        public long UncommittedCount
        {
            get
            {
                return _items.Count;
            }
        }
        #endregion
        #region CommittedCount
        private long _committedCount;
        /// <summary>
        /// Gets the number of items actually committed into this indexer.
        /// </summary>
        public long CommittedCount
        {
            get
            {
                return _committedCount;
            }
        }
        #endregion
        #endregion
        #region Methods
        #region AddIndex
        /// <summary>
        /// Adds an index to this indexer.
        /// </summary>
        public void AddIndex(AbstractIndex<TItem> index)
        {
            if (index == null)
                throw new ArgumentNullException("index");

            if (index._indexer != null)
                throw new ArgumentException("The given index is already owner by another indexer.", "index");

            _indexes.Add(index.Name, index);
            index._indexer = this;

            if (_committedCount > 0)
            {
                long oldCount = _committedCount;
                try
                {
                    _committedCount = 0;
                    index.Committing();
                }
                finally
                {
                    _committedCount = oldCount;
                }
            }
        }
        #endregion
        #region AddEqualityIndex
        /// <summary>
        /// Creates and adds an equality index to this indexer.
        /// </summary>
        public EqualityIndex<TKey, TItem> AddEqualityIndex<TKey>(string name, Func<TItem, TKey> extractKey)
        {
            var index = new EqualityIndex<TKey, TItem>(name, extractKey, _storageMode);
            AddIndex(index);
            return index;
        }
        #endregion
        #region AddOrderedIndex
        /// <summary>
        /// Creates and adds an ordered index to this indexer.
        /// </summary>
        public OrderedIndex<TKey, TItem> AddOrderedIndex<TKey>(string name, Func<TItem, TKey> extractKey)
        {
            var index = new OrderedIndex<TKey, TItem>(name, extractKey, _storageMode);
            AddIndex(index);
            return index;
        }
        #endregion

        #region TryGetIndex
        /// <summary>
        /// Tries to get an index by name.
        /// </summary>
        public AbstractIndex<TItem> TryGetIndex(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            AbstractIndex<TItem> result;
            _indexes.TryGetValue(name, out result);
            return result;
        }

        /// <summary>
        /// Tries to get a TKey index with the given name.
        /// </summary>
        public AbstractIndex<TKey, TItem> TryGetIndex<TKey>(string name)
        {
            return TryGetIndex(name) as AbstractIndex<TKey, TItem>;
        }
        #endregion
        #region GetIndex
        /// <summary>
        /// Gets an index by name or throws an exception.
        /// </summary>
        public AbstractIndex<TItem> GetIndex(string name)
        {
            var result = TryGetIndex(name);

            if (result == null)
                throw new ArgumentException("There is no index named: " + name);

            return result;
        }

        /// <summary>
        /// Gets an index by its name. Throws an exception if one does not exist.
        /// </summary>
        public AbstractIndex<TKey, TItem> GetIndex<TKey>(string name)
        {
            return (AbstractIndex<TKey, TItem>)GetIndex(name);
        }
        #endregion
        #region EnumerateIndexes
        /// <summary>
        /// Enumerates all indexes present in this indexer.
        /// </summary>
        public IEnumerable<AbstractIndex<TItem>> EnumerateIndexes()
        {
            return _indexes.Values;
        }
        #endregion

        #region EnumerateAllItemsItems
        /// <summary>
        /// Enumerates all items in this indexer, including both committed and
        /// uncommitted items.
        /// </summary>
        public IEnumerable<TItem> EnumerateAllItemsItems()
        {
            return _allItemsReadOnly;
        }
        #endregion
        #region EnumerateCommittedItems
        /// <summary>
        /// Enumerates all committed items present in this indexer.
        /// </summary>
        public IEnumerable<TItem> EnumerateCommittedItems()
        {
            for (long i = 0; i < _committedCount; i++)
                yield return _items[i];
        }
        #endregion
        #region EnumerateUncommittedItems
        /// <summary>
        /// Enumerates all items that were added into this indexer but there
        /// aren't committed yet.
        /// </summary>
        public IEnumerable<TItem> EnumerateUncommittedItems()
        {
            long uncommittedCount = UncommittedCount;
            for (long i = _committedCount; i < uncommittedCount; i++)
                yield return _items[i];
        }
        #endregion

        #region StartTransaction
        internal bool _mustRollback;
        private int _transactionCount;
        /// <summary>
        /// Starts a transaction (or an inner transaction) that you can use to add many items
        /// to this indexer.
        /// </summary>
        public IndexerTransaction<TItem> StartTransaction()
        {
            var result = new IndexerTransaction<TItem>(this);
            _transactionCount++;
            return result;
        }
        internal void _Rollback()
        {
            _transactionCount--;

            if (_transactionCount > 0)
            {
                _mustRollback = true;
                return;
            }

            long count = UncommittedCount - _committedCount;
            if (count > 0)
                _items.RemoveRange(_committedCount, count);

            _mustRollback = false;
        }
        internal void _Commit()
        {
            if (_mustRollback)
                throw new InvalidOperationException("An inner transaction was rolled back, so you can't commit anymore.");

            _transactionCount--;

            if (_transactionCount > 0)
                return;

            long uncommittedCount = UncommittedCount;
            long countToAdd = uncommittedCount - _committedCount;
            if (countToAdd == 0)
                return;

            foreach (var index in _indexes.Values)
                index.Committing();

            _committedCount = uncommittedCount;
        }
        #endregion
        #endregion

        #region Private interface implementations
        Type IIndexer.TypeOfItems
        {
            get
            {
                return typeof(TItem);
            }
        }
        IEnumerable<IIndex> IIndexer.EnumerateIndexes()
        {
            return EnumerateIndexes();
        }
        IIndex<TItem> IIndexer<TItem>.TryGetIndex(string name)
        {
            return TryGetIndex(name);
        }
        IIndex<TItem> IIndexer<TItem>.GetIndex(string name)
        {
            return GetIndex(name);
        }
        IIndex<TKey, TItem> IIndexer<TItem>.TryGetIndex<TKey>(string name)
        {
            return TryGetIndex<TKey>(name);
        }
        IIndex<TKey, TItem> IIndexer<TItem>.GetIndex<TKey>(string name)
        {
            return GetIndex<TKey>(name);
        }

        IIndex IIndexer.TryGetIndex<TKey>(string name)
        {
            return TryGetIndex<TKey>(name);
        }
        IIndex IIndexer.GetIndex<TKey>(string name)
        {
            return GetIndex<TKey>(name);
        }

        IEnumerable IIndexer.EnumerateAllItemsItems()
        {
            return EnumerateAllItemsItems();
        }

        IEnumerable IIndexer.EnumerateCommittedItems()
        {
            return EnumerateCommittedItems();
        }
        IEnumerable IIndexer.EnumerateUncommittedItems()
        {
            return EnumerateUncommittedItems();
        }
        IIndexerTransaction IIndexer.StartTransaction()
        {
            return StartTransaction();
        }

        IIndex IIndexer.TryGetIndex(string name)
        {
            return TryGetIndex(name);
        }
        IIndex IIndexer.GetIndex(string name)
        {
            return GetIndex(name);
        }
        IEnumerable<IIndex<TItem>> IIndexer<TItem>.EnumerateIndexes()
        {
            return EnumerateIndexes();
        }
        IIndexerTransaction<TItem> IIndexer<TItem>.StartTransaction()
        {
            return StartTransaction();
        }

        void IIndexer.AddIndex(IIndex index)
        {
            AddIndex((AbstractIndex<TItem>)index);
        }

        IEqualityIndex<TKey, TItem> IIndexer<TItem>.AddEqualityIndex<TKey>(string name, Func<TItem, TKey> extractKey)
        {
            return AddEqualityIndex(name, extractKey);
        }
        IOrderedIndex<TKey, TItem> IIndexer<TItem>.AddOrderedIndex<TKey>(string name, Func<TItem, TKey> extractKey)
        {
            return AddOrderedIndex(name, extractKey);
        }

        IEqualityIndex IIndexer.AddEqualityIndex<TKey>(string name, Delegate extractKey)
        {
            return AddEqualityIndex<TKey>(name, (Func<TItem, TKey>)extractKey);
        }
        IOrderedIndex IIndexer.AddOrderedIndex<TKey>(string name, Delegate extractKey)
        {
            return AddOrderedIndex<TKey>(name, (Func<TItem, TKey>)extractKey);
        }
        #endregion
    }
}
