using System;
using System.Threading.Tasks;

namespace Reader
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Dictionary dictionary = await Dictionary.GetDictionary(@"D:\MapReduce\Result\dictionary.bin",
                @"D:\MapReduce\Result\docInfo.bin",
                @"D:\MapReduce\Result\a-d.bin");

            while (true)
            {
                Console.Write("Enter query: ");
                var query = Console.ReadLine();
                foreach (var entry in dictionary.Query(query))
                {
                    Console.WriteLine(entry);
                }
            }
        }
    }
}