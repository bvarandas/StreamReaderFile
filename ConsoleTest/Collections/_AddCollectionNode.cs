using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest.Collections
{
    internal sealed class _AddCollectionNode<T>
    {
        internal readonly T[] _array;
        internal _AddCollectionNode<T> _nextNode;

        internal _AddCollectionNode(int size)
        {
            _array = new T[size];
        }
    }
}
