using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest.Collections.Interfaces
{
    
    public interface IBigIndexedCollection :
        IBigIndexedReadOnlyCollection,
        IAdvancedDisposable
    {
        new object this[long index] { get; set; }

        IBigIndexedReadOnlyCollection AsReadOnly();
    }

    
    public interface IBigIndexedCollection<T> :
        IBigIndexedReadOnlyCollection<T>,
        IBigIndexedCollection
    {
        new T this[long index] { get; set; }

        new IBigIndexedReadOnlyCollection<T> AsReadOnly();
    }
}
