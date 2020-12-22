using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using ConsoleTest.Indexing;

namespace ConsoleTest
{
    internal sealed class IndexPosition
    {
        public long Line;
        public long Position;
    }

    public class StreamFileLinesReader :IDisposable
    {
        private SortedDictionary<long, string> _lines = new SortedDictionary<long, string>();
        private ConcurrentDictionary<long, long> _linesPosition = new ConcurrentDictionary<long, long>();
        private long _actualPageLine = 5;
        private long _actualPage = 0;
        private int _incremental = 5;
        private int _pagination = 10;
        private myStreamReader _streamFile;
        private int numberLinesBuffer = 100;
        private long LastPosition = 0;
        private string _fileName = string.Empty;
        private Indexer<IndexPosition> _indexer = null;
        private IEqualityIndex<long, IndexPosition> _linePos;


        public StreamFileLinesReader(string fileName)
        {
            long i = 0;

            _streamFile = new myStreamReader(fileName);

            _fileName = fileName;

            Task.Run(() => ThreadReadingFile(fileName));

            while (_streamFile.Peek() >= 0 && i < numberLinesBuffer)
            {
                string line = _streamFile.ReadLine();
                _lines.Add(i, line);
                i++;
            }

            LastPosition = _streamFile.BaseStream.Position;
        }

        public long GetLinePosition(long line)
        {
            long position = -1;

            if (_linePos is null)
            {

            }
            else
            {
                //_linesPosition.TryGetValue(line, out position);
                var itens = _linePos.EnumerateItems(line);

                foreach (var item in itens)
                    position = item.Position;
            }

            return position;
        }

        private void ThreadReadingFile(string fileName)
        {
            long cont = 1;
            long byteSize = 0;
            var spin = new SpinWait();

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            _indexer = new Indexer<IndexPosition>();
            
            using (var fs = File.OpenRead(fileName))
            using (myStreamReader reader = new myStreamReader(fs))
            {
                using (var transaction = _indexer.StartTransaction())
                {
                    while (reader.Peek() >= 0)
                    {
                        string line = reader.ReadLine();
                        byteSize = reader.BytesRead;
                        //_linesPosition.TryAdd(cont, byteSize);
                        transaction.Add(new IndexPosition { Line = cont, Position = byteSize });
                        cont++;
                        //if ((cont % 1000)==0)
                        //spin.SpinOnce();
                        //Thread.Sleep(1);

                    }

                    transaction.Commit();

                    _linePos = _indexer.AddEqualityIndex("Line", (x) => x.Line);
                }
            }
            
            stopwatch.Stop();
            //TestMemoryMappedFile(fileName);
        }



        public StringBuilder GetLinesSearch(long line)
        {
            var ret = new StringBuilder();

            try
            {
                _lines.Clear();
                
                long seekPosition = GetLinePosition(line- (numberLinesBuffer / 2));
                if (seekPosition == -1)
                {
                    _streamFile.DiscardBufferedData();
                    _streamFile.BaseStream.Seek(0, SeekOrigin.Begin);

                }
                else
                {
                    _streamFile.DiscardBufferedData();
                    _streamFile.BaseStream.Seek(seekPosition, SeekOrigin.Begin);
                }              

                long cont = 0;

                long total = line + (numberLinesBuffer/2);
                long initial = line - (numberLinesBuffer/2);

                if (seekPosition!= -1)
                    cont = initial;

                string lineRead = string.Empty;

                while (_streamFile.Peek() >=0 && cont < total)
                {
                    lineRead = _streamFile.ReadLine();
                    if (seekPosition == -1)
                    {
                        if (cont >= initial)
                            _lines.Add(cont, lineRead);
                    }else
                        _lines.Add(cont, lineRead);
                    cont++;
                }

                if (!_streamFile.BaseStream.CanRead)
                {
                    ret.Append($"O arquivo não contem a linha {line}");

                    _streamFile.Close();
                    _streamFile = new myStreamReader(_fileName);
                    return ret ;
                }

                LastPosition = _streamFile.BaseStream.Position;

                _actualPageLine = line;
                _actualPage = line;
                for (long i = (_actualPageLine-_incremental); i <= (_actualPageLine + _incremental); i++)
                {
                    string pagelineRead= string.Empty;
                    if (_lines.TryGetValue(i, out pagelineRead))
                    {
                        ret.Append(pagelineRead + "\n");
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
                        ret.Append( linhaLida + "\n");
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
                            ret.Append( linhaLida + "\n");
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

        private void TestMemoryMappedFile(string fileName)
        {
            string filename = fileName;
            long fileLen = new FileInfo(filename).Length;
            List<long> badPositions = new List<long>();
            List<byte> currentLine = new List<byte>();
            List<string> lines = new List<string>();
            bool lastReadByteWasLF = false;
            int linesToRead = 20;
            int linesRead = 0;
            long lastBytePos = fileLen;

            MemoryMappedFile mapFile = MemoryMappedFile.CreateFromFile(filename, FileMode.Open);

            using (mapFile)
            {
                var view = mapFile.CreateViewAccessor();

                for (long i = fileLen - 1; i >= 0; i--) //iterate backwards
                {

                    try
                    {
                        byte b = view.ReadByte(i);
                        lastBytePos = i;

                        switch (b)
                        {
                            case 13: //CR
                                if (lastReadByteWasLF)
                                {
                                    {
                                        //A line has been read
                                        var bArray = currentLine.ToArray();
                                        if (bArray.LongLength > 1)
                                        {
                                            //Add line string to lines collection
                                            lines.Insert(0, Encoding.UTF8.GetString(bArray, 1, bArray.Length - 1));

                                            //Clear current line list
                                            currentLine.Clear();

                                            //Add CRLF to currentLine -- comment this out if you don't want CRLFs in lines
                                            currentLine.Add(13);
                                            currentLine.Add(10);

                                            linesRead++;
                                        }
                                    }
                                }
                                lastReadByteWasLF = false;

                                break;
                            case 10: //LF
                                lastReadByteWasLF = true;
                                currentLine.Insert(0, b);
                                break;
                            default:
                                lastReadByteWasLF = false;
                                currentLine.Insert(0, b);
                                break;
                        }

                        if (linesToRead == linesRead)
                        {
                            break;
                        }


                    }
                    catch
                    {
                        lastReadByteWasLF = false;
                        currentLine.Insert(0, (byte)'?');
                        badPositions.Insert(0, i);
                    }
                }

            }

            if (linesToRead > linesRead)
            {
                //Read last line
                {
                    var bArray = currentLine.ToArray();
                    if (bArray.LongLength > 1)
                    {
                        //Add line string to lines collection
                        lines.Insert(0, Encoding.UTF8.GetString(bArray));
                        linesRead++;
                    }
                }
            }

            //Print results
            lines.ForEach(o => Console.WriteLine(o));
            Console.ReadKey();
        }
    }
}
