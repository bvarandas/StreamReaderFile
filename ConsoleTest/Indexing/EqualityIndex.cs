using ConsoleTest.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest.Indexing
{
    /// <summary>
    /// Index to be used with the Indexer type to allow indexing items
    /// by equality comparison to their keys. That is, getting items
    /// with a given key is possible, but getting items with a "greater"
    /// or "smaller" key is not.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used by this index.</typeparam>
    /// <typeparam name="TItem">The type of the items that this index is indexing.</typeparam>
    public sealed class EqualityIndex<TKey, TItem> :
        AbstractIndex<TKey, TItem>,
        IEqualityIndex<TKey, TItem>
    {
        #region Private fields
        private BigArray<long> _buckets;
        private BigArray<_Node> _nodes;
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new equality index giving it a name and a delegate to extract the
        /// keys from the items that will be added.
        /// </summary>
        public EqualityIndex(string name, Func<TItem, TKey> extractKey, StorageMode storageMode) :
            this(name, extractKey, storageMode, null, 3)
        {
        }

        /// <summary>
        /// Creates a new equality index giving it a name and a delegate to extract the
        /// keys from the items that will be added, and also let's you specify the
        /// comparer to use to compare keys and an average number of items expected per key, as
        /// when there are many items with the same key it is not necessary to resize buckets.
        /// </summary>
        public EqualityIndex(string name, Func<TItem, TKey> extractKey, StorageMode storageMode, IEqualityComparer<TKey> comparer, int expectedAverageOfItemsPerKey) :
            base(name, extractKey, storageMode)
        {
            if (comparer == null)
                comparer = EqualityComparer<TKey>.Default;

            if (expectedAverageOfItemsPerKey < 1)
                throw new ArgumentOutOfRangeException("expectedAverageOfItemsPerKey", "expectedAverageOfItemsPerKey must be at least 1.");

            _comparer = comparer;
            _expectedAverageOfItemsPerKey = expectedAverageOfItemsPerKey;
        }
        #endregion
        #region Dispose
        /// <summary>
        /// Releases the big arrays used by this index (deleting
        /// temporary files if necessary).
        /// </summary>
        protected override void OnDispose()
        {
            Disposer.Dispose(ref _buckets);
            Disposer.Dispose(ref _nodes);
        }
        #endregion

        #region Properties
        #region ExpectedAverageOfItemsPerKey
        private readonly int _expectedAverageOfItemsPerKey;
        /// <summary>
        /// Gets a value that indicates the expected average of items
        /// per key. This only affects indexes sizes and it is not a limit
        /// of any kind.
        /// </summary>
        public int ExpectedAverageOfItemsPerKey
        {
            get
            {
                return _expectedAverageOfItemsPerKey;
            }
        }
        #endregion
        #region Comparer
        private readonly IEqualityComparer<TKey> _comparer;
        /// <summary>
        /// Gets the EqualityComparer used to get the hashcode and
        /// compare the keys.
        /// </summary>
        public IEqualityComparer<TKey> Comparer
        {
            get
            {
                return _comparer;
            }
        }
        #endregion
        #endregion
        #region Methods
        #region Committing
        /// <summary>
        /// Does the job of adding all the items to this index, resizing its buckets if necessary.
        /// </summary>
        protected internal override void Committing()
        {
            long uncommittedCount = _indexer.UncommittedCount;
            long newUnadaptedSize = uncommittedCount;
            if (_nodes == null)
            {
                long adaptedSize = _IndexerHelper._AdaptLength((newUnadaptedSize + _expectedAverageOfItemsPerKey - 1) / _expectedAverageOfItemsPerKey);

                if (StorageMode == StorageMode.InMemory)
                {
                    _nodes = new InMemoryBigArray<_Node>(adaptedSize * _expectedAverageOfItemsPerKey);
                    _buckets = new InMemoryBigArray<long>(adaptedSize, -1);
                }
                else
                {
                    switch (StorageMode)
                    {
                        case StorageMode.MemoryMappedFiles:
                            _nodes = new MmfArray<_Node>(adaptedSize * _expectedAverageOfItemsPerKey, 20, _MmfDelegates._readNodeDelegate, _MmfDelegates._writeNodeDelegate);
                            _buckets = new MmfArray<long>(adaptedSize, 8, _MmfDelegates._readLongDelegate, _MmfDelegates._writeLongDelegate);
                            break;

                        case StorageMode.RandomAccessFiles:
                            _nodes = new RafArray<_Node>(adaptedSize * _expectedAverageOfItemsPerKey, 20, _RafDelegates._readNodeDelegate, _RafDelegates._writeNodeDelegate);
                            _buckets = new RafArray<long>(adaptedSize, 8, _RafDelegates._readLongDelegate, _RafDelegates._writeLongDelegate);
                            break;

                        default:
                            throw new InvalidOperationException("Internal error. Invalid StorageMode.");
                    }

                    for (long i = 0; i < adaptedSize; i++)
                        _buckets[i] = -1;
                }
            }
            else
            {
                if (_nodes.Length < newUnadaptedSize)
                {
                    long biggerValue = Math.Max(newUnadaptedSize, _nodes.Length * 2);
                    long dividedSize = (biggerValue + _expectedAverageOfItemsPerKey - 1) / _expectedAverageOfItemsPerKey;
                    _Resize(dividedSize);
                }
            }

            for (long i = _indexer.CommittedCount; i < uncommittedCount; i++)
                _Add(_indexer._items[i], i);
        }
        #endregion

        #region _Add
        private void _Add(TItem item, long position)
        {
            var key = _extractKey(item);
            int hashCode = int.MinValue;

            if (key != null)
                hashCode = _comparer.GetHashCode(key);

            int reducedHashCode = hashCode & int.MaxValue;
            long bucketIndex = reducedHashCode % _buckets.Length;

            _Node node = new _Node();
            node._hashCode = hashCode;
            node._itemIndex = position;
            node._nextNode = _buckets[bucketIndex];
            _nodes[position] = node;
            _buckets[bucketIndex] = position;
        }
        #endregion
        #region _Resize
        private void _Resize(long newUnadaptedSize)
        {
            long count = _buckets.Length;
            long newSize = _IndexerHelper._AdaptLength(newUnadaptedSize);

            long oldLength = _nodes.Length;
            _nodes.Resize(newSize * _expectedAverageOfItemsPerKey);
            //for(long i=oldLength; i<_nodes.Length; i++)
            //_nodes[i] = new _Node { _nextNode = -1 };

            long bucketCount = Math.Min(newSize, int.MaxValue);
            if (bucketCount == _buckets.Length)
                return;

            var newBuckets = _buckets.CreateNew(bucketCount);
            if (StorageMode != StorageMode.InMemory)
            {
                for (long i = 0; i < bucketCount; i++)
                    newBuckets[i] = -1;
            }

            for (long i = 0; i < count; i++)
            {
                long nodeIndex = _buckets[i];
                while (nodeIndex != -1)
                {
                    var node = _nodes[nodeIndex];
                    int hashCode = node._hashCode;
                    int reducedHashCode = hashCode & int.MaxValue;
                    long newBucketIndex = reducedHashCode % bucketCount;

                    long nextNodeIndex = node._nextNode;
                    node._nextNode = newBuckets[newBucketIndex];
                    _nodes[nodeIndex] = node;
                    newBuckets[newBucketIndex] = nodeIndex;

                    nodeIndex = nextNodeIndex;
                }
            }

            _buckets = newBuckets;
        }
        #endregion

        #region EnumerateItems
        /// <summary>
        /// Enumerates all items that have the specified key.
        /// </summary>
        public override IEnumerable<TItem> EnumerateItems(TKey key)
        {
            //if (_indexer.InTransaction)
            //    throw new InvalidOperationException("You can't enumerate this index while a transaction is still active.");

            int hashCode = int.MinValue;
            if (key != null)
                hashCode = _comparer.GetHashCode(key);

            int reducedHashCode = hashCode & int.MaxValue;
            long bucketIndex = reducedHashCode % _buckets.Length;
            long nodeIndex = _buckets[bucketIndex];
            while (nodeIndex != -1)
            {
                if (_nodes[nodeIndex]._hashCode == hashCode)
                {
                    long itemIndex = _nodes[nodeIndex]._itemIndex;
                    TItem item = _indexer._items[itemIndex];
                    TKey itemKey = _extractKey(item);
                    if (_comparer.Equals(itemKey, key))
                        yield return item;
                }

                nodeIndex = _nodes[nodeIndex]._nextNode;
            }
        }
        #endregion
        #endregion

        #region Private interface implementations
        object IEqualityIndex.Comparer
        {
            get
            {
                return Comparer;
            }
        }
        #endregion
    }
}
