using System;

namespace BM25
{
    class Program
    {
        static void Main(string[] args)
        {
            var collection = IndexedCollection.GetCollection(@"C:\Users\Юрий\Documents\Книги");
            while (true)
            {
                Console.Write("Enter query: ");
                foreach (var result in collection.Query(Console.ReadLine()))
                {
                    Console.WriteLine($"{result.Name} : {result.Score}");
                }
            }
        }
    }
}