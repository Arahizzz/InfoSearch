using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MapReduce.Models;
using MapReduce.Services;

namespace MapReduce
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            
            MapReduce mapReduce = new MapReduce();
            
            await mapReduce.Execute(@"D:\MapReduce\gutenberg_txt");

            // var test = new WordMultiDocs
            // {
            //     Word = 123423,
            //     DocId = new List<int>{1, 343, 3455, 23657}
            // };
            //
            // var test2 = new WordMultiDocs
            // {
            //     Word = 97743,
            //     DocId = new List<int> {56, 655, 764, 34546}
            // };
            //
            // await Serializer.Serialize(@"D:\MapReduce\test.bin", new List<WordMultiDocs>{test, test2});
            //
            // var test3 = Deserializer.Deserialize(@"D:\MapReduce\test.bin");
            //
            // await foreach (var wmd in test3)
            // {
            //     Console.WriteLine(wmd.Word);
            //     foreach (var doc in wmd.DocId)
            //     {
            //         Console.WriteLine(doc);
            //     }
            //     Console.WriteLine();
            // }

            // var dict = new Dictionary<string, int>();
            // dict["abds"] = 1;
            // dict["abdgdg"] = 2;
            // dict["abdxxd"] = 3;
            // dict["abdwere"] = 4;
            //
            // await DictionarySerialiazer.Serialize(@"D:\MapReduce\test.bin", dict);
            //
            // var test = await DictionaryDeserializer.Deserialize(@"D:\MapReduce\test.bin");
            //
            // foreach (var keyValuePair in test.Keys)
            // {
            //     Console.WriteLine(keyValuePair);
            // }
        }
    }
}