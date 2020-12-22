using ConsoleTest.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest.Indexing
{
    internal sealed class _RafDelegates
    {
        internal static readonly RafReadDelegate<long> _readLongDelegate = _ReadLong;
        private static long _ReadLong(byte[] itemBytes)
        {
            return BitConverter.ToInt64(itemBytes, 0);
        }

        internal static readonly RafWriteDelegate<long> _writeLongDelegate = _WriteLong;
        private static void _WriteLong(byte[] itemBytes, long value)
        {
            BitConverterEx.FillBytes(itemBytes, 0, value);
        }

        internal static readonly RafReadDelegate<_Node> _readNodeDelegate = _ReadNode;
        private static _Node _ReadNode(byte[] itemBytes)
        {
            _Node node = new _Node();
            node._nextNode = BitConverter.ToInt64(itemBytes, 0);
            node._itemIndex = BitConverter.ToInt64(itemBytes, 8);
            node._hashCode = BitConverter.ToInt32(itemBytes, 16);
            return node;
        }

        internal static readonly RafWriteDelegate<_Node> _writeNodeDelegate = _WriteNode;
        private static void _WriteNode(byte[] itemBytes, _Node node)
        {
            BitConverterEx.FillBytes(itemBytes, 0, node._nextNode);
            BitConverterEx.FillBytes(itemBytes, 8, node._itemIndex);
            BitConverterEx.FillBytes(itemBytes, 16, node._hashCode);
        }
    }
}
