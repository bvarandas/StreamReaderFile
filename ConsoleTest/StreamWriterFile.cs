using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest
{
    public class StreamWriterFile
    {
        public StreamWriterFile(string fileName)
        {
            // Get the directories currently on the C drive.
            DirectoryInfo[] cDirs = new DirectoryInfo(@"F:\arquivo teste\").GetDirectories();

            // Write each directory name to a file.
            using (StreamWriter sw = new StreamWriter("text3.txt"))
            {
                long finalValue = 100000000000;

                for(long i = 0; i < finalValue; i++)
                {
                    sw.WriteLine($"{i}	Teste de texo {i}" );
                }
            }

            // Read and show each line from the file.
            
        }
    }
}
