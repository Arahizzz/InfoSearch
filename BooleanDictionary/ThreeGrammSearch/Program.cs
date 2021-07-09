using System;
using BinaryDictionary;
using static Dictionary.Parser;

namespace ThreeGrammSearch
{
    class Program
    {
        static void Main(string[] args)
        {
            var dictionary = DictionaryIo.ThreeGrammFromFiles(@"C:\Users\Юрий\Documents\Книги");
            while (true)
            {
                Console.Write("Enter query: ");
                try
                {
                    var parseResult = ParseQuery(Console.ReadLine());
                    var res = parseResult switch
                    {
                        SearchFunction.StartOfWord start => dictionary.StartsWith(start.Item),
                        SearchFunction.EndOfWord end => dictionary.EndsWith(end.Item),
                        SearchFunction.MiddleOfWord m => dictionary.StartAndEnds(m.Item1, m.Item2),
                        SearchFunction.WithoutJoker j => dictionary.GetWord(j.Item),
                        _ => throw new ArgumentException()
                    };
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
                catch (ArgumentException)
                {
                    Console.WriteLine("Incorrect query");
                }
            }
        }
    }
}