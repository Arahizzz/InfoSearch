using System;
using System.Threading.Tasks;

namespace FB2Reader
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var dict = await Reader.DictionaryFromFiles(@"C:\Users\Юрий\Downloads\Gogol_DARKER-Rasskazy-2011-2015-.lFpDQQ.442219.fb2");
            while (true)
            {
                Console.Write("Enter query: ");
                foreach (var doc in dict.GetContainingDocuments(Console.ReadLine()))
                {
                    Console.WriteLine(doc);
                }
            }
        }
    }
}