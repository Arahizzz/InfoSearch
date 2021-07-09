using System;
using BinaryDictionary;

namespace PermutationSearch
{
    public class Search
    {
        static void Main(string[] args)
        {
            var dictionary = DictionaryIo.PermutationFromFiles(@"C:\Users\Юрий\Documents\Книги");
            while (true)
            {
                Console.Write("Enter query: ");
                var query = Console.ReadLine();
                if (query.Occurrences('*') <= 2)
                {
                    var res = dictionary.GetWords(query);
                    foreach (var word in res)
                    {
                        Console.WriteLine(word);
                    }

                    Console.WriteLine("Found in these documents:");
                    foreach (var docname in res.GetContainingDocs())
                    {
                        Console.WriteLine(docname);
                    }
                }
                else Console.WriteLine("Incorrect query");
            }
        }
    }
}