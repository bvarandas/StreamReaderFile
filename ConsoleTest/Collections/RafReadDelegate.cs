using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest.Collections
{
    /// <summary>
	/// Delegate used by the RafArray to get an item from a byte array.
	/// </summary>
	public delegate T RafReadDelegate<T>(byte[] itemBytes);
}
