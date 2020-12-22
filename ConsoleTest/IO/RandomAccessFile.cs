using ConsoleTest.Collections;
using ConsoleTest.Collections.Caching;
using ConsoleTest.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using ConsoleTest.Extensions;

namespace ConsoleTest.IO
{
    /// <summary>
	/// A class optimized to read and write files at "random" positions 
	/// (ie, not sequential reads).
	/// </summary>
	public sealed class RandomAccessFile :
        ThreadSafeDisposable,
        IGarbageCollectionAware
    {
        private static int _defaultBlockSize = 4096;
        /// <summary>
        /// Gets or sets the default block size used by new random access
        /// files when such parameter is not specified. The initial value
        /// is 4096 (4kb).
        /// </summary>
        public static int DefaultBlockSize
        {
            get
            {
                return _defaultBlockSize;
            }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException("value");

                _defaultBlockSize = value;
            }
        }

        private static int _guaranteedMemoryLimit = 1024 * 1024 * 1024;
        /// <summary>
        /// Gets or sets how much memory the application can consume before
        /// the buffered blocks start to be collected. The default
        /// value is 1gb.
        /// Note that a single value affects all the RandomAccessFiles.
        /// That is, by default the application can have 1gb before 
        /// RandomAccessFiles' blocks are collected, it is not important
        /// why it is using 1gb.
        /// </summary>
        public static int GuaranteedMemoryLimit
        {
            get
            {
                return _guaranteedMemoryLimit;
            }
            set
            {
                if (value < 50 * 1024 * 1024)
                    throw new ArgumentOutOfRangeException("The minimum limit is 50 mb.");

                _guaranteedMemoryLimit = value;
            }
        }

        private Dictionary<long, GCHandle> _cachedBlocks = new Dictionary<long, GCHandle>();
        private HashSet<_RandomAccessFileBlock> _keepAlive = new HashSet<_RandomAccessFileBlock>();
        private HashSet<_RandomAccessFileBlock> _keepAlive2;
        private HashSet<_RandomAccessFileBlock> _modified = new HashSet<_RandomAccessFileBlock>();
        private WeakReference _gcNotifier;
        private Stream _file;
        private long _length;
        private readonly int _blockSize;
        /// <summary>
        /// Creates a new random access file for reading.
        /// </summary>
        public RandomAccessFile(string name) :
            this(name, _defaultBlockSize, FileMode.Open, FileAccess.Read, FileShare.Read)
        {
        }

        /// <summary>
        /// Creates a new random access file for reading, but using the
        /// specified blockSize.
        /// </summary>
        public RandomAccessFile(string name, int blockSize) :
            this(name, blockSize, FileMode.Open, FileAccess.Read, FileShare.Read)
        {
        }

        /// <summary>
        /// Creates a new RandomAccessFile.
        /// </summary>
        public RandomAccessFile(string name, FileMode mode, FileAccess access, FileShare share) :
            this(name, _defaultBlockSize, mode, access, share)
        {
        }

        /// <summary>
        /// Creates a new RandomAccessFile.
        /// </summary>
        public RandomAccessFile(string name, int blockSize, FileMode mode, FileAccess access, FileShare share)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            if (blockSize < 1)
                throw new ArgumentOutOfRangeException("blockSize");

            try
            {
                _gcNotifier = new WeakReference(new GCNotifier(this), true);
                _file = new FileStream(name, mode, access, share, 1, FileOptions.RandomAccess);
                _blockSize = blockSize;
                _length = _file.Length;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        /// <summary>
        /// Releases all resources used by this file.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                var notifier = _gcNotifier;
                if (notifier != null)
                {
                    _gcNotifier = null;
                    var realNotifier = (GCNotifier)notifier.Target;
                    if (realNotifier != null)
                        realNotifier.Dispose();
                }

                if (_mustFlushOnDispose)
                {
                    _collectionRequested = 0;
                    _Flush();
                }

                Disposer.Dispose(ref _file);

                _keepAlive = null;
                _keepAlive2 = null;
            }

