using ConsoleTest.Collections;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ConsoleTest.Indexing
{
    /// <summary>
    /// Interface implemented by both 32-bit and 64-bit indexers.
    /// </summary>
    public interface IIndexer :
        IAdvancedDisposable
    {
        /// <summary>
        /// Gets the type of items this dictionary supports.
        /// </summary>
        Type TypeOfItems { get; }

        /// <summary>
        /// Gets a value indicating if this indexer is actually in a transaction.
        /// </summary>
        bool InTransaction { get; }

        /// <summary>
        /// Gets the number of items actually added into this indexer, even
        /// if they aren't committed yet.
        /// </summary>
        long UncommittedCount { get; }

        /// <summary>
        /// Gets the number of items actually committed into this indexer.
        /// </summary>
        long CommittedCount { get; }

        /// <summary>
        /// Adds an index to this indexer.
        /// </summary>
        void AddIndex(IIndex index);

        /// <summary>
        /// Creates and adds an equality index to this indexer.
        /// </summary>
        IEqualityIndex AddEqualityIndex<TKey>(string name, Delegate extractKey);

        /// <summary>
        /// Creates and adds an ordered index to this indexer.
        /// </summary>
        IOrderedIndex AddOrderedIndex<TKey>(string name, Delegate extractKey);

        /// <summary>
        /// Enumerates all items in this indexer, including both committed and
        /// uncommitted items.
        /// </summary>
        IEnumerable EnumerateAllItemsItems();

        /// <summary>
        /// Enumerates all committed items present in this indexer.
        /// </summary>
        IEnumerable EnumerateCommittedItems();

        /// <summary>
        /// Enumerates all items that were added into this indexer but there
        /// aren't committed yet.
        /// </summary>
        IEnumerable EnumerateUncommittedItems();

        /// <summary>
        /// Enumerates all indexes present in this indexer.
        /// </summary>
        IEnumerable<IIndex> EnumerateIndexes();

        /// <summary>
        /// Tries to get an index by name.
        /// </summary>
        IIndex TryGetIndex(string name);

        /// <summary>
        /// Gets an index by name or throws an exception.
        /// </summary>
        IIndex GetIndex(string name);

        /// <summary>
        /// Tries to get an index that works with items of the given TKey type.
        /// </summary>
        IIndex TryGetIndex<TKey>(string name);

        /// <summary>
        /// Gets an index that works with items of the given TKey type or throws an exception.
        /// </summary>
        IIndex GetIndex<TKey>(string name);

        /// <summary>
        /// Starts a transaction into this indexer, so items can be added.
        /// Note that if you create a transaction when another one is already active, 
        /// it will be considered a child transaction. Child transactions don't
        /// really commit data, but if they aren't committed, they request a
        /// rollback of the main transaction. Trying to commit the main transaction
        /// when a child transaction requested rollback throws an InvalidOperationException.
        /// </summary>
        IIndexerTransaction StartTransaction();
    }

    /// <summary>
    /// Interface implemented by both 32-bit and 64-bit indexers.
    /// </summary>
    public interface IIndexer<TItem> :
        IIndexer
    {
        /// <summary>
        /// Creates and adds an equality index to this indexer.
        /// </summary>
        IEqualityIndex<TKey, TItem> AddEqualityIndex<TKey>(string name, Func<TItem, TKey> extractKey);

        /// <summary>
        /// Creates and adds an ordered index to this indexer.
        /// </summary>
        IOrderedIndex<TKey, TItem> AddOrderedIndex<TKey>(string name, Func<TItem, TKey> extractKey);

        /// <summary>
        /// Enumerates all indexes present in this indexer.
        /// </summary>
        new IEnumerable<IIndex<TItem>> EnumerateIndexes();

        /// <summary>
        /// Tries to get an index by name.
        /// </summary>
        new IIndex<TItem> TryGetIndex(string name);

        /// <summary>
        /// Gets an index by name or throws an exception.
        /// </summary>
        new IIndex<TItem> GetIndex(string name);

        /// <summary>
        /// Tries to get an index that works with items of the given TKey type.
        /// If name is null, the TKey name is used as the index name.
        /// </summary>
        new IIndex<TKey, TItem> TryGetIndex<TKey>(string name);

        /// <summary>
        /// Gets an index that works with items of the given TKey type or throws an exception.
        /// If name is null, the TKey name is used as the index name.
        /// </summary>
        new IIndex<TKey, TItem> GetIndex<TKey>(string name);

        /// <summary>
        /// Enumerates all items in this indexer, including both committed and
        /// uncommitted items.
        /// </summary>
        new IEnumerable<TItem> EnumerateAllItemsItems();

        /// <summary>
        /// Enumerates all committed items present in this indexer.
        /// </summary>
        new IEnumerable<TItem> EnumerateCommittedItems();

        /// <summary>
        /// Enumerates all items that were added into this indexer but there
        /// aren't committed yet.
        /// </summary>
        new IEnumerable<TItem> EnumerateUncommittedItems();

        /// <summary>
        /// Starts a transaction into this indexer, so items can be added.
        /// Note that if you create a transaction when another one is already active, 
        /// it will be considered a child transaction. Child transactions don't
        /// really commit data, but if they aren't committed, they request a
        /// rollback of the main transaction. Trying to commit the main transaction
        /// when a child transaction requested rollback throws an InvalidOperationException.
        /// </summary>
        new IIndexerTransaction<TItem> StartTransaction();
    }
}
