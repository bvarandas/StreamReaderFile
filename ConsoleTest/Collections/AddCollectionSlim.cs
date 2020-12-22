using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ConsoleTest.Collections
{
    /// <summary>
    /// Very slim collection that allows items to be added and has a ToArray() method.
    /// It is much faster than adding items to a list to then call ToArray().
    /// All adds are O(1) as there are never reallocations with copies. If needed, 
    /// a new node is allocated as the last node, and adds always go to the last node.
    /// Note: This is a struct, so if you need to pass it as parameter, pass it as a ref
	/// parameter.
    /// </summary>
    public struct AddCollectionSlim<T> :
        IEnumerable<T>,
        IList<T>
    {
        internal int _count;
        internal int _capacity;
        internal int _positionInNode;
        internal _AddCollectionNode<T> _firstNode;
        internal _AddCollectionNode<T> _lastNode;
        internal T[] _array;

        /// <summary>
        /// Gets the number of items supported before allocating a new node.
        /// But, even when new nodes are allocated, there is no copy.
        /// </summary>
        public int Capacity
        {
            get
            {
                return _capacity;
            }
        }

        /// <summary>
        /// Gets the number of items in this buffer.
        /// </summary>
        public int Count
        {
            get
            {
                return _count;
            }
        }

        /// <summary>
        /// Adds a new item to this collection.
        /// </summary>
        public void Add(T item)
        {
            if (_count == _capacity)
            {
                if (_lastNode != null)
                {
                    var newNode = new _AddCollectionNode<T>(_capacity);
                    _capacity += _capacity;
                    _lastNode._nextNode = newNode;
                    _lastNode = newNode;
                }
                else
                {
                    _firstNode = new _AddCollectionNode<T>(32);
                    _lastNode = _firstNode;
                    _capacity = 32;
                }

                _array = _lastNode._array;
                _positionInNode = 0;
            }

            _array[_positionInNode] = item;
            _count++;
            _positionInNode++;
        }

        /// <summary>
        /// Returns a copy of this collection as an array.
        /// </summary>
        public T[] ToArray()
        {
            if (_count == 0)
                return EmptyArray<T>.Instance;

            if (_count == _firstNode._array.Length)
                return _firstNode._array;

            int positionInResult = 0;
            T[] result = new T[_count];
            var node = _firstNode;
            while (node != null)
            {
                var array = node._array;

                if (node != _lastNode || _positionInNode == array.Length)
                {
                    array.CopyTo(result, positionInResult);
                    positionInResult += array.Length;
                }
                else
                {
                    for (int positionInArray = 0; positionInArray < _positionInNode; positionInArray++)
                    {
                        result[positionInResult] = array[positionInArray];
                        positionInResult++;
                    }
                }

                node = node._nextNode;
            }
            return result;
        }

        /// <summary>
        /// Gets an item by its index. Note that this is not an O(1) operation, as
        /// bigger indexes may require to navigate through many nodes. If you need to iterate
        /// through many items or really access them many times at random positions, it
        /// is possible that calling ToArray() and then doing all the gets from the
        /// array will be faster. This method is here for completeness and because
        /// it replaces the LINQ version. If this type didn't implement the ICollection
        /// interface, calling the LINQ ElementAt() would be an O(index) operation.
        /// </summary>
        public T ElementAt(int index)
        {
            if (index < 0 || index >= _count)
                throw new ArgumentOutOfRangeException("index");

            var node = _firstNode;
            while (true)
            {
                var array = node._array;
                int nodeSize = array.Length;

                if (index < nodeSize)
                    return array[index];

                index -= nodeSize;
                node = node._nextNode;
            }
        }

        /// <summary>
        /// Sets an item by its index. This is not an O(1) operation as the bigger the
        /// index, the more nodes must be navigated. This method is here for completeness
        /// but if you really need to set many items by position you will probably
        /// have a better performance using lists.
        /// </summary>
        public void SetElementAt(int index, T item)
        {
            if (index < 0 || index >= _count)
                throw new ArgumentOutOfRangeException("index");

            var node = _firstNode;
            while (true)
            {
                var array = node._array;
                int nodeSize = array.Length;

                if (index < nodeSize)
                {
                    array[index] = item;
                    return;
                }

                index -= nodeSize;
                node = node._nextNode;
            }
        }

        /// <summary>
        /// Returns a read-only copy of this collection that can be accessed through
        /// its indexes.
        /// </summary>
        public ReadOnlyCollection<T> ToReadOnlyCollection()
        {
            var array = ToArray();
            var result = new ReadOnlyCollection<T>(array);
            return result;
        }

        /// <summary>
        /// Gets the index of an item in this collection, or -1
        /// if the item does not exist.
        /// </summary>
        public int IndexOf(T item)
        {
            if (_count == 0)
                return -1;

            int result = 0;
            var node = _firstNode;
            while (node != null)
            {
                var array = node._array;

                int count;
                if (node != _lastNode)
                    count = array.Length;
                else
                    count = _positionInNode;

                for (int i = 0; i < count; i++)
                    if (EqualityComparer<T>.Default.Equals(item, array[i]))
                        return result + i;

                result += count;
                node = node._nextNode;
            }
            return result;
        }

        /// <summary>
        /// Enumerates all items in this buffer.
        /// </summary>
        public AddCollectionSlimEnumerator<T> GetEnumerator()
        {
            if (_count == 0)
                return new AddCollectionSlimEnumerator<T>();

            return new AddCollectionSlimEnumerator<T>(_firstNode, _lastNode, _positionInNode);
        }

        /// <summary>
        /// Clears this collection.
        /// </summary>
        public void Clear()
        {
            _count = 0;
            _capacity = 0;
            _positionInNode = 0;
            _firstNode = null;
            _lastNode = null;
            _array = null;
        }

        /// <summary>
        /// Checks if a given item exists in this collection.
        /// </summary>
        public bool Contains(T item)
        {
            return IndexOf(item) != -1;
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            foreach (var item in this)
            {
                array[arrayIndex] = item;
                arrayIndex++;
            }
        }

        bool ICollection<T>.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        private NotSupportedException _NotSupported()
        {
            return
                new NotSupportedException
                (
                    "When accessing the AddCollectionSlim through an interface you have a partial " +
                    "copy of the object, so many modifying methods are not supported as this can " +
                    "cause problems to the original instance. The IList<T> and ICollection<T> are " +
                    "implemented only to make this object acessible to LINQ methods or to let this " +
                    "object be passed as parameter to methods that consume such interfaces " +
                    "as read-only."
                );
        }
        void ICollection<T>.Clear()
        {
            throw _NotSupported();
        }

        void ICollection<T>.Add(T item)
        {
            throw _NotSupported();
        }

        bool ICollection<T>.Remove(T item)
        {
            throw _NotSupported();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }


        void IList<T>.Insert(int index, T item)
        {
            throw _NotSupported();
        }

        void IList<T>.RemoveAt(int index)
        {
            throw _NotSupported();
        }

        // We implemented IList only to support the indexer.
        // If we end-up using LINQ over this object, it is much faster thanks to it.
        // Different from the Add method, we implemented the setter here because even if we 
        // have a boxed copy of the struct we will only affect a node content (which is 
        // already a reference) and we will not change the count of this collection or 
        // allocate a new lastnode.
        T IList<T>.this[int index]
        {
            get
            {
                return ElementAt(index);
            }
            set
            {
                SetElementAt(index, value);
            }
        }
    }
}
