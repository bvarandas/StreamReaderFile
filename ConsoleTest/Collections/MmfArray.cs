using System;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace ConsoleTest.Collections
{
    /// <summary>
	/// Array-like class that supports more than 2 billion items and uses
	/// temporary MemoryMappedFiles as its storage.
	/// </summary>
	[CLSCompliant(false)]
    public unsafe sealed class MmfArray<T> :
        BigArray<T>
    {
        private string _fileName;
        private MemoryMappedFile _file;
        private MemoryMappedViewAccessor _accessor;
        private byte* _bytes;
        private MmfReadDelegate<T> _readItem;
        private MmfWriteDelegate<T> _writeItem;
        private int _itemLength;

        /// <summary>
        /// Createsa new MmfArray.
        /// </summary>
        public MmfArray(long arrayLength, int itemLength, MmfReadDelegate<T> readItem, MmfWriteDelegate<T> writeItem)
        {
            if (arrayLength <= 0)
                throw new ArgumentOutOfRangeException("arrayLength");

            if (itemLength < 1)
                throw new ArgumentOutOfRangeException("itemLength");

            if (readItem == null)
                throw new ArgumentNullException("readItem");

            if (writeItem == null)
                throw new ArgumentNullException("writeItem");


            _fileName = Path.GetTempFileName();
            _file = MemoryMappedFile.CreateFromFile(_fileName, FileMode.Create, null, arrayLength * itemLength);

            _accessor = _file.CreateViewAccessor();
            _length = arrayLength;
            _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref _bytes);
            _itemLength = itemLength;
            _readItem = readItem;
            _writeItem = writeItem;
        }

        /// <summary>
        /// Disposes all used resources and deletes the temporary file.
        /// </summary>
        public override void Dispose()
        {
            if (_bytes != null)
                _accessor.SafeMemoryMappedViewHandle.ReleasePointer();

            Disposer.Dispose(ref _accessor);
            Disposer.Dispose(ref _file);

            if (_fileName != null)
                File.Delete(_fileName);
        }

        /// <summary>
        /// Gets a value indicating if this array was disposed.
        /// </summary>
        public override bool WasDisposed
        {
            get
            {
                return _file == null;
            }
        }

        /// <summary>
        /// Returns true.
        /// </summary>
        protected override bool UseParallelSort
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets an item by its index.
        /// </summary>
        public override T this[long index]
        {
            get
            {
                if (index < 0 || index >= _length)
                    throw new ArgumentOutOfRangeException("index");

                return _readItem(_bytes + (index * _itemLength));
            }
            set
            {
                if (index < 0 || index >= _length)
                    throw new ArgumentOutOfRangeException("index");

                _writeItem(_bytes + (index * _itemLength), value);
            }
        }

        /// <summary>
        /// Creates a new MmfArray of the given length that uses the same
        /// read/write delegates.
        /// </summary>
        public override BigArray<T> CreateNew(long length)
        {
            return new MmfArray<T>(length, _itemLength, _readItem, _writeItem);
        }

        /// <summary>
        /// Resizes this array.
        /// </summary>
        public override void Resize(long newLength)
        {
            MemoryMappedFile file;
            MemoryMappedViewAccessor accessor;
            byte* bytes = null;

            long minLength = Math.Min(_length, newLength) * _itemLength;
            string name = Path.GetTempFileName();
            file = MemoryMappedFile.CreateFromFile(name, FileMode.Create, null, newLength * _itemLength);
            accessor = file.CreateViewAccessor();
            bytes = null;
            accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref bytes);

            for (long i = 0; i < minLength; i++)
                bytes[i] = _bytes[i];

            // our dispose only closes the old memory mapped file.
            Dispose();

            _fileName = name;
            _file = file;
            _accessor = accessor;
            _bytes = bytes;
            _length = newLength;
        }
    }
}
