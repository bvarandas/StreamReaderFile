using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest
{
    public class StreamFileLinesReader :IDisposable
    {
        private SortedDictionary<long, string> _lines = new SortedDictionary<long, string>();
        private long _actualPageLine = 5;
        private long _actualPage = 0;
        private int _incremental = 5;
        private int _pagination = 10;
        //private string fileName;
        private StreamReader _streamFile;
        private int numberLinesBuffer = 100;
        private long LastPosition = 0;
        private long AverageBytesLine = 0;
        private long SumBytesLine = 0;

        public StreamFileLinesReader(string fileName)
        {
            long i = 0;

            _streamFile = new StreamReader(fileName);

            while (_streamFile.Peek() >= 0 && i < numberLinesBuffer)
            {
                string line = _streamFile.ReadLine();
                _lines.Add(i, line);
                int byteSize = System.Text.ASCIIEncoding.ASCII.GetByteCount(line);
                SumBytesLine += byteSize;
                AverageBytesLine = SumBytesLine / (i + 1);
                i++;
            }

            LastPosition = _streamFile.BaseStream.Position;

        }


        public StringBuilder GetLinesSearch(long line)
        {
            var ret = new StringBuilder();

            try
            {
                long seek = (AverageBytesLine * (line - 1));

                _lines.Clear();
                _streamFile.BaseStream.Seek(seek, SeekOrigin.Current);
                long cont = 0;

                long total = line + numberLinesBuffer;
                long initial = line - numberLinesBuffer;

                while (_streamFile.Peek() >= 0 && cont < total)
                {
                    string lineRead = _streamFile.ReadLine();

                    if (cont >= initial) _lines.Add(cont, lineRead);

                    cont++;
                }

                if (cont < line)
                {
                    ret.Append($"O arquivo não contem a linha {line}");
                    return ret ;
                }

                LastPosition = _streamFile.BaseStream.Position;

                _actualPageLine = line;
                _actualPage = line;
                for (long i = (_actualPageLine-_incremental); i <= (_actualPageLine + _incremental); i++)
                {
                    string linhaLida = string.Empty;
                    if (_lines.TryGetValue(i, out linhaLida))
                    {
                        ret.Append(i + " - " + linhaLida + "\n");
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return ret;
        }

        private StringBuilder GetLines()
        {
            var ret = new StringBuilder();

            if (_lines.Count == 0)
            {
                ret.Append("Fim de arquivo");
                return ret;
            }
            
            long last = _lines.Keys.Max();
            long first = _lines.Keys.Min();

            if (_actualPageLine < first)
            {
                _streamFile.BaseStream.Seek(LastPosition, SeekOrigin.Current);

                long total = last;

                last = first < 0 ? 0 : first;

                last = last - numberLinesBuffer;
                total = total - numberLinesBuffer;

                _lines.Clear();

                while (_streamFile.Peek() >= 0 && last <= total)
                {
                    string line = _streamFile.ReadLine();
                    _lines.Add(last, line);
                    last++;
                }
            }

            if (_actualPageLine > last)
            {
                long total = _lines.Keys.Max() + numberLinesBuffer;

                _lines.Clear();
                last++;
                while (_streamFile.Peek() >= 0 && last <= total)
                {
                    string line = _streamFile.ReadLine();
                    _lines.Add(last, line);

                    last++;
                }

                for (long line = _actualPageLine; line <= (_actualPageLine + _incremental); line++)
                {
                    string linhaLida = string.Empty;
                    if (_lines.TryGetValue(line, out linhaLida))
                    {
                        ret.Append(line + " - " + linhaLida + "\n");
                    }
                }

                LastPosition = _streamFile.BaseStream.Position;
            }
            else
            {
                for (long line = (_actualPageLine - _incremental); line <= (_actualPageLine + _incremental); line++)
                {
                    if (line >= 0)
                    {
                        string linhaLida = string.Empty;
                        if (_lines.TryGetValue(line, out linhaLida))
                        {
                            ret.Append(line + " - " + linhaLida + "\n");
                        }
                    }
                }
            }
            return ret;
        }

        public StringBuilder GetLinesUp()
        {
            _actualPageLine--;
            if (_actualPageLine < _incremental) _actualPageLine = _incremental;
            _actualPage = _actualPageLine;
            return GetLines();
        }

        public StringBuilder GetLinesDown()
        {
            _actualPageLine++;
            _actualPage = _actualPageLine;
            return GetLines();
        }

        public StringBuilder GetLinesPageUp()
        {
            _actualPage = _actualPage - _pagination;
            if (_actualPage < _pagination) _actualPage = _pagination;
            _actualPageLine = _actualPage;
            return GetLines();
        }

        public StringBuilder GetLinesPageDown()
        {
            _actualPage = _actualPage + _pagination;
            _actualPageLine = _actualPage;
            return GetLines();
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
