using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest.Collections.Interfaces
{
	public interface IAdvancedDisposable : IDisposable
    {
        bool WasDisposed { get; }
    }
}
