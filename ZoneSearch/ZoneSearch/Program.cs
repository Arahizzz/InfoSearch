using System;
using System.Threading.Tasks;

namespace ZoneSearch
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var dict = await Reader.DictionaryFromFiles(
                new []{0.2f, 0.3f, 0.3f},@"C:\Users\Юрий\Downloads\Gogol_DARKER-Rasskazy-2011-2015-.lFpDQQ.442219.fb2");

            while (true)
            {
                Console.Write("Enter query: ");
                foreach (var result in dict.Query(Console.ReadLine()))
                {
                    Console.WriteLine(result);
                }
            }
        }
    }
}