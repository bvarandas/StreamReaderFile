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
    /// Base class required by the DictionaryIndex&lt;TKey, TItem&gt;
    /// You should never need to use this type directly, use either
    /// the rightly typed DictionaryIndex&lt;TKey, TItem&gt; or the
    /// IDictionaryIndex interface.
    /// </summary>
    public abstract class AbstractIndex<TItem> :
        IIndex<TItem>,
        IAdvancedDisposable
    {
        #region Internal constructor
        internal AbstractIndex(string name, StorageMode storageMode)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            _name = name;

            switch (storageMode)
            {
                case StorageMode.InMemory:
                case StorageMode.MemoryMappedFiles:
                case StorageMode.RandomAccessFiles:
                    break;

                default:
                    throw new ArgumentException("storageMode is invalid.", "storageMode");
            }

            _storageMode = storageMode;
        }
        #endregion
        #region Dispose
        /// <summary>
        /// Must be implemented by sub classes to release all the
        /// resources used by this index immediately.
        /// </summary>
        protected abstract void OnDispose();

        /// <summary>
        /// Cleanups all the resources used by this index, even
        /// deleting temporary files if necessary.
        /// </summary>
        public void Dispose()
        {
            if (WasDisposed)
                return;

            WasDisposed = true;

            OnDispose();

            var indexer = _indexer;
            if (indexer != null)
            {
                _indexer = null;
                indexer._indexes.Remove(_name);
            }
        }
        #endregion

        #region Abstract method - Committing
        /// <summary>
        /// This method is called when the Indexer is committing, so each
        /// index must do what is necessary to become rightly indexed.
        /// The final number of items can be obtained by accessing the
        /// indexer UncommittedCount property and the new items
        /// can be accessed through the EnumerateUncommitted() method.
        /// </summary>
        internal protected abstract void Committing();
        #endregion
        #region Properties
        #region WasDisposed
        /// <summary>
        /// Gets a value indicating if this index was already disposed.
        /// </summary>
        public bool WasDisposed { get; private set; }
        #endregion

        #region Name
        private readonly string _name;
        /// <summary>
        /// Gets the name of this index.
        /// Note that "unnamed" indexes, in fact, use the data-type as their names.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
        }
        #endregion
        #region Indexer
        internal Indexer<TItem> _indexer;
        /// <summary>
        /// Gets the indexer that owns this index.
        /// </summary>
        public Indexer<TItem> Indexer
        {
            get
            {
                return _indexer;
            }
        }
        #endregion
        #region StorageMode
        private StorageMode _storageMode;
        /// <summary>
        /// Gets the mode this index is stored (in memory, MemoryMappedFiles or RandomAccessFiles).
        /// </summary>
        public StorageMode StorageMode
        {
            get
            {
                return _storageMode;
            }
        }
        #endregion
        #endregion

        #region Private interface implementations
        IIndexer<TItem> IIndex<TItem>.Indexer
        {
            get
            {
                return Indexer;
            }
        }
        IIndexer IIndex.Indexer
        {
            get
            {
                return Indexer;
            }
        }
        Type IIndex.TypeOfKeys
        {
            get
            {
                throw new NotSupportedException();
            }
        }
        Type IIndex.TypeOfItems
        {
            get
            {
                return typeof(TItem);
            }
        }
        Delegate IIndex.ExtractKey
        {
            get
            {
                throw new NotSupportedException();
            }
        }
        IEnumerable IIndex.EnumerateItems(object key)
        {
            throw new NotSupportedException();
        }
        IEnumerable<TItem> IIndex<TItem>.EnumerateItems(object key)
        {
            throw new NotSupportedException();
        }
        #endregion
    }

    /// <summary>
    /// Base class that gives you access to an index with the
    /// key and the items correctly typed.
    /// </summary>
    public abstract class AbstractIndex<TKey, TItem> :
        AbstractIndex<TItem>,
        IIndex<TKey, TItem>
    {
        #region Constructor
        /// <summary>
        /// Creates a new abstract index setting its name and the delegate to 
        /// extract keys from items.
        /// </summary>
        protected AbstractIndex(string name, Func<TItem, TKey> extractKey, StorageMode storageMode) :
            base(name, storageMode)
        {
            if (extractKey == null)
                throw new ArgumentNullException("extractKey");

            _extractKey = extractKey;
        }
        #endregion

        #region Abstract method - EnumerateItems
        /// <summary>
        /// Enumerates all items that have the given key.
        /// </summary>
        public abstract IEnumerable<TItem> EnumerateItems(TKey key);
        #endregion
        #region Property - ExtractKey
        internal readonly Func<TItem, TKey> _extractKey;
        /// <summary>
        /// Gets the delegate used to extract the key from an item.
        /// </summary>
        public Func<TItem, TKey> ExtractKey
        {
            get
            {
                return _extractKey;
            }
        }
        #endregion

        #region Private interface implementations
        Delegate IIndex.ExtractKey
        {
            get
            {
                return _extractKey;
            }
        }

        IEnumerable<TItem> IIndex<TItem>.EnumerateItems(object key)
        {
            return EnumerateItems((TKey)key);
        }
        IEnumerable IIndex.EnumerateItems(object key)
        {
            return EnumerateItems((TKey)key);
        }

        Type IIndex.TypeOfKeys
        {
            get
            {
                return typeof(TKey);
            }
        }
        #endregion
    }
}
