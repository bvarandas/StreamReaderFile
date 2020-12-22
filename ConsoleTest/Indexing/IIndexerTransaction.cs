using ConsoleTest.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest.Indexing
{
    /// <summary>
    ///  interface implemented by 32-bit and 64-bit indexer
    /// transactions.
    /// </summary>
    public interface IIndexerTransaction :
        IAdvancedDisposable
    {
        /// <summary>
        /// Gets the indexer that started this transaction.
        /// </summary>
        IIndexer Indexer { get; }

        /// <summary>
        /// Gets a value indicating if a child transaction asked
        /// for rollback and, so, this transaction can't be committed
        /// anymore.
        /// </summary>
        bool WasRollbackRequested { get; }

        /// <summary>
        /// Adds an item to the indexer that started this transaction.
        /// </summary>
        void Add(object item);

        /// <summary>
        /// Commits this transaction. After committing you should immediately dispose
        /// the transaction, as the transaction becomes useless after the commit.
        /// If you don't commit and dispose the transaction, it is considered that
        /// you requested a rollback.
        /// </summary>
        void Commit();
    }

    /// <summary>
    ///  interface implemented by 32-bit and 64-bit indexer
    /// transactions.
    /// </summary>
    public interface IIndexerTransaction<TItem> :
        IIndexerTransaction
    {
        /// <summary>
        /// Gets the indexer that started this transaction.
        /// </summary>
        new IIndexer<TItem> Indexer { get; }

        /// <summary>
        /// Adds an item to the indexer that started this transaction.
        /// </summary>
        void Add(TItem item);
    }
}
