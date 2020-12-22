using System;
using System.IO;
using System.Text;

namespace ConsoleTest.Extensions
{
    /// <summary>
	/// Adds overloads to the stream Read method and adds the FullRead method,
	/// which will continue to read until it reads everything that was requested,
	/// or throws an IOException.
	/// </summary>
	public static class IndexerStreamExtensions
    {
        /// <summary>
        /// Calls read using the full given buffer.
        /// </summary>
        public static int Read(this Stream stream, byte[] buffer)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (buffer == null)
                throw new ArgumentNullException("buffer");

            return stream.Read(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Calls read using the given buffer and the initialIndex.
        /// </summary>
        public static int Read(this Stream stream, byte[] buffer, int initialIndex)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (buffer == null)
                throw new ArgumentNullException("buffer");

            return stream.Read(buffer, initialIndex, buffer.Length - initialIndex);
        }

        /// <summary>
        /// Writes all the bytes in the given buffer.
        /// </summary>
        public static void Write(this Stream stream, byte[] buffer)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (buffer == null)
                throw new ArgumentNullException("buffer");

            stream.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Writes the bytes from the given buffer, beginning at the given beginIndex.
        /// </summary>
        public static void Write(this Stream stream, byte[] buffer, int initialIndex)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (buffer == null)
                throw new ArgumentNullException("buffer");

            stream.Write(buffer, initialIndex, buffer.Length - initialIndex);
        }

        /// <summary>
        /// Will read the given buffer to the end.
        /// Throws an exception if it's not possible to read the full buffer.
        /// </summary>
        public static void FullRead(this Stream stream, byte[] buffer)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (buffer == null)
                throw new ArgumentNullException("buffer");

            stream.FullRead(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Full reads the stream over the given buffer, but only at the given
        /// initialIndex. If the requested length can't be read, throws an 
        /// IOException.
        /// </summary>
        public static void FullRead(this Stream stream, byte[] buffer, int initialIndex)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (buffer == null)
                throw new ArgumentNullException("buffer");

            stream.FullRead(buffer, initialIndex, buffer.Length - initialIndex);
        }

        /// <summary>
        /// Reads the buffer in the requested area, but throws an exception if
        /// can't read the full requested area.
        /// </summary>
        public static void FullRead(this Stream stream, byte[] buffer, int initialIndex, int count)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (buffer == null)
                throw new ArgumentNullException("buffer");

            if (count < 0)
                throw new ArgumentOutOfRangeException("count");

            int position = initialIndex;
            int end = initialIndex + count;

