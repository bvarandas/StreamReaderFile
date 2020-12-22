using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest.Indexing
{
    /// <summary>
    /// Interface that must be implemented by all indexes that
    /// work as equality comparer indexes.
    /// </summary>
    public interface IEqualityIndex :
        IIndex
    {
        /// <summary>
        /// Gets the EqualityComparer used by this index.
        /// </summary>
        object Comparer { get; }
    }

    /// <summary>
    /// Interface that must be implemented by all indexes that
    /// work as equality comparer indexes.
    /// </summary>
    public interface IEqualityIndex<TItem> :
        IIndex<TItem>,
        IEqualityIndex
    {
    }

    /// <summary>
    /// Interface that must be implemented by all indexes that
    /// work as equality comparer indexes.
    /// </summary>
    public interface IEqualityIndex<TKey, TItem> :
        IIndex<TKey, TItem>,
        IEqualityIndex<TItem>
    {
        /// <summary>
        /// Gets the equality comparer used by this index.
        /// </summary>
        new IEqualityComparer<TKey> Comparer { get; }
    }
}
