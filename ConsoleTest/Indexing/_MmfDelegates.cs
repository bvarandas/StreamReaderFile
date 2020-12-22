using ConsoleTest.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest.Indexing
{
    internal unsafe sealed class _MmfDelegates
    {
        internal static readonly MmfReadDelegate<long> _readLongDelegate = _ReadLong;
        private static long _ReadLong(byte* pointer)
        {
            long* longPointer = (long*)pointer;
            return *longPointer;
        }

        internal static readonly MmfWriteDelegate<long> _writeLongDelegate = _WriteLong;
        private static void _WriteLong(byte* pointer, long value)
        {
            long* longPointer = (long*)pointer;
            *longPointer = value;
        }

        internal static readonly MmfReadDelegate<_Node> _readNodeDelegate = _ReadNode;
        private static _Node _ReadNode(byte* pointer)
        {
            _Node* nodePointer = (_Node*)pointer;
            return *nodePointer;
        }

        internal static readonly MmfWriteDelegate<_Node> _writeNodeDelegate = _WriteNode;
        private static void _WriteNode(byte* pointer, _Node node)
        {
            _Node* nodePointer = (_Node*)pointer;
            *nodePointer = node;
        }
    }
}
