using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest.IO
{
    internal sealed class _RandomAccessFileBlock
    {
        internal readonly long _position;
        internal readonly byte[] _block;

        internal _RandomAccessFileBlock(long position, byte[] content)
        {
            _position = position;
            _block = content;
        }
    }
}
