using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MessagePack;

namespace BinaryDictionary
{
    class Program
    {
        static void Main(string[] args)
        {
            var savePath = "dictionaryPermutation.bin";

            Stopwatch stopwatch = Stopwatch.StartNew();
            var dict = DictionaryIo.SetDictionaryFromFiles(@"C:\Users\Юрий\Documents\Книги");
            //var dict = DictionaryIo.FromSaveFile<CoordinateDictionary>(savePath);
            foreach (var test in dict.VAnd("cat"))
            {
                Console.WriteLine(test);
            }

            stopwatch.Stop();
            //Console.WriteLine($"Trie size: {dict.Size}");
            Console.WriteLine($"Time elapsed: {stopwatch.ElapsedMilliseconds}");
            //dict.Test();
            DictionaryIo.Save(dict, savePath);
            //Console.WriteLine($"Binary file size: {new FileInfo(savePath).Length / 1000}KB");
        }
    }
}