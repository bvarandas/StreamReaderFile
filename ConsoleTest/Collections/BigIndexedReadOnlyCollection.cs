using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleTest.Collections.Interfaces;

namespace ConsoleTest.Collections
{
    /// <summary>
    /// Class that wraps a big collection so it is exposed only as read-only.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class BigIndexedReadOnlyCollection<T> :
        IBigIndexedReadOnlyCollection<T>
    {
        private readonly IBigIndexedReadOnlyCollection<T> _mutableCollection;
        /// <summary>
        /// Creates a new wrapper over the given collection.
        /// </summary>
        public BigIndexedReadOnlyCollection(IBigIndexedCollection<T> mutableCollection)
        {
            if (mutableCollection == null)
                throw new ArgumentNullException("mutableCollection");

            _mutableCollection = mutableCollection;
        }

        /// <summary>
        /// Gets an item by its index.
        /// </summary>
        public T this[long index]
        {
            get
            {
                return _mutableCollection[index];
            }
        }

        /// <summary>
        /// Gets the number of items in this collection.
        /// </summary>
        public long Count
        {
            get
            {
                return _mutableCollection.Count;
            }
        }

        /// <summary>
        /// Enumerates all items in this collection.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            return _mutableCollection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _mutableCollection.GetEnumerator();
        }

        object IBigIndexedReadOnlyCollection.this[long index]
        {
            get
            {
                return _mutableCollection[index];
            }
        }
    }
}
