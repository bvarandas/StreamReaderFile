using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest.Collections
{
    /// <summary>
	/// Enumerator returned by the AddCollectionSlim when it is enumerated.
	/// </summary>
    public struct AddCollectionSlimEnumerator<T> :
        IEnumerator<T>
    {
        private readonly _AddCollectionNode<T> _firstNode;
        private readonly _AddCollectionNode<T> _lastNode;
        private readonly int _itemsInLastNode;

        private _AddCollectionNode<T> _node;
        private T[] _array;
        private int _position;
        private int _count;

        internal AddCollectionSlimEnumerator(_AddCollectionNode<T> firstNode, _AddCollectionNode<T> lastNode, int itemsInLastNode)
        {
            _firstNode = firstNode;
            _lastNode = lastNode;
            _itemsInLastNode = itemsInLastNode;
            _node = firstNode;

            _array = firstNode._array;
            if (firstNode == lastNode)
                _count = itemsInLastNode;
            else
                _count = _array.Length;

            _position = -1;
        }

        /// <summary>
        /// Gets the current item.
        /// </summary>
        public T Current
        {
            get
            {
                return _array[_position];
            }
        }

        /// <summary>
        /// Releases the resources used by this enumerator.
        /// </summary>
        public void Dispose()
        {
            _array = null;
        }

        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        /// <summary>
        /// Tries to advance to the next item in the collection.
        /// </summary>
        public bool MoveNext()
        {
            if (_array == null)
                return false;

            _position++;
            if (_position == _count)
            {
                _node = _node._nextNode;

                if (_node == null)
                {
                    _array = null;
                    return false;
                }

                _array = _node._array;
                _position = 0;
                if (_node == _lastNode)
                    _count = _itemsInLastNode;
                else
                    _count = _array.Length;
            }

            return true;
        }

        /// <summary>
        /// Resets the enumeration, so it will start from the beginning again.
        /// </summary>
        public void Reset()
        {
            if (_firstNode == null)
                return;

            _node = _firstNode;

            _array = _firstNode._array;
            if (_firstNode == _lastNode)
                _count = _itemsInLastNode;
            else
                _count = _array.Length;

            _position = -1;
        }
    }
}
