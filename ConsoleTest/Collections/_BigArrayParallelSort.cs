using System;
using System.Collections.Generic;
using System.Threading;

namespace ConsoleTest.Collections
{
    internal sealed class _BigArrayParallelSort
    {
        internal static int _parallelSortCount;
        internal int _executingCount;
        internal List<Exception> _exceptions;

        internal void Wait()
        {
            lock (this)
                while (_executingCount > 0)
                    Monitor.Wait(this);

            if (_exceptions != null)
                throw new AggregateException(_exceptions);
        }
    }
}