            while (position < end)
            {
                int read = stream.Read(buffer, position, end - position);

                if (read == 0)
                    throw new IOException("End of Stream or Stream Closed before reading all needed information.");

                position += read;
            }
        }

        /// <summary>
        /// Reads a byte or throws an exception at end of file.
        /// </summary>
        public static byte ReadByteOrThrow(this Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            int result = stream.ReadByte();

            if (result == -1)
                throw new EndOfStreamException();

            return (byte)result;
        }

        /// <summary>
        /// Reads a compressed int.
        /// </summary>
        public static int ReadCompressedInt32(this Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            int result = 0;
            var bitShift = 0;

            while (true)
            {
                byte nextByte = stream.ReadByteOrThrow();

                result |= (nextByte & 0x7f) << bitShift;
                bitShift += 7;

                if ((nextByte & 0x80) == 0)
                    return result;
            }
        }

        /// <summary>
        /// Reads a compressed uint.
        /// </summary>
        [CLSCompliant(false)]
        public static uint ReadCompressedUInt32(this Stream stream)
        {
            return (uint)stream.ReadCompressedInt32();
        }


        /// <summary>
        /// Reads a compressed long.
        /// </summary>
        public static long ReadCompressedInt64(this Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            long result = 0;
            var bitShift = 0;

            while (true)
            {
                long nextByte = stream.ReadByteOrThrow();

                result += (nextByte & 0x7f) << bitShift;
                bitShift += 7;

                if ((nextByte & 0x80) == 0)
                    return result;
            }
        }

        /// <summary>
        /// Reads a compressed ulong.
        /// </summary>
        [CLSCompliant(false)]
        public static ulong ReadCompressedUInt64(this Stream stream)
        {
            return (ulong)stream.ReadCompressedInt64();
        }

        /// <summary>
        /// Writes a compressed int.
        /// </summary>
        public static void WriteCompressedInt32(this Stream stream, int value)
        {
            stream.WriteCompressedUInt32(unchecked((uint)value));
        }

        /// <summary>
        /// Writes a compressed uint.
        /// </summary>
        [CLSCompliant(false)]
        public static void WriteCompressedUInt32(this Stream stream, uint value)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            while (value >= 0x80)
            {
                stream.WriteByte((byte)(value | 0x80));
                value >>= 7;
            }

            stream.WriteByte((byte)value);
        }

        /// <summary>
        /// Writes a compressed int.
        /// </summary>
        public static void WriteCompressedInt64(this Stream stream, long value)
        {
            stream.WriteCompressedUInt64(unchecked((ulong)value));
        }

        /// <summary>
        /// Writes a compressed uint.
        /// </summary>
        [CLSCompliant(false)]
        public static void WriteCompressedUInt64(this Stream stream, ulong value)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            while (value >= 0x80)
            {
                stream.WriteByte((byte)(value | 0x80));
                value >>= 7;
            }

            stream.WriteByte((byte)value);
        }

        /// <summary>
        /// Reads a string written as size and utf-bytes.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static string ReadString(this Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            byte[] bytes = stream.ReadByteArray();
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Copies data from one stream to another, using the given buffer for each
        /// operation and calling an action, if provided, to tell how the progress
        /// is going.
        /// </summary>
        /// <param name="sourceStream">The stream to read data from.</param>
        /// <param name="destinationStream">The stream to write data to.</param>
        /// <param name="blockBuffer">To buffer to use for read and write operations. The buffer does not need to be of the size of the streamed data, as many read/writes are done if needed.</param>
        /// <param name="onProgress">The action to be executed as each block is successfully copied. The value passed as parameter is the number of bytes read this time (not the total). This parameter can be null.</param>
        public static void CopyTo(this Stream sourceStream, Stream destinationStream, byte[] blockBuffer, Action<int> onProgress)
        {
            if (sourceStream == null)
                throw new ArgumentNullException("sourceStream");

            if (destinationStream == null)
                throw new ArgumentNullException("destinationStream");

            if (blockBuffer == null)
                throw new ArgumentNullException("blockBuffer");

            int length = blockBuffer.Length;
            while (true)
            {
                int read = sourceStream.Read(blockBuffer, 0, length);
                if (read == 0)
                    return;

                destinationStream.Write(blockBuffer, 0, read);
                destinationStream.Flush();

                if (onProgress != null)
                    onProgress(read);
            }
        }

        /// <summary>
        /// Reads a byte-array that was written as size and bytes.
        /// </summary>
        public static byte[] ReadByteArray(this Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            int length = stream.ReadCompressedInt32();
            byte[] result = new byte[length];
            stream.FullRead(result);
            return result;
        }

        /// <summary>
        /// Writes an string as size and then utf bytes.
        /// </summary>
        public static void WriteString(this Stream stream, string value)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            byte[] buffer = Encoding.UTF8.GetBytes(value);
            stream.WriteByteArray(buffer);
        }

        /// <summary>
        /// Writes an array as size and then bytes.
        /// </summary>
        public static void WriteByteArray(this Stream stream, byte[] byteArray)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (byteArray == null)
                throw new ArgumentNullException("byteArray");

            stream.WriteCompressedInt32(byteArray.Length);
            stream.Write(byteArray, 0, byteArray.Length);
        }

        /// <summary>
        /// Reads a boolean array where the size is already known.
        /// </summary>
        public static void ReadBooleanArray(this Stream stream, bool[] array)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (array == null)
                throw new ArgumentNullException("array");

            int count = array.Length;
            byte b = 0;
            for (int i = 0; i < count; i++)
            {
                int mod = i % 8;
                if (mod == 0)
                    b = stream.ReadByteOrThrow();

                array[i] = ((b << mod) & 128) == 128;
            }
        }

        /// <summary>
        /// Writes a boolean array.
        /// </summary>
        public static void WriteBooleanArray(this Stream stream, bool[] array)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (array == null)
                throw new ArgumentNullException("array");

            int count = array.Length;
            byte b = 0;
            for (int i = 0; i < count; i++)
            {
                int mod = i % 8;

                if (array[i])
                    b |= (byte)(128 >> mod);

                if (mod == 7)
                {
                    stream.WriteByte(b);
                    b = 0;
                }
            }

            if ((count % 8) != 0)
                stream.WriteByte(b);
        }

        /// <summary>
        /// Reads a nullable boolean array which known size.
        /// </summary>
        public static void ReadNullableBooleanArray(this Stream stream, bool?[] array)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (array == null)
                throw new ArgumentNullException("array");

            int count = array.Length;
            byte b = 0;
            for (int i = 0; i < count; i++)
            {
                int mod = (i % 4) * 2;
                if (mod == 0)
                    b = stream.ReadByteOrThrow();

                switch ((b << mod) & 192)
                {
                    case 192:
                        array[i] = null;
                        break;

                    case 0:
                        array[i] = false;
                        break;

                    case 64:
                        array[i] = true;
                        break;

                    default:
                        throw new IOException("Invalid byte in array.");
                }
            }
        }

        /// <summary>
        /// Writes a nullable boolean array.
        /// </summary>
        public static void WriteNullableBooleanArray(this Stream stream, bool?[] array)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            int count = array.Length;
            byte b = 0;
            for (int i = 0; i < count; i++)
            {
                int mod = (i % 4) * 2;

                bool? value = array[i];
                if (value.HasValue)
                {
                    if (value.Value)
                        b |= (byte)(64 >> mod);

                    // there is no need for else, as we will combine 0 to it.
                }
                else
                    b |= (byte)(192 >> mod);

                if (mod == 6)
                {
                    stream.WriteByte(b);
                    b = 0;
                }
            }

            if ((count % 4) != 0)
                stream.WriteByte(b);
        }
    }
}
