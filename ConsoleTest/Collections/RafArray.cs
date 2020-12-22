using System;
using System.IO;
using ConsoleTest.IO;

namespace ConsoleTest.Collections
{
    /// <summary>
	/// Array that supports more than two billion items and uses a 
	/// temporary RandomAccessFile as its storage.
	/// </summary>
	public sealed class RafArray<T> :
        BigArray<T>
    {
        private string _fileName;
        private RandomAccessFile _file;
        private byte[] _itemBytes;
        private readonly RafReadDelegate<T> _readItem;
        private readonly RafWriteDelegate<T> _writeItem;
        private long _fileLength;

        /// <summary>
        /// Creates a new array instance.
        /// </summary>
        public RafArray(long arrayLength, int itemLength, RafReadDelegate<T> readItem, RafWriteDelegate<T> writeItem)
        {
            if (arrayLength <= 0)
                throw new ArgumentOutOfRangeException("arrayLength");

            if (itemLength < 1)
                throw new ArgumentOutOfRangeException("itemLength");

            if (readItem == null)
                throw new ArgumentNullException("readItem");

            if (writeItem == null)
                throw new ArgumentNullException("writeItem");

            _itemBytes = new byte[itemLength];
            _fileName = Path.GetTempFileName();
            _file = new RandomAccessFile(_fileName, RandomAccessFile.DefaultBlockSize, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            _file.MustFlushOnDispose = false;
            _length = arrayLength;
            _fileLength = arrayLength * itemLength;
            _file.SetLength(_fileLength);
            _readItem = readItem;
            _writeItem = writeItem;
        }

        /// <summary>
        /// Deletes the temporary file used by this array.
        /// </summary>
        public override void Dispose()
        {
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
        /// Always returns false.
        /// </summary>
        protected override bool UseParallelSort
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets or sets an item by its index.
        /// </summary>
        public override T this[long index]
        {
            get
            {
                if (index < 0 || index >= _fileLength)
                    throw new ArgumentOutOfRangeException("index");

                _file.FullRead(_itemBytes, 0, index * _itemBytes.Length, _itemBytes.Length);
                return _readItem(_itemBytes);
            }
            set
            {
                if (index < 0 || index >= _fileLength)
                    throw new ArgumentOutOfRangeException("index");

                _writeItem(_itemBytes, value);
                _file.FullWrite(_itemBytes, 0, index * _itemBytes.Length, _itemBytes.Length);
            }
        }

        /// <summary>
        /// Resizes this array.
        /// </summary>
        public override void Resize(long newLength)
        {
            _fileLength = newLength * _itemBytes.Length;
            _length = newLength;
            _file.SetLength(_fileLength);
        }

        /// <summary>
        /// Creates a new array of the given length that uses the same
        /// read/write delegates.
        /// </summary>
        public override BigArray<T> CreateNew(long length)
        {
            return new RafArray<T>(length, _itemBytes.Length, _readItem, _writeItem);
        }
    }
}
