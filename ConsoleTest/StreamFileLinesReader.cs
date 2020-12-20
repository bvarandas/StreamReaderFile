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
        private StreamReader _streamFile;
        private int numberLinesBuffer = 100;
        private long LastPosition = 0;
        private long _averageBytesLine = 0;
        private long _sumBytesLine = 0;
        private string _fileName = string.Empty;

        public StreamFileLinesReader(string fileName)
        {
            long i = 0;

            _streamFile = new StreamReader(fileName);

            _fileName = fileName;

            while (_streamFile.Peek() >= 0 && i < numberLinesBuffer)
            {
                string line = _streamFile.ReadLine();
                _lines.Add(i, line);
                this.CalculateAverageBytesLine(line, i);
                i++;
            }

            LastPosition = _streamFile.BaseStream.Position;

        }

        private void CalculateAverageBytesLine(string line, long cont)
        {
            int byteSize = System.Text.ASCIIEncoding.ASCII.GetByteCount(line);
            _sumBytesLine += byteSize;
            _averageBytesLine = _sumBytesLine / (cont + 1);
        }


        public StringBuilder GetLinesSearch(long line)
        {
            var ret = new StringBuilder();

            try
            {
                long seek = (_averageBytesLine * (line - 1));

                _lines.Clear();

                
                    //_streamFile.BaseStream.Seek(0, SeekOrigin.Begin);

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
                    _streamFile.Close();
                    _streamFile = new StreamReader(_fileName);
                    return ret ;
                }

                LastPosition = _streamFile.BaseStream.Position;

                _actualPageLine = line;
                _actualPage = line;
                for (long i = (_actualPageLine-_incremental); i <= (_actualPageLine + _incremental); i++)
                {
                    string lineRead= string.Empty;
                    if (_lines.TryGetValue(i, out lineRead))
                    {
                        ret.Append(i + " - " + lineRead + "\n");

                        CalculateAverageBytesLine(lineRead, i);
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
