using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest.Collections
{
    /// <summary>
    /// Static BigArray configuration class.
    /// </summary>
    public static class InMemoryBigArray
    {
        private static int _defaultBlockLength = 10 * 1024 * 1024;
        /// <summary>
        /// Gets or sets the default BlockLength used by new BigArray
        /// instances.
        /// </summary>
        public static int DefaultBlockLength
        {
            get
            {
                return _defaultBlockLength;
            }
            set
            {
                if (value < 1024)
                    throw new ArgumentOutOfRangeException("value", "Minimum DefaultBlockLength value is 1024kb (1024*1024).");

                _defaultBlockLength = value;
            }
        }
    }

    /// <summary>
    /// This class represents an array that can have more than 2 billion items,
    /// considering there is enough memory.
    /// </summary>
    public sealed class InMemoryBigArray<T> :
        BigArray<T>
    {
        private static readonly T[][] _emptyArray = new T[0][];

        private readonly int _blockLength;
        private readonly T _defaultValue;
        private T[][] _firstArray;
        private int _lastBlockLength;

        /// <summary>
        /// Creates a new bigarray of the given length.
        /// </summary>
        /// <param name="length"></param>
        public InMemoryBigArray(long length) :
            this(length, default(T))
        {
        }

        /// <summary>
        /// Creates a new bigarray of the given length, also setting all the
        /// uninitialized values to the given defaultValue.
        /// </summary>
        public InMemoryBigArray(long length, T defaultValue) :
            this(length, defaultValue, InMemoryBigArray.DefaultBlockLength)
        {
        }

        /// <summary>
        /// Creates a new bigarray of the given length, setting the
        /// default value and also specifying the size of each block that
        /// will be allocated.
        /// </summary>
        public InMemoryBigArray(long length, T defaultValue, int blockLength)
        {
            if (blockLength < 1024 * 1024)
                throw new ArgumentOutOfRangeException("blockSize");

            _defaultValue = defaultValue;
            _blockLength = blockLength;
            _length = length;

            if (length == 0)
            {
                _firstArray = _emptyArray;
                return;
            }

            int numBlocks;
            int lastBlockSize;

            checked
            {
                numBlocks = (int)(length / blockLength);
                lastBlockSize = (int)(length % blockLength);

                if (lastBlockSize > 0)
                    numBlocks++;
                else
                    lastBlockSize = blockLength;
            }

            _lastBlockLength = lastBlockSize;
            _firstArray = new T[numBlocks][];
        }

        /// <summary>
        /// Immediately allows all the inner arrays to be reclaimed by
        /// the garbage collector.
        /// </summary>
        public override void Dispose()
        {
            _firstArray = null;
        }

        /// <summary>
        /// Gets a value indicating if this array was already disposed.
        /// </summary>
        public override bool WasDisposed
        {
            get
            {
                return _firstArray == null;
            }
        }

        /// <summary>
        /// Gets the size of each block used by this array.
        /// </summary>
        public int BlockLength
        {
            get
            {
                return _blockLength;
            }
        }

        /// <summary>
        /// Returns true. This class has big benefits using parallel sort.
        /// </summary>
        protected override bool UseParallelSort
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets or sets an item by index.
        /// </summary>
        public override T this[long index]
        {
            get
            {
                var block = _firstArray[index / _blockLength];
                if (block == null)
                    return _defaultValue;

                return block[index % _blockLength];
            }
            set
            {
                long blockIndex = index / _blockLength;

                var block = _firstArray[blockIndex];
                if (block == null)
                {
                    if (_comparer.Equals(value, _defaultValue))
                        return;

                    if (blockIndex == _firstArray.Length - 1)
                        block = new T[_lastBlockLength];
                    else
                        block = new T[_blockLength];

                    if (!_comparer.Equals(default(T), _defaultValue))
                        for (int i = 0; i < block.Length; i++)
                            block[i] = _defaultValue;

                    _firstArray[blockIndex] = block;
                }

                block[index % _blockLength] = value;
            }
        }

        /// <summary>
        /// Creates a new InMemoryBigArray of the given length, using the
        /// same default values as this one.
        /// </summary>
        public override BigArray<T> CreateNew(long length)
        {
            return new InMemoryBigArray<T>(length, _defaultValue, _blockLength);
        }

        /// <summary>
        /// Gets the index of an item in this array, or returns -1
        /// if it does not exist.
        /// </summary>
        public override long IndexOf(T item, long startIndex, long count)
        {
            if (startIndex < 0 || startIndex > _length)
                throw new ArgumentOutOfRangeException("startIndex");

            if (count == 0)
                return -1;

            long end = startIndex + count;
            if (count < 0 || end > _length)
                throw new ArgumentOutOfRangeException("count");

            long blockIndex = startIndex / _blockLength;
            long positionInBlock = startIndex % _blockLength;
            T[] block = _firstArray[blockIndex];
            long position = startIndex;

            while (true)
            {
                if (block == null)
                {
                    position += _blockLength - positionInBlock;

                    if (position >= end)
                        return -1;

                    blockIndex++;
                    positionInBlock = 0;
                    block = _firstArray[blockIndex];
                    continue;
                }

                if (_comparer.Equals(block[positionInBlock], item))
                    return position;

                position++;

                if (position >= end)
                    return -1;

                if (positionInBlock > block.Length)
                {
                    blockIndex++;
                    positionInBlock = 0;
                    block = _firstArray[blockIndex];
                }
            }
        }

        /// <summary>
        /// Resizes this array.
        /// </summary>
        public override void Resize(long newLength)
        {
            if (newLength < 0)
                throw new ArgumentOutOfRangeException("newLength", "newLength can't be negative.");

            if (newLength == 0)
            {
                _firstArray = _emptyArray;
                _lastBlockLength = 0;
                _length = 0;
                return;
            }

            int numBlocks;
            int lastBlockSize;
            checked
            {
                numBlocks = (int)(newLength / _blockLength);
                lastBlockSize = (int)(newLength % _blockLength);

                if (lastBlockSize > 0)
                    numBlocks++;
                else
                    lastBlockSize = _blockLength;
            }

            _lastBlockLength = lastBlockSize;

            T[][] newArray = _firstArray;
            if (numBlocks != _firstArray.Length)
            {
                int minBlocks = Math.Min(numBlocks, _firstArray.Length);
                newArray = new T[numBlocks][];
                for (int i = 0; i < minBlocks; i++)
                    newArray[i] = _firstArray[i];
            }

            var block = newArray[numBlocks - 1];
            if (block != null)
            {
                int oldLength = block.Length;
                Array.Resize(ref block, _lastBlockLength);
                newArray[numBlocks - 1] = block;

                if (!_comparer.Equals(default(T), _defaultValue))
                    for (int i = oldLength; i < _lastBlockLength; i++)
                        block[i] = _defaultValue;
            }

            if (_firstArray.Length > 0 && newLength > _length && newArray.Length != _firstArray.Length)
            {
                int oldLastBlockIndex = _firstArray.Length - 1;
                if (newArray[oldLastBlockIndex] != null)
                    Array.Resize(ref newArray[oldLastBlockIndex], _blockLength);
            }

            _firstArray = newArray;
            _length = newLength;
        }

        /// <summary>
        /// Enumerates all items in this array.
        /// </summary>
        public override IEnumerator<T> GetEnumerator()
        {
            int lastBlockIndex = _firstArray.Length - 1;
            for (int i = 0; i <= lastBlockIndex; i++)
            {
                var block = _firstArray[i];

                if (block == null)
                {
                    int numberOfFakes;
                    if (i == lastBlockIndex)
                        numberOfFakes = _lastBlockLength;
                    else
                        numberOfFakes = _blockLength;

                    for (int j = 0; j < numberOfFakes; j++)
                        yield return _defaultValue;
                }
                else
                {
                    foreach (var value in block)
                        yield return value;
                }
            }
        }
    }
}
