using System;

namespace ConsoleTest.Collections
{
    /// <summary>
	/// Delegate used by the MmfArray to read an item from a memory address.
	/// </summary>
	[CLSCompliant(false)]
    public unsafe delegate T MmfReadDelegate<T>(byte* itemAddress);
}
