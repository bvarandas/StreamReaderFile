using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest.Collections
{
    /// <summary>
	/// Delegate used by the RafArray to write items to a byte array.
	/// </summary>
	public delegate void RafWriteDelegate<T>(byte[] itemBytes, T item);
}
