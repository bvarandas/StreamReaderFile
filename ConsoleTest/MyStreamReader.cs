using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest
{
    
    [Serializable()]
    public class myStreamReader : TextReader
    {
        public new static readonly myStreamReader Null = new NullmyStreamReader();

        internal const int DefaultBufferSize = 1024;  
        private const int DefaultFileStreamBufferSize = 4096;
        private const int MinBufferSize = 128;
        private Stream stream;
        private Encoding encoding;
        private Decoder decoder;
        private byte[] byteBuffer;
        private char[] charBuffer;
        private byte[] _preamble;
        private int charPos;
        private int charLen;
        private int byteLen;
        private int _maxCharsPerBuffer;
        private bool _detectEncoding;
        private bool _checkPreamble;
        private bool _isBlocked;

        private int _lineLength;
        public int LineLength
        {
            get { return _lineLength; }
        }

        private int _bytesRead;
        public int BytesRead
        {
            get { return _bytesRead; }
        }

        internal myStreamReader()
        {
        }

        public myStreamReader(Stream stream)
            : this(stream, Encoding.UTF8, true, DefaultBufferSize)
        {
        }

        public myStreamReader(Stream stream, bool detectEncodingFromByteOrderMarks)
            : this(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks, DefaultBufferSize)
        {
        }

        public myStreamReader(Stream stream, Encoding encoding)
            : this(stream, encoding, true, DefaultBufferSize)
        {
        }

        public myStreamReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks)
            : this(stream, encoding, detectEncodingFromByteOrderMarks, DefaultBufferSize)
        {
        }

        public myStreamReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize)
        {
            if (stream == null || encoding == null)
                throw new ArgumentNullException((stream == null ? "stream" : "encoding"));
            if (!stream.CanRead)
                throw new ArgumentException(Environment.GetEnvironmentVariable("Argument_StreamNotReadable"));
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException("bufferSize", Environment.GetEnvironmentVariable("ArgumentOutOfRange_NeedPosNum"));

            Init(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize);
        }

        public myStreamReader(String path)
            : this(path, Encoding.UTF8, true, DefaultBufferSize)
        {
        }

        public myStreamReader(String path, bool detectEncodingFromByteOrderMarks)
            : this(path, Encoding.UTF8, detectEncodingFromByteOrderMarks, DefaultBufferSize)
        {
        }

        public myStreamReader(String path, Encoding encoding)
            : this(path, encoding, true, DefaultBufferSize)
        {
        }

        public myStreamReader(String path, Encoding encoding, bool detectEncodingFromByteOrderMarks)
            : this(path, encoding, detectEncodingFromByteOrderMarks, DefaultBufferSize)
        {
        }

        public myStreamReader(String path, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize)
        {
            if (path == null || encoding == null)
                throw new ArgumentNullException((path == null ? "path" : "encoding"));
            if (path.Length == 0)
                throw new ArgumentException(Environment.GetEnvironmentVariable("Argument_EmptyPath"));
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException("bufferSize", Environment.GetEnvironmentVariable("ArgumentOutOfRange_NeedPosNum"));

            Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultFileStreamBufferSize);
            Init(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize);
        }

        private void Init(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize)
        {
            this.stream = stream;
            this.encoding = encoding;
            decoder = encoding.GetDecoder();
            if (bufferSize < MinBufferSize) bufferSize = MinBufferSize;
            byteBuffer = new byte[bufferSize];
            _maxCharsPerBuffer = encoding.GetMaxCharCount(bufferSize);
            charBuffer = new char[_maxCharsPerBuffer];
            byteLen = 0;
            _detectEncoding = detectEncodingFromByteOrderMarks;
            _preamble = encoding.GetPreamble();
            _checkPreamble = (_preamble.Length > 0);
            _isBlocked = false;
        }

        public override void Close()
        {
            Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (stream != null)
                    stream.Close();
            }
            if (stream != null)
            {
                stream = null;
                encoding = null;
                decoder = null;
                byteBuffer = null;
                charBuffer = null;
                charPos = 0;
                charLen = 0;
            }
            base.Dispose(disposing);
        }

        public virtual Encoding CurrentEncoding
        {
            get { return encoding; }
        }

        
        public virtual Stream BaseStream
        {
            get { return stream; }
        }

        public void DiscardBufferedData()
        {
            byteLen = 0;
            charLen = 0;
            charPos = 0;
            decoder = encoding.GetDecoder();
            _isBlocked = false;
        }

        public override int Peek()
        {

            if (charPos == charLen)
            {
                if (_isBlocked || ReadBuffer() == 0) return -1;
            }
            return charBuffer[charPos];
        }

        public override int Read()
        {
            if (charPos == charLen)
            {
                if (ReadBuffer() == 0) return -1;
            }
            return charBuffer[charPos++];
        }

        public override int Read([In, Out] char[] buffer, int index, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer", Environment.GetEnvironmentVariable("ArgumentNull_Buffer"));
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? "index" : "count"), Environment.GetEnvironmentVariable("ArgumentOutOfRange_NeedNonNegNum"));
            if (buffer.Length - index < count)
                throw new ArgumentException(Environment.GetEnvironmentVariable("Argument_InvalidOffLen"));

            int charsRead = 0;
            
            bool readToUserBuffer = false;
            while (count > 0)
            {
                int n = charLen - charPos;
                if (n == 0) n = ReadBuffer(buffer, index + charsRead, count, out readToUserBuffer);
                if (n == 0) break;  // We're at EOF
                if (n > count) n = count;
                if (!readToUserBuffer)
                {
                    Buffer.BlockCopy(charBuffer, charPos * 2, buffer, (index + charsRead) * 2, n * 2);
                    charPos += n;
                }
                charsRead += n;
                count -= n;
            
                if (_isBlocked)
                    break;
            }
            return charsRead;
        }

        public override String ReadToEnd()
        {
            char[] chars = new char[charBuffer.Length];
            int len;
            StringBuilder sb = new StringBuilder(charBuffer.Length);
            while ((len = Read(chars, 0, chars.Length)) != 0)
            {
                sb.Append(chars, 0, len);
            }
            return sb.ToString();
        }

        private void CompressBuffer(int n)
        {
            Buffer.BlockCopy(byteBuffer, n, byteBuffer, 0, byteLen - n);
            byteLen -= n;
        }

        private static bool BytesMatch(byte[] buffer, byte[] compareTo)
        {
            for (int i = 0; i < compareTo.Length; i++)
                if (buffer[i] != compareTo[i])
                    return false;
            return true;
        }

        private void DetectEncoding()
        {
            if (byteLen < 2)
                return;
            _detectEncoding = false;
            bool changedEncoding = false;
            if (byteBuffer[0] == 0xFE && byteBuffer[1] == 0xFF)
            {
                encoding = new UnicodeEncoding(true, true);
                decoder = encoding.GetDecoder();
                CompressBuffer(2);
                changedEncoding = true;
            }
            else if (byteBuffer[0] == 0xFF && byteBuffer[1] == 0xFE)
            {
                encoding = new UnicodeEncoding(false, true);
                decoder = encoding.GetDecoder();
                CompressBuffer(2);
                changedEncoding = true;
            }
            else if (byteLen >= 3 && byteBuffer[0] == 0xEF && byteBuffer[1] == 0xBB && byteBuffer[2] == 0xBF)
            {
                encoding = Encoding.UTF8;
                decoder = encoding.GetDecoder();
                CompressBuffer(3);
                changedEncoding = true;
            }
            else if (byteLen == 2)
                _detectEncoding = true;

            if (changedEncoding)
            {
                _maxCharsPerBuffer = encoding.GetMaxCharCount(byteBuffer.Length);
                charBuffer = new char[_maxCharsPerBuffer];
            }
        }


        private int ReadBuffer()
        {
            charLen = 0;
            byteLen = 0;
            charPos = 0;
            do
            {
                byteLen = stream.Read(byteBuffer, 0, byteBuffer.Length);

                if (byteLen == 0)  // We're at EOF
                    return charLen;

                _isBlocked = (byteLen < byteBuffer.Length);

                if (_checkPreamble && byteLen >= _preamble.Length)
                {
                    _checkPreamble = false;
                    if (BytesMatch(byteBuffer, _preamble))
                    {
                        _detectEncoding = false;
                        CompressBuffer(_preamble.Length);
                    }
                }

                if (_detectEncoding && byteLen >= 2)
                    DetectEncoding();

                charLen += decoder.GetChars(byteBuffer, 0, byteLen, charBuffer, charLen);
            } while (charLen == 0);

            return charLen;
        }

        private int ReadBuffer(char[] userBuffer, int userOffset, int desiredChars, out bool readToUserBuffer)
        {
            charLen = 0;
            byteLen = 0;
            charPos = 0;
            int charsRead = 0;

            readToUserBuffer = desiredChars >= _maxCharsPerBuffer;

            do
            {
                byteLen = stream.Read(byteBuffer, 0, byteBuffer.Length);

                if (byteLen == 0)  // EOF
                    return charsRead;

                _isBlocked = (byteLen < byteBuffer.Length);


                if (_detectEncoding && byteLen >= 2)
                {
                    DetectEncoding();

                    readToUserBuffer = desiredChars >= _maxCharsPerBuffer;
                }

                if (_checkPreamble && byteLen >= _preamble.Length)
                {
                    _checkPreamble = false;
                    if (BytesMatch(byteBuffer, _preamble))
                    {
                        _detectEncoding = false;
                        CompressBuffer(_preamble.Length);
                        
                        readToUserBuffer = desiredChars >= _maxCharsPerBuffer;
                    }
                }

                

                charPos = 0;
                if (readToUserBuffer)
                {
                    charsRead += decoder.GetChars(byteBuffer, 0, byteLen, userBuffer, userOffset + charsRead);
                    charLen = 0;  
                }
                else
                {
                    charsRead = decoder.GetChars(byteBuffer, 0, byteLen, charBuffer, charsRead);
                    charLen += charsRead;  
                }
            } while (charsRead == 0);
            
            return charsRead;
        }

        public override String ReadLine()
        {
            _lineLength = 0;
            if (charPos == charLen)
            {
                if (ReadBuffer() == 0) return null;
            }
            StringBuilder sb = null;
            do
            {
                int i = charPos;
                do
                {
                    char ch = charBuffer[i];
                    int EolChars = 0;
                    if (ch == '\r' || ch == '\n')
                    {
                        EolChars = 1;
                        String s;
                        if (sb != null)
                        {
                            sb.Append(charBuffer, charPos, i - charPos);
                            s = sb.ToString();
                        }
                        else
                        {
                            s = new String(charBuffer, charPos, i - charPos);
                        }
                        charPos = i + 1;
                        if (ch == '\r' && (charPos < charLen || ReadBuffer() > 0))
                        {
                            if (charBuffer[charPos] == '\n')
                            {
                                charPos++;
                                EolChars = 2;
                            }
                        }
                        _lineLength = s.Length + EolChars;
                        _bytesRead = _bytesRead + _lineLength;
                        return s;
                    }
                    i++;
                } while (i < charLen);
                i = charLen - charPos;
                if (sb == null) sb = new StringBuilder(i + 80);
                sb.Append(charBuffer, charPos, i);
            } while (ReadBuffer() > 0);
            string ss = sb.ToString();
            _lineLength = ss.Length;
            _bytesRead = _bytesRead + _lineLength;
            return ss;
        }

        private class NullmyStreamReader : myStreamReader
        {
            public override Stream BaseStream
            {
                get { return Stream.Null; }
            }

            public override Encoding CurrentEncoding
            {
                get { return Encoding.Unicode; }
            }

            public override int Peek()
            {
                return -1;
            }

            public override int Read()
            {
                return -1;
            }

            public override int Read(char[] buffer, int index, int count)
            {
                return 0;
            }

            public override String ReadLine()
            {
                return null;
            }

            public override String ReadToEnd()
            {
                return String.Empty;
            }
        }
    }
}

