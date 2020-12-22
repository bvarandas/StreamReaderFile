using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest.Collections
{
    /// <summary>
	/// The BitConverter class does not allow to fill an existing array,
	/// it always creates a new array which, in many cases, need to be copied.
	/// This class simple add filling methods.
	/// </summary>
	public static class BitConverterEx
    {
        /// <summary>
        /// Fills 8 bytes of a byte array with the contents of a given 
        /// long (Int64) value.
        /// </summary>
        public static void FillBytes(byte[] buffer, int offset, long value)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            if (offset < 0 || offset + 8 > buffer.Length)
                throw new ArgumentOutOfRangeException("offset");

            for (int i = 0; i < 8; i++)
            {
                buffer[offset] = (byte)value;
                offset++;
                value >>= 8;
            }
        }

        /// <summary>
        /// Fills 4 bytes of a byte array with the contents of a given 
        /// int (Int32) value.
        /// </summary>
        public static void FillBytes(byte[] buffer, int offset, int value)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            if (offset < 0 || offset + 4 > buffer.Length)
                throw new ArgumentOutOfRangeException("offset");

            for (int i = 0; i < 4; i++)
            {
                buffer[offset] = (byte)value;
                offset++;
                value >>= 8;
            }
        }
    }
}
