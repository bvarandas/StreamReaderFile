using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Concurrent;

namespace ConsoleTest
{
    class Program
    {
        static StreamFileLinesReader Reader = null;
        //static StreamWriterFileTest writer = null;

        static void Main(string[] args)
        {
            string fileName = string.Empty;

            //writer = new StreamWriterFileTest(fileName);

            var lines = new StringBuilder();

            while (!File.Exists(fileName))
            {
                Console.WriteLine("Insira o nome do arquivo válido");

                fileName = Console.ReadLine();

                if (!File.Exists(fileName))
                {
                    Console.WriteLine($"Arquivo {fileName} não foi encontrado!");
                    continue;
                }
                else
                {
                    Console.WriteLine($"O Arquivo {fileName} encontrado com sucesso!");
                }

                Reader = new StreamFileLinesReader(fileName);

                Console.WriteLine("Primeiras linhas carregadas com sucesso!!!");
            }

            ConsoleKeyInfo keyRead;
            do
            {
                keyRead = Console.ReadKey();
                Console.WriteLine("---You Pressed " + keyRead.Key.ToString());

                if (keyRead.Key == ConsoleKey.UpArrow)
                {
                    lines = Reader.GetLinesUp();
                    Console.WriteLine(lines);
                }

                if (keyRead.Key == ConsoleKey.DownArrow)
                {
                    lines = Reader.GetLinesDown();
                    Console.WriteLine(lines);
                }

                if (keyRead.Key == ConsoleKey.PageUp)
                {
                    lines =  Reader.GetLinesPageUp();
                    Console.WriteLine(lines);
                }

                if (keyRead.Key == ConsoleKey.PageDown)
                {
                    lines =  Reader.GetLinesPageDown();
                    Console.WriteLine(lines);
                }

                if (keyRead.Key == ConsoleKey.L)
                {
                    Console.WriteLine("Digite a linha desejada:");

                    var line = Convert.ToInt64(Console.ReadLine());

                    try
                    {
                        lines = Reader.GetLinesSearch(line);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                    Console.WriteLine(lines);

                }
            } while (keyRead.Key != ConsoleKey.Escape);
        }
    }
}