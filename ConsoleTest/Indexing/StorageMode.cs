using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest.Indexing
{
    /// <summary>
	/// Mode used by the Indexer and its Indexers to store its internal
	/// bigarrays and lists.
	/// </summary>
	public enum StorageMode
    {
        /// <summary>
        /// The required items will be stored in memory.
        /// </summary>
        InMemory,

        /// <summary>
        /// The required items will be stored in memory mapped files.
        /// </summary>
        MemoryMappedFiles,

        /// <summary>
        /// The required items will be stored in random access files.
        /// </summary>
        RandomAccessFiles
    }
}
