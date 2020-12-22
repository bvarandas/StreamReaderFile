using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest.Collections.Caching
{
    /// <summary>
	/// Interface that must be implemented by objects that want to
	/// register to the Collected notification.
	/// </summary>
    public interface IGarbageCollectionAware
    {
        /// <summary>
		/// Method invoked when a collection occurs.
		/// </summary>
		void OnCollected();
    }
}
