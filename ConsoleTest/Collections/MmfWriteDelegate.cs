using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest.Collections
{
    /// <summary>
	/// Delegate used by the MmfArray to write an item to a memory address.
	/// </summary>
	[CLSCompliant(false)]
    public unsafe delegate void MmfWriteDelegate<T>(byte* itemAddress, T item);
}
