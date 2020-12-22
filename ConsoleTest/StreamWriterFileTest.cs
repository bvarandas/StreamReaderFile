using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTest
{
    public class StreamWriterFileTest
    {
        public StreamWriterFileTest(string fileName)
        {
            // Write each directory name to a file.
            using (StreamWriter sw = new StreamWriter(@"F:\arquivo teste\text3.txt"))
            {
                long finalValue = 50000000;

                for (long i = 0; i < finalValue; i++)
                {
                    sw.WriteLine($"{i}	orem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.");
                }
            }

            // Read and show each line from the file.

        }
    }
}
