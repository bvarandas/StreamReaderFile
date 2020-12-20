using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest
{
    public class StreamLineReader : IDisposable
    {
        const int BufferLength = 1024;

        Stream _streamFile;
        int _read = 0, _index = 0;
        byte[] _bff = new byte[BufferLength];

        long _currentPosition = 0;
        int _currentLine = 0;

        public long CurrentPosition
        {
            get
            {
                return _currentPosition;
            }
        }
        
        public int CurrentLine
        {
            get
            {
                return _currentLine;
            }
        }
        
        public StreamLineReader(Stream stream)
        {
            _streamFile = stream;
        }
        
        public bool GoToLine(int goToLine)
        {
            return IGetCount(goToLine, true) == goToLine;
        }
        
        public int GetCount(int goToLine)
        {
            return IGetCount(goToLine, false);
        }
        
        int IGetCount(int goToLine, bool stopWhenLine)
        {
            _streamFile.Seek(0, SeekOrigin.Begin);
            _currentPosition = 0;
            _currentLine = 0;
            _index = 0;
            _read = 0;

            long savePosition = _streamFile.Length;

            do
            {
                if (_currentLine == goToLine)
                {
                    savePosition = _currentPosition;
                    if (stopWhenLine) return _currentLine;
                }
            }
            while (ReadLine() != null);

            

            int count = _currentLine;

            _currentLine = goToLine;
            _streamFile.Seek(savePosition, SeekOrigin.Begin);

            return count;
        }
        
        public string ReadLine()
        {
            bool found = false;

            StringBuilder sb = new StringBuilder();
            while (!found)
            {
                if (_read <= 0)
                {
                    
                    _index = 0;
                    _read = _streamFile.Read(_bff, 0, BufferLength);
                    if (_read == 0)
                    {
                        if (sb.Length > 0) break;
                        return null;
                    }
                }

                for (int max = _index + _read; _index < max;)
                {
                    char ch = (char)_bff[_index];
                    _read--; _index++;
                    _currentPosition++;

                    if (ch == '\0' || ch == '\n')
                    {
                        found = true;
                        break;
                    }
                    else if (ch == '\r') continue;
                    else sb.Append(ch);
                }
            }

            _currentLine++;
            return sb.ToString();
        }
        
        public void Dispose()
        {
            if (_streamFile != null)
            {
                _streamFile.Close();
                _streamFile.Dispose();
                _streamFile = null;
            }
        }
    }
}
