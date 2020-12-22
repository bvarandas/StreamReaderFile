using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest.Collections.Caching
{
    /// <summary>
	/// Create an instance of this object pointing to an IGarbageCollectionAware
	/// instance so it will get notified of garbage collections.
	/// Creating two instances pointing to the same object will cause two notifications
	/// to be sent to the object. Also, this invocation is done during the GC,
	/// while other threads may be paused, so doing locks may cause dead-locks.
	/// If you may need to register multiple times or simple want to be able
	/// to use locks, see the GCUtils.RegisterForGcNotification method.
	/// </summary>
	public sealed class GCNotifier :
        IDisposable
    {
        private GCHandle _handle;
        /// <summary>
        /// Creates a new notifier object that will notify collections
        /// to the given object (considering such given object is not
        /// itself collected, in which case this notifier will die too).
        /// </summary>
        public GCNotifier(IGarbageCollectionAware objectToNotify)
        {
            if (objectToNotify == null)
                throw new ArgumentNullException("objectToNotify");

            _handle = GCHandle.Alloc(objectToNotify, GCHandleType.Weak);
        }

        /// <summary>
        /// Notifies the target object of a collection if it is still
        /// alive and reregisters itself for a new finalize or, if
        /// the target object is already collected, releases the
        /// GCHandle.
        /// </summary>
        ~GCNotifier()
        {
            if (!_handle.IsAllocated)
                return;

            object target = _handle.Target;
            if (target == null)
            {
                _handle.Free();
                return;
            }

            // There is no need to try/catch here. If an exception is thrown, the
            // application is dead.
            IGarbageCollectionAware gcAware = (IGarbageCollectionAware)target;
            gcAware.OnCollected();

            GC.ReRegisterForFinalize(this);
        }

        /// <summary>
        /// Frees the GC handle and suppress the finalize of this object.
        /// </summary>
        public void Dispose()
        {
            var handle = _handle;
            if (handle.IsAllocated)
            {
                _handle = default(GCHandle);
                handle.Free();
            }

            GC.SuppressFinalize(this);
        }
    }
}
