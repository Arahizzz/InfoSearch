using System;
using System.Diagnostics;

namespace Clasterisation
{
    class Program
    {
        static void Main(string[] args)
        {
            var collection = IndexedCollection.GetCollection(@"D:\MapReduce\gutenberg_txt\gutenberg\1\0");
            var vectorSpace = new VectorSpace(collection);
        }
    }
}