            var cachedBlocks = _cachedBlocks;
            if (cachedBlocks != null)
            {
                _cachedBlocks = null;

                foreach (var handle in cachedBlocks.Values)
                    handle.Free();
            }

            base.Dispose(disposing);
        }

        private bool _mustFlushOnDispose = true;
        /// <summary>
        /// Gets or sets a value indicating if a Flush should
        /// be done on dispose. This only affects manual disposes.
        /// </summary>
        public bool MustFlushOnDispose
        {
            get
            {
                return _mustFlushOnDispose;
            }
            set
            {
                _mustFlushOnDispose = value;
            }
        }

        /// <summary>
        /// Gets or sets a single byte by its index.
        /// </summary>
        public byte this[long index]
        {
            get
            {
                if (index < 0 || index >= _length)
                    throw new ArgumentOutOfRangeException("index");

                lock (DisposeLock)
                {
                    _CheckUndisposedAndFlushRequest();

                    int positionInBlock = (int)(index % _blockSize);
                    long blockIndex = index / _blockSize;

                    _RandomAccessFileBlock bytes = _GetBlock(blockIndex);
                    _keepAlive.Add(bytes);
                    return bytes._block[positionInBlock];
                }
            }
            set
            {
                if (index < 0)
                    throw new ArgumentOutOfRangeException("index");

                lock (DisposeLock)
                {
                    _CheckUndisposedAndFlushRequest();

                    if (index >= _length)
                    {
                        _length = index + 1;
                        //SetLength(index+1);
                    }

                    int positionInBlock = (int)(index % _blockSize);
                    long blockIndex = index / _blockSize;

                    _RandomAccessFileBlock bytes = _GetBlock(blockIndex);
                    bytes._block[positionInBlock] = value;
                    _modified.Add(bytes);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating if the file can be read.
        /// In fact, you should always use a read supporting mode, but
        /// the value can be false if the file was disposed.
        /// </summary>
        public bool CanRead
        {
            get
            {
                var file = _file;
                if (file == null)
                    return false;

                return file.CanRead;
            }
        }

        /// <summary>
        /// Gets a value indicating if the file can be written.
        /// </summary>
        public bool CanWrite
        {
            get
            {
                var file = _file;
                if (file == null)
                    return false;

                return file.CanWrite;
            }
        }

        /// <summary>
        /// Gets the length of this file.
        /// </summary>
        public long Length
        {
            get
            {
                return _length;
            }
        }

        /// <summary>
        /// Sets the length of this file.
        /// </summary>
        public void SetLength(long value)
        {
            lock (DisposeLock)
            {
                _CheckUndisposedAndFlushRequest();

                if (value == _length)
                    return;

                _file.SetLength(value);
                _length = value;
            }
        }

        /// <summary>
        /// Reads up to count bytes from the file. It is possible that a smaller value
        /// is returned if you achieve the end of a block (not necessarely the end of the file).
        /// A return of 0 really means the end of the file.
        /// </summary>
        public int PartialRead(byte[] buffer, int positionInBuffer, long positionInFile, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            if (positionInBuffer < 0 || positionInBuffer >= buffer.Length)
                throw new ArgumentException("positionInBuffer");

            if (positionInFile < 0)
                throw new ArgumentOutOfRangeException("positionInFile");

            if (count < 1 || positionInBuffer + count > buffer.Length)
                throw new ArgumentOutOfRangeException("count");

            lock (DisposeLock)
            {
                _CheckUndisposedAndFlushRequest();

                return _ReadInsideLock(buffer, positionInBuffer, positionInFile, count);
            }
        }

        /// <summary>
        /// Reads exactly count bytes and puts it into the buffer. If there
        /// is no such amount of bytes in the file, throws an IOException.
        /// </summary>
        public void FullRead(byte[] buffer, int positionInBuffer, long positionInFile, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            if (positionInBuffer < 0 || positionInBuffer >= buffer.Length)
                throw new ArgumentException("positionInBuffer");

            if (positionInFile < 0)
                throw new ArgumentOutOfRangeException("positionInFile");

            if (count < 1 || positionInBuffer + count > buffer.Length)
                throw new ArgumentOutOfRangeException("count");

            lock (DisposeLock)
            {
                _CheckUndisposedAndFlushRequest();

                if (positionInFile + count > _length)
                    throw new IOException("Attempted to read after the end of file.");

                while (count > 0)
                {
                    int read = _ReadInsideLock(buffer, positionInBuffer, positionInFile, count);
                    if (read <= 0)
                        throw new IOException("Attempted to read after the end of file.");

                    count -= read;
                    positionInFile += read;
                    positionInBuffer += read;
                }
            }
        }

        private int _ReadInsideLock(byte[] buffer, int positionInBuffer, long positionInFile, int count)
        {
            if (positionInFile >= _length)
                return 0;

            int positionInBlock = (int)(positionInFile % _blockSize);
            long blockIndex = positionInFile / _blockSize;
            long lastBlockIndex = _length / _blockSize;
            int remaining;
            if (blockIndex != lastBlockIndex)
                remaining = _blockSize - positionInBlock;
            else
            {
                remaining = (int)(_length - positionInFile);
                if (count > remaining)
                    count = remaining;
            }

            if (count > remaining)
                count = remaining;

            _RandomAccessFileBlock bytes = _GetBlock(blockIndex);

            _keepAlive.Add(bytes);

            Buffer.BlockCopy(bytes._block, positionInBlock, buffer, positionInBuffer, count);
            return count;
        }

        /// <summary>
        /// Tries to write count bytes to the buffer. It is possible that less
        /// bytes are written by achieving the end of a block.
        /// </summary>
        public int PartialWrite(byte[] buffer, int positionInBuffer, long positionInFile, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            if (positionInBuffer < 0 || positionInBuffer >= buffer.Length)
                throw new ArgumentException("positionInBuffer");

            if (positionInFile < 0)
                throw new ArgumentOutOfRangeException("positionInFile");

            if (count < 1 || positionInBuffer + count > buffer.Length)
                throw new ArgumentOutOfRangeException("count");

            lock (DisposeLock)
            {
                _CheckUndisposedAndFlushRequest();

                if (!_file.CanWrite)
                    throw new InvalidOperationException("This file is not opened on a write-supporting mode.");

                return _WriteInsideLock(buffer, positionInBuffer, positionInFile, count);
            }
        }

        /// <summary>
        /// Writes count bytes to the file or throws an IOException.
        /// </summary>
        public void FullWrite(byte[] buffer, int positionInBuffer, long positionInFile, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            if (positionInBuffer < 0 || positionInBuffer >= buffer.Length)
                throw new ArgumentException("positionInBuffer");

            if (positionInFile < 0)
                throw new ArgumentOutOfRangeException("positionInFile");

            if (count < 1 || positionInBuffer + count > buffer.Length)
                throw new ArgumentOutOfRangeException("count");

            lock (DisposeLock)
            {
                _CheckUndisposedAndFlushRequest();

                while (count > 0)
                {
                    int wrote = _WriteInsideLock(buffer, positionInBuffer, positionInFile, count);
                    if (wrote <= 0)
                        throw new IOException("Error during write.");

                    count -= wrote;
                    positionInFile += wrote;
                    positionInBuffer += wrote;
                }
            }
        }
        private int _WriteInsideLock(byte[] buffer, int positionInBuffer, long positionInFile, int count)
        {
            long blockIndex = positionInFile / _blockSize;

            int positionInBlock = (int)(positionInFile % _blockSize);
            int remaining = _blockSize - positionInBlock;
            if (count > remaining)
                count = remaining;

            long total = positionInFile + count;
            if (total > _length)
            {
                //_file.SetLength(total);
                _length = total;
            }

            _RandomAccessFileBlock bytes = _GetBlock(blockIndex);
            _modified.Add(bytes);

            Buffer.BlockCopy(buffer, positionInBuffer, bytes._block, positionInBlock, count);

            return count;
        }

        private _RandomAccessFileBlock _GetBlock(long blockIndex)
        {
            GCHandle handle;
            if (_cachedBlocks.TryGetValue(blockIndex, out handle))
            {
                object target = handle.Target;
                if (target != null)
                    return (_RandomAccessFileBlock)target;

                var bytes = _ReadFromFile(blockIndex);
                handle.Target = bytes;
                return bytes;
            }

            var bytes2 = _ReadFromFile(blockIndex);
            handle = GCHandle.Alloc(bytes2, GCHandleType.Weak);
            try
            {
                _cachedBlocks.Add(blockIndex, handle);
            }
            catch
            {
                handle.Free();
                throw;
            }

            return bytes2;
        }
        private _RandomAccessFileBlock _ReadFromFile(long blockIndex)
        {
            long position = blockIndex * _blockSize;
            long toRead = _file.Length - position;
            if (toRead > _blockSize)
                toRead = _blockSize;

            var result = new byte[_blockSize];
            if (toRead > 0)
            {
                _file.Position = position;
                _file.FullRead(result, 0, (int)toRead);
            }

            return new _RandomAccessFileBlock(position, result);
        }

        private void _CheckUndisposedAndFlushRequest()
        {
            CheckUndisposed();

            if (_collectionRequested == 1)
                _Flush();
        }

        /// <summary>
        /// Saves all cached data to the file.
        /// </summary>
        public void Flush()
        {
            lock (DisposeLock)
            {
                CheckUndisposed();

                _Flush();
            }
        }

        private int _collectionRequested;
        private void _Flush()
        {
            var modified = _modified;
            if (modified == null)
                return;

            var file = _file;
            if (file == null)
                return;

            try
            {
                file.SetLength(_length);
                if (modified.Count > 0)
                {
                    long lastBlockPosition = _length - (_length % _blockSize);
                    _modified = new HashSet<_RandomAccessFileBlock>();
                    foreach (var block in modified)
                    {
                        int count = _blockSize;
                        if (block._position > lastBlockPosition)
                            continue;

                        if (block._position == lastBlockPosition)
                            count = (int)(_length % _blockSize);

                        file.Position = block._position;
                        file.Write(block._block, 0, count);
                    }
                }

                if (_collectionRequested == 1)
                {
                    var addCollection = new AddCollectionSlim<GCHandle>();
                    var oldCachedBlocks = _cachedBlocks;
                    try
                    {
                        _keepAlive2 = _keepAlive;
                        _keepAlive = new HashSet<_RandomAccessFileBlock>();
                        var newCachedBlocks = new Dictionary<long, GCHandle>(oldCachedBlocks.Count);
                        foreach (var pair in oldCachedBlocks)
                        {
                            var handle = pair.Value;
                            if (handle.Target != null)
                                newCachedBlocks.Add(pair.Key, handle);
                            else
                                addCollection.Add(handle);
                        }

                        _cachedBlocks = newCachedBlocks;
                    }
                    catch (OutOfMemoryException)
                    {
                    }

                    if (_cachedBlocks != oldCachedBlocks)
                        foreach (var handle in addCollection)
                            handle.Free();
                }
            }
            finally
            {
                _collectionRequested = 0;
            }
        }

        void IGarbageCollectionAware.OnCollected()
        {
            if (GC.GetTotalMemory(false) <= _guaranteedMemoryLimit)
                return;

            // If we are the first to mark the collection as requested we
            // continue.
            if (Interlocked.Exchange(ref _collectionRequested, 1) == 1)
                return;

            bool locked = false;
            try
            {
                Monitor.TryEnter(DisposeLock, 1000, ref locked);

                // if we don't get the lock, there is no problem, some next read, write
                // or even a new collection later will be able to do the flush.
                if (locked)
                {
                    // if we got the lock, we check again... it is possible that
                    // since we marked a flush request until we got the lock someone
                    // already did the flush.
                    if (_collectionRequested == 1)
                        _Flush();
                }
            }
            finally
            {
                if (locked)
                    Monitor.Exit(DisposeLock);
            }
        }
    }
}
