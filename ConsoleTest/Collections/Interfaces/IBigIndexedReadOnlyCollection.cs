using System;
using System.Collections;
using System.Collections.Generic;

namespace ConsoleTest.Collections.Interfaces
{
    public interface IBigIndexedReadOnlyCollection : IEnumerable
    {
        long Count { get; }

        object this[long index] { get; }
    }

    public interface IBigIndexedReadOnlyCollection<T> : IEnumerable<T>, IBigIndexedReadOnlyCollection
    {
        new T this[long index] { get; }
    }
}
