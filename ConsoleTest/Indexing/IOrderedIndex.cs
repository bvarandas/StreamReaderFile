using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest.Indexing
{
    /// <summary>
    /// Interface that must be implemented by indexes
    /// that always keep their values ordered.
    /// </summary>
    public interface IOrderedIndex :
        IIndex
    {
        /// <summary>
        /// Gets the comparer delegate used by this ordered index.
        /// </summary>
        Delegate Comparer { get; }

        /// <summary>
        /// Gets an item by its ordered position.
        /// </summary>
        object this[long position] { get; }

        /// <summary>
        /// Does a binary search for a given key.
        /// If the item does not exist, the complement (~result)
        /// is returned, which contains the position where the item should be.
        /// Note that if there are many pairs with the same key, it will
        /// return the position of a valid pair, but it is not guaranteed to
        /// be the first or the last position with such key.
        /// </summary>
        long BinarySearch(object key);

        /// <summary>
        /// Does a binary search for the given key and, if there are many
        /// pairs with the same key, it is guaranteed that the result will
        /// be the position of the first item with such key.
        /// </summary>
        long BinarySearchFirst(object key);

        /// <summary>
        /// Does a binary search for the given key and, if there are many
        /// pairs with the same key, it is guaranteed that the result will
        /// be the position of the last item with such key.
        /// </summary>
        long BinarySearchLast(object key);

        /// <summary>
        /// Enumerates all items that fall between the minimumKey and maximumKey, including
        /// items with those keys (that is, &gt;= and &lt;= operators);
        /// </summary>
        IEnumerable EnumerateItemsInRange(object minimumKey, object maximumKey);

        /// <summary>
        /// Enumerates all items in this index, correctly ordered.
        /// </summary>
        IEnumerable EnumerateAllItems();

        /// <summary>
        /// Returns all items that have a key greater, but not equal, to the given
        /// key.
        /// </summary>
        IEnumerable EnumerateItemsGreaterThan(object key);

        /// <summary>
        /// Returns all items that have a key greater or equal to the given
        /// key.
        /// </summary>
        IEnumerable EnumerateItemsGreaterThanOrEqualTo(object key);

        /// <summary>
        /// Returns all items that have a key less, but not equal, to the given
        /// key.
        /// </summary>
        IEnumerable EnumerateItemsLessThan(object key);

        /// <summary>
        /// Returns all items that have a key less or equal to the given
        /// key.
        /// </summary>
        IEnumerable EnumerateItemsLessThanOrEqualTo(object key);
    }

    /// <summary>
    /// Interface that must be implemented by indexes
    /// that always keep their values ordered.
    /// </summary>
    public interface IOrderedIndex<TItem> :
        IIndex<TItem>,
        IOrderedIndex
    {
        /// <summary>
        /// Gets an item by its ordered position.
        /// </summary>
        new TItem this[long position] { get; }

        /// <summary>
        /// Enumerates all items that fall between the minimumKey and maximumKey, including
        /// items with those keys (that is, &gt;= and &lt;= operators);
        /// </summary>
        new IEnumerable<TItem> EnumerateItemsInRange(object minimumKey, object maximumKey);

        /// <summary>
        /// Returns all items that have a key greater, but not equal, to the given
        /// key.
        /// </summary>
        new IEnumerable<TItem> EnumerateItemsGreaterThan(object key);

        /// <summary>
        /// Returns all items that have a key greater or equal to the given
        /// key.
        /// </summary>
        new IEnumerable<TItem> EnumerateItemsGreaterThanOrEqualTo(object key);

        /// <summary>
        /// Returns all items that have a key less, but not equal, to the given
        /// key.
        /// </summary>
        new IEnumerable<TItem> EnumerateItemsLessThan(object key);

        /// <summary>
        /// Returns all items that have a key less or equal to the given
        /// key.
        /// </summary>
        new IEnumerable<TItem> EnumerateItemsLessThanOrEqualTo(object key);
    }

    /// <summary>
    /// Interface that must be implemented by indexes
    /// that always keep their values ordered.
    /// </summary>
    public interface IOrderedIndex<TKey, TItem> :
        IIndex<TKey, TItem>,
        IOrderedIndex<TItem>
    {
        /// <summary>
        /// Gets the comparer used by this ordered index.
        /// </summary>
        new Comparison<TKey> Comparer { get; }

        /// <summary>
        /// Does a binary search for a given key.
        /// If the item does not exist, the complement (~result)
        /// is returned, which contains the position where the item should be.
        /// Note that if there are many pairs with the same key, it will
        /// return the position of a valid pair, but it is not guaranteed to
        /// be the first or the last position with such key.
        /// </summary>
        long BinarySearch(TKey key);

        /// <summary>
        /// Does a binary search for the given key and, if there are many
        /// pairs with the same key, it is guaranteed that the result will
        /// be the position of the first item with such key.
        /// </summary>
        long BinarySearchFirst(TKey key);

        /// <summary>
        /// Does a binary search for the given key and, if there are many
        /// pairs with the same key, it is guaranteed that the result will
        /// be the position of the last item with such key.
        /// </summary>
        long BinarySearchLast(TKey key);

        /// <summary>
        /// Enumerates all items that fall between the minimumKey and maximumKey, including
        /// items with those keys (that is, &gt;= and &lt;= operators);
        /// </summary>
        IEnumerable<TItem> EnumerateItemsInRange(TKey minimumKey, TKey maximumKey);

        /// <summary>
        /// Enumerates all items in this index, correctly ordered.
        /// </summary>
        new IEnumerable<TItem> EnumerateAllItems();

        /// <summary>
        /// Returns all items that have a key greater, but not equal, to the given
        /// key.
        /// </summary>
        IEnumerable<TItem> EnumerateItemsGreaterThan(TKey key);

        /// <summary>
        /// Returns all items that have a key greater or equal to the given
        /// key.
        /// </summary>
        IEnumerable<TItem> EnumerateItemsGreaterThanOrEqualTo(TKey key);

        /// <summary>
        /// Returns all items that have a key less, but not equal, to the given
        /// key.
        /// </summary>
        IEnumerable<TItem> EnumerateItemsLessThan(TKey key);

        /// <summary>
        /// Returns all items that have a key less or equal to the given
        /// key.
        /// </summary>
        IEnumerable<TItem> EnumerateItemsLessThanOrEqualTo(TKey key);
    }
}
