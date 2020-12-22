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
    /// Interface implemented by both 32-bit and 64-bit indexes.
    /// </summary>
    public interface IIndex :
        IAdvancedDisposable
    {
        /// <summary>
        /// Gets the type of the key used by this index.
        /// </summary>
        Type TypeOfKeys { get; }

        /// <summary>
        /// Gets the type of the items that this index
        /// is indexing.
        /// </summary>
        Type TypeOfItems { get; }

        /// <summary>
        /// Gets the indexer that created this index.
        /// </summary>
        IIndexer Indexer { get; }

        /// <summary>
        /// Gets the delegate used to extract the key from an item.
        /// </summary>
        Delegate ExtractKey { get; }

        /// <summary>
        /// Gets the name of this index.
        /// Note that "unnamed" indexes, in fact, use the data-type as their names.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Enumerates all items that have the given key.
        /// </summary>
        IEnumerable EnumerateItems(object key);
    }

    /// <summary>
    /// Interface implemented by both 32-bit and 64-bit indexes.
    /// </summary>
    public interface IIndex<TItem> :
        IIndex
    {
        /// <summary>
        /// Gets the indexer that created this index.
        /// </summary>
        new IIndexer<TItem> Indexer { get; }

        /// <summary>
        /// Enumerates all items that have the given key.
        /// </summary>
        new IEnumerable<TItem> EnumerateItems(object key);
    }

    /// <summary>
    /// Interface implemented by both 32-bit and 64-bit indexes.
    /// </summary>
    public interface IIndex<TKey, TItem> :
        IIndex<TItem>
    {
        /// <summary>
        /// Gets the delegate used to extract the key from an item.
        /// </summary>
        new Func<TItem, TKey> ExtractKey { get; }

        /// <summary>
        /// Enumerates all items that have the given key.
        /// </summary>
        IEnumerable<TItem> EnumerateItems(TKey key);
    }
}
